using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Level.Map;
using Level.Object;
using Level.Object.Group;
using Level.UI;
using Object;
using UI;
using UI.Controls.Action;
using UI.Controls.Device;
using UI.HUD;
using Extensions;
using Object.StateChart;
using Object.StateChart.States;

namespace Level;

/// <summary>
/// Manages the setup of and objects inside a level and provides them information about it.  Is "global" in a way in that
/// all objects in the level may be able to see it and request information from it, but each level has its own manager.
/// </summary>
[Tool]
public partial class Level : Node2D
{
    // State chart events
    private readonly StringName SelectEvent = "select";
    private readonly StringName CancelEvent = "cancel";
    private readonly StringName DoneEvent = "done";
    // State chart conditions
    private readonly StringName OccupiedCondition = "occupied";       // Current cell occupant (see below for options)
    private readonly StringName TraversableCondition = "traversable"; // Current cell is traversable
    // State chart occupied values
    private const string NotOccupied = "";                  // Nothing in the cell
    private const string SelectedOccuiped = "selected";     // Cell occupied by the selected unit (if there is one)
    private const string ActiveAllyOccupied = "active";     // Cell occupied by an active unit in this turn's army
    private const string InActiveAllyOccupied = "inactive"; // Cell occupied by an inactive unit in this turn's army
    private const string FriendlyOccuipied = "friendly";    // Cell occupied by unit in army allied to this turn's army
    private const string EnemyOccupied = "enemy";           // Cell occupied by unit in enemy army to this turn's army
    private const string OtherOccupied = "other";           // Cell occupied by something else

    private Chart _state = null;
    private State _selectedState = null;
    private Grid _map = null;
    private Overlay _overlay = null;
    private Camera2DBrain _camera = null;
    private Cursor _cursor = null;
    private Pointer _pointer = null;
    private Unit _selected = null;
    private ControlHint _cancelHint = null;
    private Vector2? _prevZoom = null;
    private Vector4 _prevDeadzone = new();
    private BoundedNode2D _prevTarget = null;
    private Vector2I? _initialCell = null;
    private Path _path = null;
    private IEnumerable<Vector2I> _traversable = Array.Empty<Vector2I>(), _attackable = Array.Empty<Vector2I>(), _supportable = Array.Empty<Vector2I>();
    private Army[] _armies = Array.Empty<Army>();
    private int _armyIndex = 0;
    private int _turn = 1;
    private Label _turnLabel = null;

    private Grid Grid => _map ??= GetNode<Grid>("Grid");
    private Overlay Overlay => _overlay ??= GetNode<Overlay>("Overlay");
    private Camera2DBrain Camera => _camera ??= GetNode<Camera2DBrain>("Camera");
    private Cursor Cursor => _cursor ??= GetNode<Cursor>("Cursor");
    private Pointer Pointer => _pointer ??= GetNode<Pointer>("Pointer");
    private ControlHint CancelHint => _cancelHint ??= GetNode<ControlHint>("UserInterface/HUD/Hints/CancelHint");
    private Label TurnLabel => _turnLabel = GetNode<Label>("%TurnLabel");

    private int CurrentArmy
    {
        get => _armyIndex;
        set
        {
            if (_armies.Any())
            {
                // Refresh all the units in the current army so they aren't gray anymore and are animated
                if (!Engine.IsEditorHint())
                    foreach (Unit unit in (IEnumerable<Unit>)_armies[_armyIndex])
                        unit.Refresh();

                // Increment the army pointer, wrapping back to the first one if it's at the end, and incrementing the turn count if it did
                _armyIndex = value % _armies.Length;

                // Update the UI
                if (!Engine.IsEditorHint())
                {
                    TurnLabel.AddThemeColorOverride("font_color", _armies[_armyIndex].Color);
                    TurnLabel.Text = $"Turn {Turn}: {_armies[_armyIndex].Name}";
                }
            }
            else
                _armyIndex = 0;
        }
    }

