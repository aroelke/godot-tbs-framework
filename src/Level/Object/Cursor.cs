using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using UI.Controls.Action;
using UI.Controls.Device;
using Extensions;

namespace Level.Object;

/// <summary>Cursor on the <see cref="Map.Grid"/> used for highlighting a cell and selecting things in it.</summary>
[Tool]
public partial class Cursor : GridNode
{
    /// <summary>Emitted when the cursor moves to a new cell.</summary>
    /// <param name="cell">Position of the center of the cell moved to.</param>
    [Signal] public delegate void CursorMovedEventHandler(Vector2 position);

    /// <summary>Signals that a cell has been selected.</summary>
    /// <param name="cell">Coordinates of the cell that has been selected.</param>
    [Signal] public delegate void CellSelectedEventHandler(Vector2I cell);

    private ImmutableHashSet<Vector2I> _hard = ImmutableHashSet<Vector2I>.Empty;

    private DigitalMoveAction _mover = null;
    private DigitalMoveAction MoveController => _mover ??= GetNode<DigitalMoveAction>("MoveController");

    private Node _converters = null;
    private Node Converters => _converters ??= GetNode<Node>("AnalogDigital");

    /// <summary>Action for selecting a cell.</summary>
    [Export] public InputActionReference SelectAction = new();

    /// <summary>Whether or not the cursor should wrap to the other side if a direction is pressed toward the edge it's on.</summary>
    [Export] public bool Wrap = false;

    /// <summary>Cell the cursor occupies. Overrides <see cref="GridNode.Cell"/> to ensure that the position is updated before any signals fire.</summary>
    public override Vector2I Cell
    {
        get => base.Cell;
        set
        {
            if (Engine.IsEditorHint() && Grid is null)
                base.Cell = value;
            else
            {
                Vector2I next = Grid.Clamp(value);
                if (next != Cell)
                {
                    // Briefly break continuous digital movement to allow reaction from the player when the cursor has reached the edge of the soft restriction
                    if (SoftRestriction.Contains(next))
                    {
                        if (DeviceManager.Mode == InputMode.Digital)
                        {
                            Vector2I direction = next - base.Cell;
                            Vector2I further = value + direction;
                            if (Grid.Contains(further) && !SoftRestriction.Contains(further))
                                MoveController.ResetEcho();
                        }
                    }

                    Position = Grid.PositionOf(next);
                    base.Cell = next;
                    EmitSignal(SignalName.CursorMoved, Position + Grid.CellSize/2);
                }
            }
        }
    }

    /// <summary>"Soft zone" that breaks cursor continuous movement and skips to the edge of.</summary>
    public HashSet<Vector2I> SoftRestriction = new();

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
            if (_hard.Any())
            {
                if (!_hard.Contains(Cell))
                    Cell = _hard.OrderBy((c) => Cell.DistanceTo(c)).First();
                Converters.ProcessMode = ProcessModeEnum.Inherit;
            }
            else
                Converters.ProcessMode = ProcessModeEnum.Disabled;
        }
    }

    /// <summary>When a direction is pressed, move the cursor to the adjacent cell there.</summary>
    public void OnDirectionPressed(Vector2I direction)
    {
        if (!_hard.Any())
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
                Cell = ahead.OrderBy((c) => (c*direction.Inverse()).Length()).OrderBy((c) => Cell.DistanceTo(c)).OrderBy((c) => ((c - Cell)*direction).Length()).First();
            else if (Wrap)
                Cell = HardRestriction.OrderBy((c) => (c*direction.Inverse()).Length()).OrderByDescending((c) => ((c - Cell)*direction).Length()).First();
        }
    }

    /// <summary>Update the <see cref="Map.Grid"/> cell when the pointer signals it has moved, unless the cursor is what's controlling movement.</summary>
    /// <param name="position">Position of the pointer.</param>
    public void OnPointerMoved(Vector2 position)
    {
        if (DeviceManager.Mode != InputMode.Digital && (!HardRestriction.Any() || HardRestriction.Contains(Grid.CellOf(position))))
            Cell = Grid.CellOf(position);
    }

    /// <summary>
    /// Skip in a direction, stopping at the edge of the <see cref="HardRestriction"/>, <see cref="SoftRestriction"/>, or <see cref="Map.Grid"/>, whichever is first.
    /// </summary>
    /// <param name="direction">Direction to skip.</param>
    public void OnSkip(Vector2I direction)
    {
        if (HardRestriction.Any())
        {
            IEnumerable<Vector2I> ahead = HardRestriction.Where((c) => (c - Cell)*direction > Vector2I.Zero);
            if (ahead.Any())
                Cell = ahead.OrderBy((c) => (c*direction.Inverse()).Length()).OrderBy((c) => Cell.DistanceTo(c)).OrderByDescending((c) => ((c - Cell)*direction).Length()).First();
        }
        else
        {
            if ((Cell.Y == 0 && direction.Y < 0) || (Cell.Y == Grid.Size.Y - 1 && direction.Y > 0))
                direction = direction with { Y = 0 };
            if ((Cell.X == 0 && direction.X < 0) || (Cell.X == Grid.Size.X - 1 && direction.X > 0))
                direction = direction with { X = 0 };

            if (direction != Vector2I.Zero)
            {
                if (SoftRestriction.Any())
                {
                    bool traversable = SoftRestriction.Contains(Cell + direction);
                    Vector2I target = Cell; // Don't want to directly update cell to avoid firing events
                    while (Grid.Contains(target + direction) && SoftRestriction.Contains(target + direction) == traversable)
                        target += direction;
                    Cell = target;
                }
                else
                    Cell = Grid.Clamp(Cell + direction*Grid.Size);
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (@event.IsActionReleased(SelectAction))
            EmitSignal(SignalName.CellSelected, Grid.CellOf(Position));
    }
}