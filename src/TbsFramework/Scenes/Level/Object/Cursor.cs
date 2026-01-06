using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Nodes.Components;
using TbsFramework.UI.Controls.Device;

namespace TbsFramework.Scenes.Level.Object;

/// <summary>Cursor on the <see cref="Map.Grid"/> used for highlighting a cell and selecting things in it.</summary>
[Tool]
public partial class Cursor : GridNode
{
    private class CursorData() : GridObjectData(false) { public override GridObjectData Clone() => throw new NotSupportedException("The cursor's data should never need to be copied."); }

    /// <summary>Signals that the cell containing the cursor has changed.</summary>
    /// <param name="cell">New cell containing the cursor.</param>
    [Signal] public delegate void CellChangedEventHandler(Vector2I cell);

    /// <summary>Emitted when the cursor moves to a new location.</summary>
    /// <param name="region">Region enclosed by the cursor after movement.</param>
    [Signal] public delegate void CursorMovedEventHandler(Rect2 region);

    /// <summary>Emitted when a cursor stops in a new cell.</summary>
    /// <param name="cell">Cell the cursor stopped in.</param>
    [Signal] public delegate void CellEnteredEventHandler(Vector2I cell);

    /// <summary>Signals that a cell has been selected.</summary>
    /// <param name="cell">Coordinates of the cell that has been selected.</param>
    [Signal] public delegate void CellSelectedEventHandler(Vector2I cell);

    /// <summary>
    /// Compares two vector projections whose X values are their components along a direction and Y values are their components perpendicular
    /// to it such that vectors that are longer along the parallel axis are lesser. If they're the same distance, then ones that are shorter
    /// along the perpendicular axis are lesser.
    /// </summary>
    private static readonly IComparer<Vector2> FurtherAlongDirection = Comparer<Vector2>.Create(static (a, b) => {
        // Prioritize cells further away along direction
        if (a.X > b.X)
            return -1;
        else if (a.X < b.X)
            return 1;

        // ... but cells closer along direction's inverse
        if (a.Y < b.Y)
            return -1;
        else if (a.Y > b.Y)
            return 1;
        
        return 0;
    });

    private readonly NodeCache _cache = null;
    private Vector2I _previous = Vector2I.Zero;
    private ImmutableHashSet<Vector2I> _hard = [];
    private bool _halted = false;
    private bool _skip = false;

    private MoveController    MoveController  => _cache.GetNode<MoveController>("MoveController");
    private AudioStreamPlayer MoveSoundPlayer => _cache.GetNodeOrNull<AudioStreamPlayer>("MoveSound");

    /// <summary>Whether or not the cursor should wrap to the other side if a direction is pressed toward the edge it's on.</summary>
    [Export] public bool Wrap = false;

    /// <summary>Sound to play when the cursor moves to a new cell.</summary>
    [Export] public AudioStream MoveSound
    {
        get => MoveSoundPlayer?.Stream;
        set
        {
            if (MoveSoundPlayer is not null)
                MoveSoundPlayer.Stream = value;
        }
    }

    public override GridObjectData Data { get; } = new CursorData();

    /// <summary>"Soft zone" that breaks cursor continuous movement and skips to the edge of.</summary>
    public HashSet<Vector2I> SoftRestriction = [];

    /// <summary>
    /// Set of cells the cursor is restricted to moving in.  If empty, the cursor moves normally on the whole <see cref="Map.Grid"/>. Setting
    /// this value can cause the cursor to move if its current cell is not in the restriction.
    /// </summary>
    public ImmutableHashSet<Vector2I> HardRestriction
    {
        get => _hard;
        set
        {
            _hard = value;
            if (!_hard.IsEmpty)
            {
                if (!_hard.Contains(Cell))
                    Cell = _hard.OrderBy((c) => Cell.DistanceTo(c)).First();
                MoveController.EnableAnalog = true;
            }
            else
                MoveController.EnableAnalog = false;
        }
    }

