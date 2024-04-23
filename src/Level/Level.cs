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
public partial class Level : Node
{
#region Constants
    // State chart events
    private readonly StringName SelectEvent = "select";
    private readonly StringName CancelEvent = "cancel";
    private readonly StringName DoneEvent = "done";
    // State chart conditions
    private readonly StringName OccupiedProperty = "occupied";       // Current cell occupant (see below for options)
    private readonly StringName TargetProperty = "target";           // Current cell contains a potential target (for attack or support)
    private readonly StringName TraversableProperty = "traversable"; // Current cell is traversable
    // State chart occupied values
    private const string NotOccupied = "";                  // Nothing in the cell
    private const string SelectedOccuiped = "selected";     // Cell occupied by the selected unit (if there is one)
    private const string ActiveAllyOccupied = "active";     // Cell occupied by an active unit in this turn's army
    private const string InActiveAllyOccupied = "inactive"; // Cell occupied by an inactive unit in this turn's army
    private const string FriendlyOccuipied = "friendly";    // Cell occupied by unit in army allied to this turn's army
    private const string EnemyOccupied = "enemy";           // Cell occupied by unit in enemy army to this turn's army
    private const string OtherOccupied = "other";           // Cell occupied by something else

    // Zone layer names
    private const string LocalDangerZone = "local danger";
    private const string AllyTraversable = "ally traversable";
    private const string GlobalDanger = "global danger";
#endregion
#region Declarations
    private Chart _state = null;
    private Grid _map = null;
    private Path _path = null;
    private PathOverlay _pathOverlay = null;
    private RangeOverlay _actionOverlay = null;
    private Camera2DBrain _camera = null;
    private Cursor _cursor = null;
    private Pointer _pointer = null;
    private Unit _selected = null, _target = null;
    private IEnumerator<Army> _armies = null;
    private Vector2I? _initialCell = null;
    private ControlHint _cancelHint = null;
    private AudioStreamPlayer _errorSound = null;

    private Grid Grid => _map ??= GetNode<Grid>("Grid");
    private PathOverlay PathOverlay => _pathOverlay ??= GetNode<PathOverlay>("PathOverlay");
    private RangeOverlay ActionOverlay => _actionOverlay ??= GetNode<RangeOverlay>("ActionRangeOverlay");
    private Camera2DBrain Camera => _camera ??= GetNode<Camera2DBrain>("Camera");
    private Cursor Cursor => _cursor ??= GetNode<Cursor>("Cursor");
    private Pointer Pointer => _pointer ??= GetNode<Pointer>("Pointer");
    private ControlHint CancelHint => _cancelHint ??= GetNode<ControlHint>("UserInterface/HUD/Hints/CancelHint");
    private AudioStreamPlayer ErrorSound => _errorSound ??= GetNode<AudioStreamPlayer>("ErrorSound");
#endregion
#region Helper Properties and Methods
    private ImmutableHashSet<Unit> _zoneUnits = ImmutableHashSet<Unit>.Empty;

    private RangeOverlay _zoneOverlay = null;
    private Label _turnLabel = null;

    private RangeOverlay ZoneOverlay => _zoneOverlay ??= GetNode<RangeOverlay>("ZoneOverlay");
    private Label TurnLabel => _turnLabel = GetNode<Label>("%TurnLabel");

    /// <summary>Units to include in the local unit zones. Updates the highlighted squares when set.</summary>
    private ImmutableHashSet<Unit> ZoneUnits
    {
        get => _zoneUnits;
        set
        {
            _zoneUnits = value;
            UpdateDangerZones();
        }
    }

    /// <summary>
    /// If the <see cref="Object.Cursor"/> isn't in the specified cell, move it to (the center of) that cell. During mouse control, this is done smoothly
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
            if (!rect.HasPoint(Grid.GetGlobalMousePosition()))
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

