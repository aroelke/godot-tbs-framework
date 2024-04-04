using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotStateCharts;
using Level.Map;
using Level.Object;
using Level.Object.Group;
using Level.UI;
using Object;
using UI;
using UI.Controls.Action;
using UI.Controls.Device;
using UI.HUD;
using Util;

namespace Level;

/// <summary>
/// Manages the setup of and objects inside a level and provides them information about it.  Is "global" in a way in that
/// all objects in the level may be able to see it and request information from it, but each level has its own manager.
/// </summary>
[Tool]
public partial class Level : Node2D
{
    // State chart events
    private const string SelectEvent = "select";
    private const string CancelEvent = "cancel";
    private const string DoneEvent = "done";
    // State chart conditions
    private const string OccupiedCondition = "occupied";
    private const string SelectedCondition = "selected";
    private const string TraversableCondition = "traversable";

    private StateChart _state = null;
    private StateChartState _selectedState = null;
    private Grid _map = null;
    private Overlay _overlay = null;
    private Camera2DBrain _camera = null;
    private Cursor _cursor = null;
    private Pointer _pointer = null;
    private Vector2I _cursorPrev = Vector2I.Zero;
    private Unit _selected = null;
    private PathFinder _pathfinder = null;
    private ControlHint _cancelHint = null;
    private Vector2? _prevZoom = null;
    private Vector4 _prevDeadzone = new();
    private BoundedNode2D _prevTarget = null;
    private Vector2I? _initialCell = null;
    private Path _path = null;

    private Grid Grid => _map ??= GetNode<Grid>("Grid");
    private Overlay Overlay => _overlay ??= GetNode<Overlay>("Overlay");
    private Camera2DBrain Camera => _camera ??= GetNode<Camera2DBrain>("Camera");
    private Cursor Cursor => _cursor ??= GetNode<Cursor>("Cursor");
    private Pointer Pointer => _pointer ??= GetNode<Pointer>("Pointer");
    private ControlHint CancelHint => _cancelHint ??= GetNode<ControlHint>("UserInterface/HUD/Hints/CancelHint");