    public Cursor() : base() { _cache = new(this); }

    /// <summary>Disable cursor movement.</summary>
    /// <param name="visible">Whether or not to hide the cursor while it's halted.</param>
    public void Halt(bool hide=false)
    {
        _halted = true;
        Visible = !hide;
    }

    /// <summary>Re-enable cursor movement.</summary>
    public void Resume()
    {
        _halted = false;
        Visible = true;
    }

    /// <summary>When a direction is pressed, move the cursor to the adjacent cell there and signal that the cell has been entered.</summary>
    /// <param name="direction">Direction that was pressed.</param>
    public void OnDirectionPressed(Vector2I direction)
    {
        if (!_halted)
        {
            if (_skip)
            {
                if (!HardRestriction.IsEmpty)
                {
                    IEnumerable<Vector2I> ahead = HardRestriction.Where((c) => (c - Cell)*direction > Vector2I.Zero);
                    if (ahead.Any())
                        Cell = ahead.OrderBy((c) => (c - Cell).ProjectionsTo(direction).Abs(), FurtherAlongDirection).First();
                }
                else
                {
                    if ((Cell.Y == 0 && direction.Y < 0) || (Cell.Y == Grid.Size.Y - 1 && direction.Y > 0))
                        direction = direction with { Y = 0 };
                    if ((Cell.X == 0 && direction.X < 0) || (Cell.X == Grid.Size.X - 1 && direction.X > 0))
                        direction = direction with { X = 0 };

                    if (direction != Vector2I.Zero)
                    {
                        if (SoftRestriction.Count != 0)
                        {
                            bool traversable = SoftRestriction.Contains(Cell + direction);
                            Vector2I target = Cell; // Don't want to directly update cell to avoid firing events
                            while (Grid.Contains(target + direction) && SoftRestriction.Contains(target + direction) == traversable)
                                target += direction;
                            Cell = target;
                        }
                        else
                            Cell = Data.Grid.Clamp(Cell + direction*Grid.Size);
                    }
                }
            }
            else
            {
                OnDirectionEchoed(direction);
                OnDirectionReleased(direction);
            }
        }
    }

    /// <summary>When a direction is pressed, move the curso to the adjacent cell there but don't signal the entry.</summary>
    /// <param name="direction">Direction that was pressed.</param>
    public void OnDirectionEchoed(Vector2I direction)
    {
        if (!_halted)
        {
            if (_hard.IsEmpty)
            {
                if (Wrap)
                    Cell = (Cell + direction + Grid.Size) % Grid.Size;
                else
                    Cell += direction;
            }
            else
            {
                IEnumerable<Vector2I> ahead = HardRestriction.Where((c) => (c - Cell)*direction > Vector2I.Zero);
                if (ahead.Any())
                    Cell = ahead.OrderBy((c) => ((c - Cell).Abs().Sum(), (c - Cell).Normalized().Dot(direction)), static (a, b) => {
                        (int dA, float thetaA) = a;
                        (int dB, float thetaB) = b;

                        // Prioritize closer cells
                        if (dA != dB)
                            return dA - dB;

                        // ...but smaller angles (larger dot products) if distance is equal
                        if (thetaA < thetaB)
                            return 1;
                        else if (thetaA > thetaB)
                            return -1;

                        return 0;
                    }).First();
                else if (Wrap)
                    Cell = HardRestriction.OrderBy((c) => (c - Cell).ProjectionsTo(direction).Abs(), FurtherAlongDirection).First();
            }
        }
    }

    /// <summary>When a direction is released, signal that the current cell has been entered.</summary>
    /// <remarks>Mostly only useful for visual updates. Could fire twice when not echoing.</remarks>
    /// <param name="direction">Direction that was released.</param>
    public void OnDirectionReleased(Vector2I direction)
    {
        if (!_halted)
            EmitSignal(SignalName.CellEntered, Cell);
    }