    /// <summary>Update the displayed danger zones to reflect the current positions of the enemy <see cref="Unit"/>s.</summary>
    private void UpdateDangerZones()
    {
        // Update local danger zone
        IEnumerable<Unit> enemies = ZoneUnits.Where((u) => !StartingArmy.AlliedTo(u));
        IEnumerable<Unit> allies = ZoneUnits.Where(StartingArmy.AlliedTo);
        if (enemies.Any())
            ZoneOverlay[LocalDangerZone] = enemies.SelectMany((u) => u.AttackableCells(u.TraversableCells())).ToImmutableHashSet();
        else
            ZoneOverlay[LocalDangerZone] = ImmutableHashSet<Vector2I>.Empty;
        if (allies.Any())
            ZoneOverlay[AllyTraversable] = allies.SelectMany((u) => u.TraversableCells()).ToImmutableHashSet();
        else
            ZoneOverlay[AllyTraversable] = ImmutableHashSet<Vector2I>.Empty;
        
        // Update global danger zone
        if (ShowGlobalDangerZone)
            ZoneOverlay[GlobalDanger] = GetChildren().OfType<Army>()
                .Where((a) => !a.AlliedTo(StartingArmy))
                .SelectMany((a) => (IEnumerable<Unit>)a)
                .SelectMany((u) => u.AttackableCells(u.TraversableCells())).ToImmutableHashSet();
        else
            ZoneOverlay[GlobalDanger] = ImmutableHashSet<Vector2I>.Empty;
    }

    /// <summary>Update the UI turn counter for the current turn and change its color to match the army.</summary>
    private void UpdateTurnCounter()
    {
        TurnLabel.AddThemeColorOverride("font_color", _armies.Current.Color);
        TurnLabel.Text = $"Turn {Turn}: {_armies.Current.Name}";
    }
#endregion
#region Exports
    private int _turn = 1;
    private bool _showGlobalZone = false;

    /// <summary>
    /// <see cref="Army"/> that gets the first turn and is controlled by the player. If null, use the first <see cref="Army"/>
    /// in the child list. After that, go down the child list in order, wrapping when at the end.
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
                _turnLabel.Text = $"Turn {_turn}: {_armies.Current.Name}";
        }
    }

    /// <summary>Whether or not to show the global danger zone relative to <see cref="StartingArmy"/>.</summary>
    [Export] public bool ShowGlobalDangerZone
    {
        get => _showGlobalZone;
        set
        {
            _showGlobalZone = value;
            if (!Engine.IsEditorHint())
                UpdateDangerZones();
        }
    }

    /// <summary>Action to toggle the global danger zone.</summary>
    [Export] public InputActionReference ToggleGlobalDangerZoneAction = new();

    /// <summary>Map cancel selection action reference (distinct from menu back/cancel).</summary>
    [ExportGroup("Cursor Actions")]
    [Export] public InputActionReference CancelAction = new();

    /// <summary>Map "previous" action, which cycles the cursor to the previous unit in the same army or action target, depending on state.</summary>
    [ExportGroup("Cursor Actions")]
    [Export] public InputActionReference PreviousAction = new();

    /// <summary>Map "next" action, which cycles the cursor to the next unit in the same army or action target, depending on state.</summary>
    [ExportGroup("Cursor Actions")]
    [Export] public InputActionReference NextAction = new();