    /// <summary>Restore the camera zoom back to what it was before a unit was selected.</summary>
    private void RestoreCameraZoom()
    {
        if (_prevZoom is not null)
        {
            Camera.ZoomTarget = _prevZoom.Value;
            _prevZoom = null;
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

    /// <summary>Deselect any units and clear drawn ranges.</summary>
    public void OnIdleEntered()
    {
        if (_selected is not null)
        {
            _selected.Deselect();
            _selected = null;
        }

        CancelHint.Visible = false;
    }

    /// <summary>Display the total movement, attack, and support ranges of the selected unit and begin drawing the path arrow for it to move on.</summary>
    public void OnSelectedEntered()
    {
        _selected.Select();
        _initialCell = _selected.Cell;

        // Compute move/attack/support ranges for selected unit
        _pathfinder = new(Grid, _selected);
        _path = Path.Empty(Grid, _pathfinder.TraversableCells).Add(_selected.Cell);
        Overlay.TraversableCells = _pathfinder.TraversableCells;
        Overlay.AttackableCells = _pathfinder.AttackableCells.Where((c) => {
            if (Grid.Occupants.ContainsKey(c) && ((Grid.Occupants[c] as Unit)?.Affiliation.AlliedTo(_selected) ?? false)) // exclude cells occupied by allies
                return false;
            else
                return !Overlay.TraversableCells.Contains(c);
        });
        Overlay.SupportableCells = _pathfinder.SupportableCells.Where((c) => {
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
        Overlay.Clear();
        RestoreCameraZoom();
    }

    public void OnSelectedToTargeting() => _pathfinder = null;

    public void OnSelectedToIdle() => _pathfinder = null;

    /// <summary>
    /// Move the cursor back to the unit's position if it's not there already (waiting for it to move if it's mouse controlled),
    /// then go back to the previous state (i.e. the one before the one that was canceled).
    /// </summary>
    public void OnCancelSelectionEntered()
    {
        // Clear out the displayed ranges. This can't be done on selected exit because it needs to exist when entering moving.
        _pathfinder = null;

        // In some cases (namely, when the player selects a square outside the selected unit's move range), the cursor should not warp
        // when canceling selection. This is indicated by nulling the selectd unit before transitioning to the "cancel selection" state
        if (_selected is not null)
            WarpCursor(_selected.Cell);

        // Go to idle state
        _state.SendEvent(DoneEvent);
    }

    /// <summary>Begin moving the selected unit and then wait for it to finish moving.</summary>
    public void OnMovingEntered()
    {
        // Move the unit and delete the pathfinder as we don't need it anymore
        Grid.Occupants.Remove(_selected.Cell);
        _selected.MoveAlong(_path.ToList());
        _selected.DoneMoving += OnUnitDoneMoving;
        Grid.Occupants[_selected.Cell] = _selected;
        _pathfinder = null;

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
    }

    /// <summary>Move the selected unit back to its starting position and move the pointer there, then go back to "selected" state.</summary>
    public void OnCancelTargetingEntered()
    {
        // Move the selected unit back to its original cell
        Grid.Occupants.Remove(_selected.Cell);
        _selected.Cell = _initialCell.Value;
        _selected.Position = Grid.PositionOf(_selected.Cell);
        Grid.Occupants[_selected.Cell] = _selected;

        WarpCursor(_selected.Cell);

        _initialCell = null;
        _state.SendEvent(DoneEvent);
    }

    /// <summary>Compute the attack and support ranges of the selected unit from its location.</summary>
    public void OnTargetingEntered()
    {
        // Show the unit's attack/support ranges
        IEnumerable<Vector2I> attackable = PathFinder.GetCellsInRange(Grid, _selected.AttackRange, _selected.Cell);
        IEnumerable<Vector2I> supportable = PathFinder.GetCellsInRange(Grid, _selected.SupportRange, _selected.Cell);
        Overlay.AttackableCells = attackable.Where((c) => Grid.Occupants.ContainsKey(c) && (!(Grid.Occupants[c] as Unit)?.Affiliation.AlliedTo(_selected) ?? false));
        Overlay.SupportableCells = supportable.Where((c) => Grid.Occupants.ContainsKey(c) && ((Grid.Occupants[c] as Unit)?.Affiliation.AlliedTo(_selected) ?? false));
    }

    /// <summary>Clean up when exiting targeted state.</summary>
    public void OnTargetingExited()
    {
        Overlay.Clear();
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
        if (_selectedState.Active && _pathfinder.TraversableCells.Contains(cell))
        {
//            _pathfinder.AddToPath(cell);
//            Overlay.Path = _pathfinder.Path;
            Overlay.Path = (_path = _path.Add(cell).Clamp(_selected.MoveRange)).ToList();

            if (DeviceManager.Mode == InputMode.Digital)
            {
                Vector2I direction = cell - _cursorPrev;
                Vector2I next = cell + direction;
                if (Grid.Contains(next) && !_pathfinder.TraversableCells.Contains(next))
                    Cursor.BreakMovement();
            }
        }

        _cursorPrev = cell;
    }

    /// <summary>
    /// When the cursor wants to skip, move it to the edge of the region it's in in that direction. The "region the cursor is in" is defined based
    /// on whether or not it's inside the set of traversable cells (if a unit is selected) or not.
    /// </summary>
    /// <param name="direction">Direction to skip the cursor in.</param>
    public void OnCursorRequestSkip(Vector2I direction)
    {
        if ((Cursor.Cell.Y == 0 && direction.Y < 0) || (Cursor.Cell.Y == Grid.Size.Y - 1 && direction.Y > 0))
            direction = direction with { Y = 0 };
        if ((Cursor.Cell.X == 0 && direction.X < 0) || (Cursor.Cell.X == Grid.Size.X - 1 && direction.X > 0))
            direction = direction with { X = 0 };

        if (direction != Vector2I.Zero)
        {
            Vector2I neighbor = Cursor.Cell + direction;
            if (_selectedState.Active)
            {
                bool traversable = _pathfinder.TraversableCells.Contains(neighbor);
                Vector2I target = neighbor;
                int i = 1;
                while (Grid.Contains(neighbor + direction*i) && _pathfinder.TraversableCells.Contains(neighbor + direction*i) == traversable)
                {
                    target = neighbor + direction*i;
                    i++;
                }
                Cursor.Cell = target;
            }
            else
                Cursor.Cell = Grid.Clamp(Cursor.Cell + direction*Grid.Size);
        }
    }

    /// <summary>When a cell is selected, act based on what is or isn't in the cell.</summary>
    /// <param name="cell">Coordinates of the cell selection.</param>
    public void OnCellSelected(Vector2I cell)
    {
        if (_selected is null && Grid.Occupants.ContainsKey(cell) && Grid.Occupants[cell] is Unit unit)
            _selected = unit;
        _state.SendEvent(SelectEvent);
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
            _state = StateChart.Of(GetNode("State"));
            _selectedState = StateChartState.Of(GetNode("State/Root/UnitSelected"));

            Camera.Limits = new(Vector2I.Zero, (Vector2I)(Grid.Size*Grid.CellSize));
            Cursor.Grid = Grid;
            _cursorPrev = Cursor.Cell;
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
            _state.SetExpressionProperty(OccupiedCondition, Grid.Occupants.ContainsKey(Cursor.Cell) && Grid.Occupants[Cursor.Cell] is Unit);
            _state.SetExpressionProperty(SelectedCondition, _selected is not null && Grid.Occupants.ContainsKey(Cursor.Cell) && (Grid.Occupants[Cursor.Cell] as Unit) == _selected);
            _state.SetExpressionProperty(TraversableCondition, _pathfinder is not null && _pathfinder.TraversableCells.Contains(Cursor.Cell));

            float zoom = Input.GetAxis(CameraActionAnalogZoomOut, CameraActionAnalogZoomIn);
            if (zoom != 0)
                Camera.Zoom += Vector2.One*(float)(CameraZoomAnalogFactor*zoom*delta);
        }
    }
}