    /// <summary>
    /// When the cursor's cell changes, play the cursor-moved sound, stop echoing digital movement at the edge of a movement region, and emit the
    /// <see cref="SignalName.CursorMoved"/> signal to indicate the new rectangle enclosed by the cursor.
    /// </summary>
    /// <param name="cell">Cell that was moved to.</param>
    public void OnCellChanged(Vector2I cell)
    {
        if (!_halted)
        {
            MoveSoundPlayer.Play();
            if (!MoveController.Active && (DeviceManager.Mode == InputMode.Digital || !HardRestriction.IsEmpty))
                Callable.From<Vector2I>((c) => EmitSignal(SignalName.CellEntered, c)).CallDeferred(cell);
        }

        // Briefly break continuous digital movement to allow reaction from the player when the cursor has reached the edge of the soft restriction
        if (SoftRestriction.Contains(cell))
        {
            if (DeviceManager.Mode == InputMode.Digital)
            {
                Vector2I direction = cell - _previous;
                Vector2I further = cell + direction;
                if (Grid.Contains(further) && !SoftRestriction.Contains(further))
                    MoveController.ResetEcho();
            }
        }

        EmitSignal(SignalName.CellChanged, cell);
        EmitSignal(SignalName.CursorMoved, Grid.CellRect(cell));
        _previous = cell;
    }

    /// <summary>Update the <see cref="Map.Grid"/> cell when the pointer signals it has moved, unless the cursor is what's controlling movement.</summary>
    /// <param name="position">Position of the pointer.</param>
    public void OnPointerMoved(Vector2 position)
    {
        if (DeviceManager.Mode != InputMode.Digital && (HardRestriction.IsEmpty || HardRestriction.Contains(Grid.CellOf(position))))
            Cell = Grid.CellOf(position);
    }

    /// <summary>
    /// Skip in a direction, stopping at the edge of the <see cref="HardRestriction"/>, <see cref="SoftRestriction"/>, or <see cref="Map.Grid"/>, whichever is first.
    /// </summary>
    /// <param name="direction">Direction to skip.</param>
    public void OnSkip(Vector2I direction)
    {
        if (!_halted)
        {
            if (!HardRestriction.IsEmpty)
            {
                IEnumerable<Vector2I> ahead = HardRestriction.Where((c) => (c - Cell)*direction > Vector2I.Zero);
                if (ahead.Any())
                    Cell = ahead.OrderBy((c) => (c - Cell).ProjectionsTo(direction).Abs(), FurtherAlongDirection).First();
            }
            else
            {
                if ((Cell.Y == 0 && direction.Y < 0) || (Cell.Y == Grid.Size.Y - 1 && direction.Y > 0))
                    direction = direction with { Y = 0 };
                if ((Cell.X == 0 && direction.X < 0) || (Cell.X == Grid.Size.X - 1 && direction.X > 0))
                    direction = direction with { X = 0 };

                if (direction != Vector2I.Zero)
                {
                    if (SoftRestriction.Count != 0)
                    {
                        bool traversable = SoftRestriction.Contains(Cell + direction);
                        Vector2I target = Cell; // Don't want to directly update cell to avoid firing events
                        while (Grid.Contains(target + direction) && SoftRestriction.Contains(target + direction) == traversable)
                            target += direction;
                        Cell = target;
                    }
                    else
                        Cell = Data.Grid.Clamp(Cell + direction*Grid.Size);
                }
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();
        _previous = Cell;
        Data.CellChanged += OnCellChanged;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (!_halted)
        {
            if (@event.IsActionPressed(InputManager.Select))
                EmitSignal(SignalName.CellSelected, Grid.CellOf(Position));
            
            if (@event.IsActionPressed(InputManager.Accelerate))
                _skip = true;
            else if (@event.IsActionReleased(InputManager.Accelerate))
                _skip = false;
        }
    }
}