#endregion
#region Idle State
    private Timer _turnAdvance = null;
    private Timer TurnAdvance => _turnAdvance = GetNode<Timer>("TurnAdvance");

    /// <summary>Deselect or deactivate the selected <see cref="Unit"/> and clean up after finishing actions.</summary>
    /// <param name="done">Whether or not the <see cref="Unit"/> completed its action (so it should be deactivated).</param>
    public void OnToIdleTaken(bool done)
    {
        if (done)
        {
            _selected.Finish();

            // Switch to the next army
            if (!((IEnumerable<Unit>)_armies.Current).Any((u) => u.Active))
                TurnAdvance.Start();
        }
        else
            _selected.Deselect();
        _selected = null;

        CancelHint.Visible = false;
    }

    /// <summary>Update the UI when re-entering idle.</summary>
    public void OnIdleEntered() => OnIdleCursorMoved(Cursor.Cell);

    /// <summary>
    /// Handle events that might occur during idle <see cref="State"/>.
    /// - select: if the cursor is over a <see cref="Unit"/> enemy to the player during the player's turn, toggle its attack range in the local danger zone
    /// - cancel: if the cursor is over a <see cref="Unit"/> enemy to the player during the player's turn, remove its attack range from the local danger zone
    /// </summary>
    /// <param name="event">Name of the event.</param>
    public void OnIdleEventReceived(StringName @event)
    {
        if (_armies.Current == StartingArmy && Grid.Occupants.GetValueOrDefault(Cursor.Cell) is Unit unit && !StartingArmy.Contains(unit))
        {
            if (@event == SelectEvent)
            {
                if (ZoneUnits.Contains(unit))
                    ZoneUnits = ZoneUnits.Remove(unit);
                else
                    ZoneUnits = ZoneUnits.Add(unit);
            }
            else if (@event == CancelEvent && ZoneUnits.Contains(unit))
                ZoneUnits = ZoneUnits.Remove(unit);
        }
    }

    /// <summary>
    /// Advance the turn cycle:
    /// - Go to the next <see cref="Army"/>
    /// - If returning to <see cref="StartingArmy"/>, increment <see cref="Turn"/>
    /// </summary>
    public void OnTurnAdvance()
    {
        // Refresh all the units in the current army so they aren't gray anymore and are animated
        foreach (Unit unit in (IEnumerable<Unit>)_armies.Current)
            unit.Refresh();

        if (_armies.MoveNext() && _armies.Current == StartingArmy)
            Turn++;
        
        UpdateTurnCounter();
    }

    /// <summary>
    /// When the <see cref="Object.Cursor"/> moves over a <see cref="Unit"/> while in idle <see cref="State"/>, display that <see cref="Unit"/>'s
    /// action ranges, but clear them when it moves off.
    /// </summary>
    /// <param name="cell">Cell the <see cref="Object.Cursor"/> moved into.</param>
    public void OnIdleCursorMoved(Vector2I cell)
    {
        ActionOverlay.Clear();

        if (_armies.Current == StartingArmy && Grid.Occupants.GetValueOrDefault(cell) is Unit hovered)
        {
            ActionRanges actionable = hovered.ActionRanges().WithOccupants(
                Grid.Occupants.Select((e) => e.Value).OfType<Unit>().Where((u) => u.Affiliation.AlliedTo(hovered)),
                Grid.Occupants.Select((e) => e.Value).OfType<Unit>().Where((u) => !u.Affiliation.AlliedTo(hovered))
            );
            ActionOverlay.UsedCells = actionable.Exclusive().ToDictionary();
        }
    }

    /// <summary>
    /// Cycle the <see cref="Object.Cursor"/> between units in the same army using <see cref="PreviousAction"/> and <see cref="NextAction"/>
    /// while nothing is selected.
    /// </summary>
    public void OnIdleInput(InputEvent @event)
    {
        if (Grid.Occupants.GetValueOrDefault(Cursor.Cell) is Unit unit)
        {
            if (@event.IsActionReleased(PreviousAction))
            {
                Unit prev = unit.Affiliation.Previous(unit);
                if (prev is not null)
                    Cursor.Cell = prev.Cell;
            }
            if (@event.IsActionReleased(NextAction))
            {
                Unit next = unit.Affiliation.Next(unit);
                if (next is not null)
                    Cursor.Cell = next.Cell;
            }
        }
    }

    /// <summary>Choose a selected <see cref="Unit"/>.</summary>
    public void OnIdleToSelectedTaken() => _selected = Grid.Occupants[Cursor.Cell] as Unit;
