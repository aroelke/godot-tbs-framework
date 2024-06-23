using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Nodes;
using UI;
using UI.Controls.Action;
using UI.Controls.Device;
using UI.HUD;
using Extensions;
using Nodes.StateChart;
using Nodes.StateChart.States;
using Scenes.Level.UI;
using Scenes.Level.Object;
using Scenes.Level.Object.Group;
using Scenes.Level.Map;
using Scenes.Combat.Data;

namespace Scenes.Level;

/// <summary>
/// Manages the setup of and objects inside a level and provides them information about it.  Is "global" in a way in that
/// all objects in the level may be able to see it and request information from it, but each level has its own manager.
/// </summary>
[Tool]
public partial class LevelManager : Node
{
    private readonly NodeCache _cache;
    public LevelManager() : base() => _cache = new(this);

#region Constants
    // State chart events
    private readonly StringName SelectEvent = "select";
    private readonly StringName CancelEvent = "cancel";
    private readonly StringName WaitEvent = "wait";
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
    private Path _path = null;
    private Unit _selected = null, _target = null;
    private IEnumerator<Army> _armies = null;
    private Vector2I? _initialCell = null;

    private Chart StateChart => _cache.GetNode<Chart>("State");
    private Grid Grid => _cache.GetNode<Grid>("Grid");
    private PathOverlay PathOverlay => _cache.GetNode<PathOverlay>("PathOverlay");
    private RangeOverlay ActionOverlay => _cache.GetNode<RangeOverlay>("ActionRangeOverlay");
    private Camera2DBrain Camera => _cache.GetNode<Camera2DBrain>("Camera");
    private Cursor Cursor => _cache.GetNode<Cursor>("Cursor");
    private Pointer Pointer => _cache.GetNode<Pointer>("Pointer");
    private ControlHint CancelHint => _cache.GetNode<ControlHint>("UserInterface/HUD/Hints/CancelHint");
    private AudioStreamPlayer SelectSound => _cache.GetNode<AudioStreamPlayer>("SelectSound");
    private AudioStreamPlayer ErrorSound => _cache.GetNode<AudioStreamPlayer>("ErrorSound");
    private AudioStreamPlayer ZoneOnSound => _cache.GetNode<AudioStreamPlayer>("ZoneOnSound");
    private AudioStreamPlayer ZoneOffSound => _cache.GetNode<AudioStreamPlayer>("ZoneOffSound");
#endregion
#region Helper Properties and Methods
    private ImmutableHashSet<Unit> _zoneUnits = ImmutableHashSet<Unit>.Empty;

    private RangeOverlay ZoneOverlay => _cache.GetNode<RangeOverlay>("ZoneOverlay");
    private Label TurnLabel => _cache.GetNode<Label>("%TurnLabel");

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
    /// over time to maintain consistency with the system pointer, and other inputs are disabled while it moves.
    /// </summary>
    /// <param name="cell">Cell to move the cursor to.</param>
    private async void MoveCursor(Vector2I cell)
    {
        Rect2 rect = Grid.CellRect(cell);
        switch (DeviceManager.Mode)
        {
        case InputMode.Mouse:
            // If the input mode is mouse and the cursor is not on the cell's square, move it there over time
            if (!rect.HasPoint(Grid.GetGlobalMousePosition()))
            {
                Pointer.Fly(Grid.PositionOf(cell) + Grid.CellSize/2, Camera.DeadZoneSmoothTime);
                BoundedNode2D target = Camera.Target;
                Camera.Target = Grid.Occupants[cell];
                StateChart.SendEvent(WaitEvent);
                await ToSignal(Pointer, Pointer.SignalName.FlightCompleted);
                StateChart.SendEvent(DoneEvent);
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

    /// <returns>The audio player that plays the "zone on" or "zone off" sound depending on <paramref name="on"/>.</returns>
    private AudioStreamPlayer ZoneUpdateSound(bool on) => on ? ZoneOnSound : ZoneOffSound;
#endregion
#region Exports
    private int _turn = 1;
    private bool _showGlobalZone = false;

    [Export] public AudioStream BackgroundMusic = null;

    /// <summary>
    /// <see cref="Army"/> that gets the first turn and is controlled by the player. If null, use the first <see cref="Army"/>
    /// in the child list. After that, go down the child list in order, wrapping when at the end.
    /// </summary>
    [ExportGroup("Turn Control")]
    [Export] public Army StartingArmy = null;

    /// <summary>Turn count (including current turn, so it starts at 1).</summary>
    [ExportGroup("Turn Control")]
    [Export] public int Turn
    {
        get => _turn;
        set
        {
            _turn = value;
            if (!Engine.IsEditorHint())
                TurnLabel.Text = $"Turn {_turn}: {_armies.Current.Name}";
        }
    }

    /// <summary>Whether or not to show the global danger zone relative to <see cref="StartingArmy"/>.</summary>
    [ExportGroup("Range Overlay")]
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
    [ExportGroup("Range Overlay")]
    [Export] public InputActionReference ToggleGlobalDangerZoneAction = new();

    /// <summary>Modulate color for the action range overlay to use during idle state to differentiate from the one displayed while selecting a move path.</summary>
    [ExportGroup("Range Overlay")]
    [Export] public Color ActionRangeIdleModulate = new(1, 1, 1, 0.66f);

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
    /// <summary>Update the UI when re-entering idle.</summary>
    public void OnIdleEntered() => ActionOverlay.Modulate = ActionRangeIdleModulate;

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
                ZoneUpdateSound(ZoneUnits.Contains(unit)).Play();
            }
            else if (@event == CancelEvent && ZoneUnits.Contains(unit))
            {
                ZoneUnits = ZoneUnits.Remove(unit);
                ZoneUpdateSound(ZoneUnits.Contains(unit)).Play();
            }
        }
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
            void SelectUnit(Func<Unit, Unit> selector)
            {
                ActionOverlay.Clear();
                Unit selected = selector(unit);
                if (selected is not null)
                    MoveCursor(selected.Cell);
            }

            if (@event.IsActionPressed(PreviousAction))
                SelectUnit(unit.Affiliation.Previous);
            if (@event.IsActionPressed(NextAction))
                SelectUnit(unit.Affiliation.Next);
        }
    }

    /// <summary>Choose a selected <see cref="Unit"/>.</summary>
    public void OnIdleToSelectedTaken()
    {
        ActionOverlay.Modulate = Colors.White;
        _selected = Grid.Occupants[Cursor.Cell] as Unit;
    }