    /// <summary>
    /// If the cursor isn't in the specified cell, move it to (the center of) that cell. During mouse control, this is done smoothly
    /// over time to maintain consistency with the system pointer.
    /// </summary>
    /// <param name="cell">Cell to move the cursor to.</param>
    private async void WarpCursor(Vector2I cell)
    {
        Rect2 rect = Grid.CellRect(cell);
        switch (DeviceManager.Mode)
        {
        case InputMode.Mouse:
            // If the input mode is mouse and the cursor is not on the cell's square, move it there over time
            if (!rect.HasPoint(GetGlobalMousePosition()))
            {
                Tween tween = CreateTween();
                tween
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.Out)
                    .TweenMethod(
                        Callable.From((Vector2 position) => {
                            Pointer.Position = position;
                            GetViewport().WarpMouse(Pointer.ViewportPosition);
                        }),
                        Pointer.Position,
                        Grid.PositionOf(cell) + Grid.CellSize/2,
                        Camera.DeadZoneSmoothTime
                    );

                BoundedNode2D target = Camera.Target;
                Camera.Target = Grid.Occupants[cell];
                await ToSignal(tween, Tween.SignalName.Finished);
                tween.Kill();
                Camera.Target = target;
            }
            break;
        // If the input mode is digital or analog, just warp the cursor back to the cell
        case InputMode.Digital:
            Cursor.Cell = cell;
            break;
        case InputMode.Analog:
            if (!rect.HasPoint(Pointer.Position))
                Pointer.Warp(rect.GetCenter());
            break;
        }
    }

    /// <summary>
    /// <see cref="Army"/> that gets the first turn. If null, use the first <see cref="Army"/> in the child list.
    /// After that, go down the child list in order, wrapping when at the end.
    /// </summary>
    [Export] public Army StartingArmy = null;

    /// <summary>Turn count (including current turn, so it starts at 1).</summary>
    [Export] public int Turn
    {
        get => _turn;
        set
        {
            _turn = value;
            if (!Engine.IsEditorHint())
                _turnLabel.Text = $"Turn {_turn}: {_armies[CurrentArmy].Name}";
        }
    }

    /// <summary>Map cancel selection action reference (distinct from menu back/cancel).</summary>
    [Export] public InputActionReference CancelAction = new();

    [ExportGroup("Camera/Zoom", "CameraZoom")]

    /// <summary>Amount to zoom the camera each time it's digitally zoomed.</summary>
    [Export] public float CameraZoomDigitalFactor = 0.25f;

    /// <summary>Amount to zoom the camera while it's being zoomed with an analog stick.</summary>
    [Export] public float CameraZoomAnalogFactor = 2;

    [ExportGroup("Camera/Input Actions", "CameraAction")]

    /// <summary>Digital action to zoom the camera in.</summary>
    [Export] public InputActionReference CameraActionDigitalZoomIn = new();

    /// <summary>Analog action to zoom the camera in.</summary>
    [Export] public InputActionReference CameraActionAnalogZoomIn = new();

    /// <summary>Digital action to zoom the camera out.</summary>
    [Export] public InputActionReference CameraActionDigitalZoomOut = new();

    /// <summary>Analog action to zoom the camera out.</summary>
    [Export] public InputActionReference CameraActionAnalogZoomOut = new();

    /// <summary>Deselect or deactivate the selected <see cref="Unit"/> and clean up after finishing actions.</summary>
    /// <param name="done">Whether or not the unit completed its action (so it should be deactivated).</param>
    public void OnToIdleTaken(bool done)
    {
        if (done)
        {
            _selected.Finish();

            // Switch to the next army
            if (!((IEnumerable<Unit>)_armies[CurrentArmy]).Any((u) => u.Active))
            {
                CurrentArmy++;
                if (CurrentArmy == Array.IndexOf(_armies, StartingArmy))
                    Turn++;
            }
        }
        else
            _selected.Deselect();
        _selected = null;

        CancelHint.Visible = false;
    }

    /// <summary>Choose a selected unit.</summary>
    public void OnIdleToSelectedTaken() => _selected = Grid.Occupants[Cursor.Cell] as Unit;

    /// <summary>Display the total movement, attack, and support ranges of the selected unit and begin drawing the path arrow for it to move on.</summary>
    public void OnSelectedEntered()
    {
        _selected.Select();
        _initialCell = _selected.Cell;

        // Compute move/attack/support ranges for selected unit
        _traversable = _selected.TraversableCells();
        _attackable = _selected.AttackableCells(_traversable);
        _supportable = _selected.SupportableCells(_traversable);
        _path = Path.Empty(Grid, _traversable).Add(_selected.Cell);
        Cursor.SoftRestriction = _traversable.ToHashSet();
        Overlay.TraversableCells = _traversable;
        Overlay.AttackableCells = _attackable.Where((c) => {
            if (Grid.Occupants.ContainsKey(c) && ((Grid.Occupants[c] as Unit)?.Affiliation.AlliedTo(_selected) ?? false)) // exclude cells occupied by allies
                return false;
            else
                return !Overlay.TraversableCells.Contains(c);
        });
        Overlay.SupportableCells = _supportable.Where((c) => {
            if (Overlay.TraversableCells.Contains(c) || Overlay.AttackableCells.Contains(c))
                return false;
            else
                return !Grid.Occupants.ContainsKey(c) || ((Grid.Occupants[c] as Unit)?.Affiliation.AlliedTo(_selected) ?? false); // include cells occupied by allies
        });
        CancelHint.Visible = true;

        // If the camera isn't zoomed out enough to show the whole range, zoom out so it does
        Rect2? zoomRect = Overlay.GetEnclosingRect(Grid);
        if (zoomRect is not null)
        {
            Vector2 zoomTarget = GetViewportRect().Size/zoomRect.Value.Size;
            zoomTarget = Vector2.One*Mathf.Min(zoomTarget.X, zoomTarget.Y);
            if (Camera.Zoom > zoomTarget)
            {
                _prevZoom = Camera.Zoom;
                Camera.ZoomTarget = zoomTarget;
            }
        }
    }

    /// <summary>Clean up when exiting selected state.</summary>
    public void OnSelectedExited()
    {
        // Clear out movement/action ranges
        _traversable = Array.Empty<Vector2I>();
        Cursor.SoftRestriction.Clear();
        Overlay.Clear();
        
        // Restore the camera zoom back to what it was before a unit was selected
        if (_prevZoom is not null)
        {
            Camera.ZoomTarget = _prevZoom.Value;
            _prevZoom = null;
        }
    }

    /// <summary>Begin moving the selected unit and then wait for it to finish moving.</summary>
    public void OnMovingEntered()
    {
        // Move the unit and delete the pathfinder as we don't need it anymore
        Grid.Occupants.Remove(_selected.Cell);
        _selected.MoveAlong(_path.ToList());
        _selected.DoneMoving += OnUnitDoneMoving;
        Grid.Occupants[_selected.Cell] = _selected;

        // Track the unit as it's moving
        _prevDeadzone = new(Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight);
        (Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight) = Vector4.Zero;
        _prevTarget = Camera.Target;
        Camera.Target = _selected.MotionBox;
    }

    /// <summary>When done moving, restore the camera target (most likely to the cursor).</summary>
    public void OnMovingExited()
    {
        (Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight) = _prevDeadzone;
        Camera.Target = _prevTarget;
        _path = null;
    }

    /// <summary>Compute the attack and support ranges of the selected unit from its location.</summary>
    public void OnTargetingEntered()
    {
        // Show the unit's attack/support ranges
        IEnumerable<Vector2I> attackable = _selected.AttackableCells();
        IEnumerable<Vector2I> supportable = _selected.SupportableCells();
        Overlay.AttackableCells = attackable.Where((c) => Grid.Occupants.ContainsKey(c) && (!(Grid.Occupants[c] as Unit)?.Affiliation.AlliedTo(_selected) ?? false));
        Overlay.SupportableCells = supportable.Where((c) => Grid.Occupants.ContainsKey(c) && ((Grid.Occupants[c] as Unit)?.Affiliation.AlliedTo(_selected) ?? false));

        // Restrict cursor movement to actionable cells
        Pointer.AnalogTracking = false;
        Cursor.HardRestriction = Overlay.AttackableCells.ToImmutableHashSet().Union(Overlay.SupportableCells);
        Cursor.Wrap = true;
        WarpCursor(Cursor.Cell);
    }

    /// <summary>Clean up displayed ranges and restore cursor freedom when exiting targeting state.</summary>
    public void OnTargetingExited()
    {
        Overlay.Clear();

        Pointer.AnalogTracking = true;
        Cursor.HardRestriction = Cursor.HardRestriction.Clear();
        Cursor.Wrap = false;
    }

    /// <summary>
    /// Move the selected unit back to its starting position (only does anything when canceling targeting). Then move the cursor to
    /// the unit's current position, and go back to the previous state.
    /// </summary>
    public void OnCancelUnitActionEntered()
    {
        // Move the selected unit back to its original cell
        Grid.Occupants.Remove(_selected.Cell);
        _selected.Cell = _initialCell.Value;
        _selected.Position = Grid.PositionOf(_selected.Cell);
        Grid.Occupants[_selected.Cell] = _selected;
        _initialCell = null;

        WarpCursor(_selected.Cell);
        _state.SendEvent(DoneEvent);
    }

    /// <summary>
    /// When the cursor moves:
    /// - While a unit is selected:
    ///   - Update the path that's being drawn
    ///   - Briefly break continuous digital movement if the cursor moves to the edge of the traversable region
    /// </summary>
    /// <param name="cell">Cell the cursor moved to.</param>
    public void OnCursorMoved(Vector2I cell)
    {
        if (_traversable.Contains(cell))
            Overlay.Path = (_path = _path.Add(cell).Clamp(_selected.MoveRange)).ToList();
    }

    /// <summary>When a cell is selected, act based on what is or isn't in the cell.</summary>
    /// <param name="cell">Coordinates of the cell selection.</param>
    public void OnCellSelected(Vector2I cell)
    {
        if (Grid.CellOf(Pointer.Position) == cell)
            _state.SendEvent(SelectEvent);
        else
            _state.SendEvent(CancelEvent);
    }

    /// <summary>When the unit finishes moving, move to the next state.</summary>
    public void OnUnitDoneMoving()
    {
        _selected.DoneMoving -= OnUnitDoneMoving;
        _state.SendEvent(DoneEvent);
    }

    /// <summary>When a grid node is added to a group, update its grid.</summary>
    /// <param name="child"></param>
    public void OnChildEnteredGroup(Node child)
    {
        if (child is GridNode gd)
            gd.Grid = Grid;
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        // Make sure there's a map
        int maps = GetChildren().Where((c) => c is Grid).Count();
        if (maps < 1)
            warnings.Add("Level does not contain a map.");
        else if (maps > 1)
            warnings.Add($"Level contains too many maps ({maps}).");

        // Make sure there are units to control and to fight.
        if (!GetChildren().Where((c) => c is Army).Any())
            warnings.Add("There are not any armies to assign units to.");

        return warnings.ToArray();
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            _state = GetNode<Chart>("State");
            _selectedState = GetNode<State>("State/Root/UnitSelected");

            Camera.Limits = new(Vector2I.Zero, (Vector2I)(Grid.Size*Grid.CellSize));
            Cursor.Grid = Grid;
            Pointer.Bounds = Camera.Limits;

            foreach (Node child in GetChildren())
            {
                if (child is IEnumerable<Unit> army)
                {
                    foreach (Unit unit in army)
                    {
                        unit.Grid = Grid;
                        unit.Cell = Grid.CellOf(unit.Position);
                        unit.Position = Grid.PositionOf(unit.Cell);
                        Grid.Occupants[unit.Cell] = unit;
                    }
                }
            }

            _armies = GetChildren().OfType<Army>().ToArray();
            StartingArmy ??= _armies[0];
            CurrentArmy = Array.IndexOf(_armies, StartingArmy);
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionReleased(CancelAction))
            _state.SendEvent(CancelEvent);

        if (@event.IsActionPressed(CameraActionDigitalZoomIn))
            Camera.ZoomTarget += Vector2.One*CameraZoomDigitalFactor;
        if (@event.IsActionPressed(CameraActionDigitalZoomOut))
            Camera.ZoomTarget -= Vector2.One*CameraZoomDigitalFactor;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            _state.ExpressionProperties = _state.ExpressionProperties
                .SetItem(OccupiedCondition, !Grid.Occupants.ContainsKey(Cursor.Cell) ? NotOccupied : Grid.Occupants[Cursor.Cell] switch
                {
                    Unit unit when unit == _selected => SelectedOccuiped,
                    Unit unit when _armies[CurrentArmy] == unit.Affiliation => unit.Active ? ActiveAllyOccupied : InActiveAllyOccupied,
                    Unit unit when _armies[CurrentArmy].AlliedTo(unit.Affiliation) => FriendlyOccuipied,
                    Unit => EnemyOccupied,
                    _ => OtherOccupied
                })
                .SetItem(TraversableCondition, _traversable.Contains(Cursor.Cell));

            float zoom = Input.GetAxis(CameraActionAnalogZoomOut, CameraActionAnalogZoomIn);
            if (zoom != 0)
                Camera.Zoom += Vector2.One*(float)(CameraZoomAnalogFactor*zoom*delta);
        }
    }
}