#endregion
#region Unit Selected State
    private ActionRanges _actionable = new();

    /// <summary>Display the total movement, attack, and support ranges of the selected <see cref="Unit"/> and begin drawing the path arrow for it to move on.</summary>
    public void OnSelectedEntered()
    {
        _selected.Select();
        _initialCell = _selected.Cell;

        // Compute move/attack/support ranges for selected unit
        _actionable = _selected.ActionRanges().WithOccupants(
            Grid.Occupants.Select((e) => e.Value).OfType<Unit>().Where((u) => u.Affiliation.AlliedTo(_selected)),
            Grid.Occupants.Select((e) => e.Value).OfType<Unit>().Where((u) => !u.Affiliation.AlliedTo(_selected))
        );
        _path = Path.Empty(Grid, _actionable.Traversable).Add(_selected.Cell);
        Cursor.SoftRestriction = _actionable.Traversable.ToHashSet();

        ActionOverlay.UsedCells = _actionable.Exclusive().ToDictionary();
        CancelHint.Visible = true;

        // If the camera isn't zoomed out enough to show the whole range, zoom out so it does
        Rect2? zoomRect = ActionOverlay.GetEnclosingRect(Grid);
        if (zoomRect is not null)
        {
            Vector2 zoomTarget = Grid.GetViewportRect().Size/zoomRect.Value.Size;
            zoomTarget = Vector2.One*Mathf.Min(zoomTarget.X, zoomTarget.Y);
            if (Camera.Zoom > zoomTarget)
                Camera.PushZoom(zoomTarget);
        }
    }

    /// <summary>
    /// While selecting a path, moving the <see cref="Object.Cursor"/> over a targetable <see cref="Unit"/> computes a <see cref="Path"/>
    /// to space that can target it, preferring ending on further spaces.
    /// </summary>
    /// <param name="cell">Cell the <see cref="Object.Cursor"/> moved into.</param>
    public void OnSelectedCursorMoved(Vector2I cell)
    {
        _target = null;

        if (_actionable.Traversable.Contains(cell))
            PathOverlay.Path = (_path = _path.Add(cell).Clamp(_selected.MoveRange)).ToList();
        else if (Grid.Occupants.GetValueOrDefault(cell) is Unit target)
        {
            IEnumerable<Vector2I> sources = Array.Empty<Vector2I>();
            if (target != _selected && _armies.Current.AlliedTo(target) && _actionable.Supportable.Contains(cell))
                sources = _selected.SupportableCells(cell).Where(_actionable.Traversable.Contains);
            else if (!_armies.Current.AlliedTo(target) && _actionable.Attackable.Contains(cell))
                sources = _selected.AttackableCells(cell).Where(_actionable.Traversable.Contains);
            sources = sources.Where((c) => !Grid.Occupants.ContainsKey(c));
            if (sources.Any())
            {
                _target = target;
                if (!sources.Contains(_path[^1]))
                    PathOverlay.Path = (_path = sources.Select((c) => _path.Add(c).Clamp(_selected.MoveRange)).OrderBy((p) => p[^1].DistanceTo(_path[^1])).OrderByDescending((p) => p[^1].DistanceTo(cell)).First()).ToList();
            }
        }
    }

    /// <summary>
    /// If the cursor tries to select a cell that contains an allied, non-selected unit, don't do anything but play a sound to indicate that's
    /// not allowed.
    /// </summary>
    /// <param name="cell">Cell being selected.</param>
    public void OnSelectedCellSelected(Vector2I cell)
    {
        if (_actionable.Traversable.Contains(cell))
        {
            Unit highlighted = Grid.Occupants.GetValueOrDefault(cell) as Unit;
            if (highlighted != _selected && _armies.Current.AlliedTo(highlighted))
                ErrorSound.Play();
        }
    }

    /// <summary>Clean up when exiting selected <see cref="State"/>.</summary>
    public void OnSelectedExited()
    {
        // Clear out movement/action ranges
        _actionable = _actionable.Clear();
        Cursor.SoftRestriction.Clear();
        PathOverlay.Clear();
        ActionOverlay.Clear();
        
        // Restore the camera zoom back to what it was before a unit was selected
        if (Camera.HasZoomMemory())
            Camera.PopZoom();
    }