#endregion
#region Unit Selected State
    private ActionRanges _actionable = new();

    /// <summary>Set the selected <see cref="Unit"/> to its idle state and the deselect it.</summary>
    private void DeselectUnit()
    {
        _selected.Deselect();
        _selected = null;
        CancelHint.Visible = false;
    }

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
            PathOverlay.Path = (_path = _path.Add(cell).Clamp(_selected.Stats.Move)).ToList();
        else if (Grid.Occupants.GetValueOrDefault(cell) is Unit target)
        {
            IEnumerable<Vector2I> sources = Array.Empty<Vector2I>();
            if (target != _selected && _armies.Current.AlliedTo(target) && _actionable.Supportable.Contains(cell))
                sources = _selected.SupportableCells(cell).Where(_actionable.Traversable.Contains);
            else if (!_armies.Current.AlliedTo(target) && _actionable.Attackable.Contains(cell))
                sources = _selected.AttackableCells(cell).Where(_actionable.Traversable.Contains);
            sources = sources.Where((c) => !Grid.Occupants.ContainsKey(c) || Grid.Occupants[c] == _selected);
            if (sources.Any())
            {
                _target = target;
                if (!sources.Contains(_path[^1]))
                    PathOverlay.Path = (_path = sources.Select((c) => _path.Add(c).Clamp(_selected.Stats.Move)).OrderBy(
                        (p) => new Vector2I(-p[^1].DistanceTo(cell), p[^1].DistanceTo(_path[^1])),
                        (a, b) => a < b ? -1 : a > b ? 1 : 0
                    ).First()).ToList();
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

    /// <summary>Deselect the selected <see cref="Unit"/> and clean up after canceling actions.</summary>
    public void OnSelectedToIdleTaken()
    {
        DeselectUnit();
        PathOverlay.Clear();
    }

    /// <summary>Move the <see cref="Object.Cursor"/> back to the selected <see cref="Unit"/> and then deselect it.</summary>
    public void OnSelectedCanceled()
    {
        _initialCell = null;
        PathOverlay.Clear();
        Callable.From<Vector2I>(MoveCursor).CallDeferred(_selected.Cell);
        DeselectUnit();
    }

    /// <summary>Clean up overlays when movement destination is chosen.</summary>
    public void OnDestinationChosen()
    {
        // Clear out movement/action ranges
        _actionable = _actionable.Clear();
        Cursor.SoftRestriction.Clear();
        PathOverlay.Clear();
        ActionOverlay.Clear();
    }

    public void OnSelectedExited()
    {
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
        Callable.From(() => StateChart.SendEvent(DoneEvent)).CallDeferred();
    }

    /// <summary>Begin moving the selected <see cref="Unit"/> and then wait for it to finish moving.</summary>
    public void OnMovingEntered()
    {
        // Track the unit as it's moving
        _prevDeadzone = new(Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight);
        (Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight) = Vector4.Zero;
        _prevCameraTarget = Camera.Target;
        Camera.Target = _selected.MotionBox;

        // Move the unit and delete the pathfinder as we don't need it anymore
        Grid.Occupants.Remove(_selected.Cell);
        _selected.DoneMoving += OnUnitDoneMoving;
        Grid.Occupants[_path[^1]] = _selected;
        _selected.MoveAlong(_path); // must be last in case it fires right away
    }

    /// <summary>Press the cancel button during movement to skip to the end.</summary>
    public void OnMovingInput(InputEvent @event)
    {
        if (@event.IsActionPressed(CancelAction))
            _selected.SkipMoving();
    }

    /// <summary>When done moving, restore the <see cref="Camera2DBrain">camera</see> target (most likely to the cursor) and update danger zones.</summary>
    public void OnMovingExited()
    {
        (Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight) = _prevDeadzone;
        Camera.Target = _prevCameraTarget;
        _path = null;
        UpdateDangerZones();

        // If a target has already been selected (because it was shortcutted during the select state), skip through targeting
        if (_target is not null)
            Callable.From<Unit>(OnTargetSelected).CallDeferred(_target);
        else
        {
            // Show the unit's attack/support ranges
            ActionRanges actionable = new(
                _selected.AttackableCells().Where((c) => !(Grid.Occupants.GetValueOrDefault(c) as Unit)?.Affiliation.AlliedTo(_selected) ?? false),
                _selected.SupportableCells().Where((c) => (Grid.Occupants.GetValueOrDefault(c) as Unit)?.Affiliation.AlliedTo(_selected) ?? false)
            );
            ActionOverlay.UsedCells = actionable.ToDictionary();

            // Restrict cursor movement to actionable cells
            if (_target is null)
            {
                Pointer.AnalogTracking = false;
                Cursor.HardRestriction = actionable.Attackable.Union(actionable.Supportable);
                Cursor.Wrap = true;
                Callable.From(() => MoveCursor(Cursor.Cell)).CallDeferred();
            }
        }
    }
#endregion
#region Targeting State
    private ImmutableList<CombatAction> _combatResults = null;

    /// <summary>
    /// Cycle the <see cref="Object.Cursor"/> between targets of the same action (attack, support, etc.) using <see cref="PreviousAction"/>
    /// and <see cref="NextAction"/> while choosing targets.
    /// </summary>
    public void OnTargetingInput(InputEvent @event)
    {
        int next = 0;
        if (@event.IsActionPressed(PreviousAction))
            next = -1;
        else if (@event.IsActionPressed(NextAction))
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
                MoveCursor(cells[(Array.IndexOf(cells, Cursor.Cell) + next + cells.Length) % cells.Length]);
        }
    }

    /// <summary>Move the selected <see cref="Unit"/> and <see cref="Object.Cursor"/> back to the cell the unit was at before it moved.</summary>
    public void OnTargetingCanceled()
    {
        // Move the selected unit back to its original cell
        Grid.Occupants.Remove(_selected.Cell);
        _selected.Cell = _initialCell.Value;
        _selected.Position = Grid.PositionOf(_selected.Cell);
        Grid.Occupants[_selected.Cell] = _selected;
        _initialCell = null;
        Callable.From<Vector2I>(MoveCursor).CallDeferred(_selected.Cell);

        _target = null;
        UpdateDangerZones();
        OnTargetingExited();
    }

    /// <summary>If a target is selected, begin combat fighting that target.  Otherwise, just end the selected <see cref="Unit"/>'s turn.</summary>
    /// <param name="cell">Cell being selected.</param>
    public void OnTargetingCellSelected(Vector2I cell)
    {
        if (Cursor.HardRestriction.Any())
        {
            if (Grid.CellOf(Pointer.Position) == cell && Grid.Occupants[cell] != _selected)
            {
                if (Grid.Occupants[cell] is Unit target)
                    OnTargetSelected(target);
                else
                    StateChart.SendEvent(DoneEvent);
            }
        }
        else
        {
            SelectSound.Play();
            StateChart.SendEvent(DoneEvent);
        }
    }

    /// <summary>Begin combat when a target is selected.</summary>
    /// <param name="target">Unit targeted for combat or support.</param>
    public void OnTargetSelected(Unit target)
    {
        _target = target;
        StateChart.SendEvent(DoneEvent);
        SceneManager.BeginCombat(_selected, target, _combatResults = CombatCalculations.CombatResults(_selected, target));
    }

    /// <summary>Clean up displayed ranges and restore <see cref="Object.Cursor"/> freedom when exiting targeting <see cref="State"/>.</summary>
    public void OnTargetingExited()
    {
        ActionOverlay.Clear();
        Pointer.AnalogTracking = true;
        Cursor.HardRestriction = Cursor.HardRestriction.Clear();
        Cursor.Wrap = false;
    }
#endregion
#region In Combat
    /// <summary>Update the map to reflect combat results when it's added back to the tree.</summary>
    public void OnCombatEnteredTree()
    {
        _selected.Health.Value -= CombatCalculations.TotalDamage(_selected, _combatResults);
        _target.Health.Value -= CombatCalculations.TotalDamage(_target, _combatResults);
        _target = null;
        _combatResults = null;
        ActionOverlay.Clear();
        SceneManager.Singleton.TransitionCompleted += OnTransitionedFromCombat;
    }

    /// <summary>Finish waiting once the transition back has completed.</summary>
    public void OnTransitionedFromCombat()
    {
        StateChart.SendEvent(DoneEvent);
        SceneManager.Singleton.TransitionCompleted -= OnTransitionedFromCombat;
    }
#endregion
#region Turn Advancing
    private Timer TurnAdvance => _cache.GetNode<Timer>("TurnAdvance");

    /// <summary>Update the UI turn counter for the current turn and change its color to match the army.</summary>
    private void UpdateTurnCounter()
    {
        TurnLabel.AddThemeColorOverride("font_color", _armies.Current.Color);
        TurnLabel.Text = $"Turn {Turn}: {_armies.Current.Name}";
    }

    /// <summary>
    /// Clean up after finishing a unit's action, then go to the next army if it was the last available unit in the current army, incrementing the turn
    /// counter when going back to <see cref="StartingArmy"/>.
    /// </summary>
    public async void OnTurnAdvancingEntered()
    {
        // If the selected unit died, there might not be one anymore
        if (_selected is not null)
        {
            _selected.Finish();
            _selected = null;
        }
        CancelHint.Visible = false;

        bool advance = !((IEnumerable<Unit>)_armies.Current).Any((u) => u.Active);
        if (advance)
        {
            TurnAdvance.Start();
            await ToSignal(TurnAdvance, Timer.SignalName.Timeout);

            // Refresh all the units in the current army so they aren't gray anymore and are animated
            foreach (Unit unit in (IEnumerable<Unit>)_armies.Current)
                unit.Refresh();

            do
            {
                if (_armies.MoveNext() && _armies.Current == StartingArmy)
                    Turn++;
            } while (!((IEnumerable<Unit>)_armies.Current).Any());
            UpdateTurnCounter();
        }
        Callable.From(() => {
            StateChart.SendEvent(DoneEvent);
            if (advance)
                Callable.From<Vector2I>(MoveCursor).CallDeferred(((IEnumerable<Unit>)_armies.Current).First().Cell);
        }).CallDeferred();
    }
#endregion
#region State Independent
    /// <summary>When a cell is selected, act based on what is or isn't in the cell.</summary>
    /// <param name="cell">Coordinates of the cell selection.</param>
    public void OnCellSelected(Vector2I cell)
    {
        if (Grid.CellOf(Pointer.Position) == cell)
            StateChart.SendEvent(SelectEvent);
        else
            StateChart.SendEvent(CancelEvent);
    }

    /// <summary>When a <see cref="GridNode"/> is added to a group, update its <see cref="GridNode.Grid"/>.</summary>
    /// <param name="child"></param>
    public void OnChildEnteredGroup(Node child)
    {
        if (child is GridNode gd)
            gd.Grid = Grid;
    }

    public void OnUnitDefeated(Unit defeated)
    {
        if (_selected == defeated)
            _selected = null;
        defeated.Die();
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

        // Make sure there's background music
        if (BackgroundMusic is null)
            warnings.Add("Background music hasn't been added. Whatever's playing will stop.");

        return warnings.ToArray();
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
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
            UnitEvents.Singleton.UnitDefeated += OnUnitDefeated;

            _armies = GetChildren().OfType<Army>().GetCyclicalEnumerator();
            if (StartingArmy is null)
                StartingArmy = _armies.Current;
            else // Advance the army enumerator until it's pointing at StartingArmy
                while (_armies.Current != StartingArmy)
                    if (!_armies.MoveNext())
                        break;
            UpdateTurnCounter();

            MusicController.ResetPlayback();
            MusicController.PlayTrack(BackgroundMusic);
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionPressed(CancelAction))
            StateChart.SendEvent(CancelEvent);

        if (@event.IsActionPressed(ToggleGlobalDangerZoneAction))
        {
            if (Grid.Occupants.GetValueOrDefault(Cursor.Cell) is Unit unit)
            {
                ZoneUnits = ZoneUnits.Contains(unit) ? ZoneUnits.Remove(unit) : ZoneUnits.Add(unit);
                ZoneUpdateSound(ZoneUnits.Contains(unit)).Play();
            }
            else
            {
                ShowGlobalDangerZone = !ShowGlobalDangerZone;
                ZoneUpdateSound(ShowGlobalDangerZone).Play();
            }
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            StateChart.ExpressionProperties = StateChart.ExpressionProperties
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