#endregion
#region Moving State
    private Vector4 _prevDeadzone = Vector4.Zero;
    private BoundedNode2D _prevCameraTarget = null;

    /// <summary>When the <see cref="Unit"/> finishes moving, move to the next <see cref="State"/>.</summary>
    public void OnUnitDoneMoving()
    {
        _selected.DoneMoving -= OnUnitDoneMoving;
        _state.SendEvent(DoneEvent);
    }

    /// <summary>Begin moving the selected <see cref="Unit"/> and then wait for it to finish moving.</summary>
    public void OnMovingEntered()
    {
        // Move the unit and delete the pathfinder as we don't need it anymore
        Grid.Occupants.Remove(_selected.Cell);
        _selected.MoveAlong(_path);
        _selected.DoneMoving += OnUnitDoneMoving;
        Grid.Occupants[_selected.Cell] = _selected;

        // Track the unit as it's moving
        _prevDeadzone = new(Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight);
        (Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight) = Vector4.Zero;
        _prevCameraTarget = Camera.Target;
        Camera.Target = _selected.MotionBox;
    }

    /// <summary>When done moving, restore the <see cref="Camera2DBrain">camera</see> target (most likely to the cursor) and update danger zones.</summary>
    public void OnMovingExited()
    {
        (Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight) = _prevDeadzone;
        Camera.Target = _prevCameraTarget;
        _path = null;
        UpdateDangerZones();
    }
#endregion
#region Targeting State
    /// <summary>Compute the attack and support ranges of the selected <see cref="Unit"/> from its location.</summary>
    public void OnTargetingEntered()
    {
        // Show the unit's attack/support ranges
        ActionRanges actionable = new(
            _selected.AttackableCells().Where((c) => !(Grid.Occupants.GetValueOrDefault(c) as Unit)?.Affiliation.AlliedTo(_selected) ?? false),
            _selected.SupportableCells().Where((c) => (Grid.Occupants.GetValueOrDefault(c) as Unit)?.Affiliation.AlliedTo(_selected) ?? false)
        );
        ActionOverlay.UsedCells = actionable.ToDictionary();

        // Restrict cursor movement to actionable cells
        Pointer.AnalogTracking = false;
        Cursor.HardRestriction = actionable.Attackable.Union(actionable.Supportable);
        Cursor.Wrap = true;

        // If a target has already been selected (because it was shortcutted during the select state), skip through targeting
        if (_target == null)
            WarpCursor(Cursor.Cell);
        else
            _state.SendEvent(SelectEvent);
    }

    /// <summary>
    /// Cycle the <see cref="Object.Cursor"/> between targets of the same action (attack, support, etc.) using <see cref="PreviousAction"/>
    /// and <see cref="NextAction"/> while choosing targets.
    /// </summary>
    public void OnTargetingInput(InputEvent @event)
    {
        int next = 0;
        if (@event.IsActionReleased(PreviousAction))
            next = -1;
        else if (@event.IsActionReleased(NextAction))
            next = 1;

        if (next != 0)
        {
            Vector2I[] cells = Array.Empty<Vector2I>();
            if (ActionOverlay[ActionRanges.AttackableRange].Contains(Cursor.Cell))
                cells = ActionOverlay[ActionRanges.AttackableRange].ToArray();
            else if (ActionOverlay[ActionRanges.SupportableRange].Contains(Cursor.Cell))
                cells = ActionOverlay[ActionRanges.SupportableRange].ToArray();
            else
                GD.PushError("Cursor is not on an actionable cell during targeting");
            
            if (cells.Length > 1)
                Cursor.Cell = cells[(Array.IndexOf(cells, Cursor.Cell) + next + cells.Length) % cells.Length];
        }
    }

    /// <summary>Clean up displayed ranges and restore <see cref="Object.Cursor"/> freedom when exiting targeting <see cref="State"/>.</summary>
    public void OnTargetingExited()
    {
        _target = null;
        PathOverlay.Clear();
        ActionOverlay.Clear();

        Pointer.AnalogTracking = true;
        Cursor.HardRestriction = Cursor.HardRestriction.Clear();
        Cursor.Wrap = false;
    }
#endregion
#region Cancel States
    /// <summary>
    /// Move the selected <see cref="Unit"/> back to its starting position (only does anything when canceling targeting). Then move the
    /// <see cref="Object.Cursor"/> to the <see cref="Unit"/>'s current position, and go back to the previous <see cref="State"/>.
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

    public void OnCancelTargetingExited() => UpdateDangerZones();
#endregion
#region State Independent
    /// <summary>When a cell is selected, act based on what is or isn't in the cell.</summary>
    /// <param name="cell">Coordinates of the cell selection.</param>
    public void OnCellSelected(Vector2I cell)
    {
        if (Grid.CellOf(Pointer.Position) == cell)
            _state.SendEvent(SelectEvent);
        else
            _state.SendEvent(CancelEvent);
    }

    /// <summary>When a <see cref="GridNode"/> is added to a group, update its <see cref="GridNode.Grid"/>.</summary>
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

            Camera.Limits = new(Vector2I.Zero, (Vector2I)(Grid.Size*Grid.CellSize));
            Pointer.World = Cursor.Grid = Grid;
            Pointer.Bounds = Camera.Limits;

            foreach (Unit unit in GetChildren().OfType<IEnumerable<Unit>>().Flatten())
            {
                unit.Grid = Grid;
                unit.Cell = Grid.CellOf(unit.Position);
                unit.Position = Grid.PositionOf(unit.Cell);
                Grid.Occupants[unit.Cell] = unit;
            }

            _armies = GetChildren().OfType<Army>().GetCyclicalEnumerator();
            if (StartingArmy is null)
                StartingArmy = _armies.Current;
            else // Advance the army enumerator until it's pointing at StartingArmy
                while (_armies.Current != StartingArmy)
                    if (!_armies.MoveNext())
                        break;
            UpdateTurnCounter();
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionReleased(CancelAction))
            _state.SendEvent(CancelEvent);

        if (@event.IsActionReleased(ToggleGlobalDangerZoneAction))
        {
            if (Grid.Occupants.GetValueOrDefault(Cursor.Cell) is Unit unit)
                ZoneUnits = ZoneUnits.Contains(unit) ? ZoneUnits.Remove(unit) : ZoneUnits.Add(unit);
            else
                ShowGlobalDangerZone = !ShowGlobalDangerZone;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            _state.ExpressionProperties = _state.ExpressionProperties
                .SetItem(OccupiedProperty, Grid.Occupants.GetValueOrDefault(Cursor.Cell) switch
                {
                    Unit unit when unit == _selected => SelectedOccuiped,
                    Unit unit when _armies.Current == unit.Affiliation => unit.Active ? ActiveAllyOccupied : InActiveAllyOccupied,
                    Unit unit when _armies.Current.AlliedTo(unit.Affiliation) => FriendlyOccuipied,
                    Unit => EnemyOccupied,
                    null => NotOccupied,
                    _ => OtherOccupied
                })
                .SetItem(TargetProperty, Grid.Occupants.GetValueOrDefault(Cursor.Cell) is Unit target &&
                                          ((target != _selected && _armies.Current.AlliedTo(target) && _actionable.Supportable.Contains(Cursor.Cell)) ||
                                           (!_armies.Current.AlliedTo(target) && _actionable.Attackable.Contains(Cursor.Cell))))
                .SetItem(TraversableProperty, _actionable.Traversable.Contains(Cursor.Cell));
        }
    }
#endregion
}