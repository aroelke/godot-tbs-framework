using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsTemplate.Data;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;
using TbsTemplate.Nodes;
using TbsTemplate.Scenes.Combat.Data;
using TbsTemplate.UI.Controls.Action;
using TbsTemplate.UI;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.Scenes.Level;

/// <summary>
/// Manages the setup of and objects inside a level and provides them information about it.  Is "global" in a way in that
/// all objects in the level may be able to see it and request information from it, but each level has its own manager.
/// </summary>
[SceneTree, Tool]
public partial class LevelManager : Node
{
#region Constants
    // State chart events
    private readonly StringName SelectEvent = "select";
    private readonly StringName CancelEvent = "cancel";
    private readonly StringName SkipEvent = "skip";
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

    // Overlay Layer names
    private readonly StringName MoveLayer = "MoveLayer";
    private readonly StringName AttackLayer = "AttackLayer";
    private readonly StringName SupportLayer = "SupportLayer";
    private readonly StringName AllyTraversable = "TraversableZone";
    private readonly StringName LocalDangerZone = "LocalDangerZone";
    private readonly StringName GlobalDanger = "GlobalDangerZone";
#endregion
#region Declarations
    private Path _path = null;
    private Unit _selected = null, _target = null;
    private IEnumerator<Army> _armies = null;
    private Vector2I? _initialCell = null;
    private BoundedNode2D _prevCameraTarget = null;

    private Grid Grid = null;
#endregion
#region Helper Properties and Methods
    private ImmutableHashSet<Unit> _zoneUnits = [];

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

    private Vector2 MenuPosition(Rect2 rect, Vector2 size)
    {
        Rect2 viewportRect = Grid.GetGlobalTransformWithCanvas()*rect;
        float viewportCenter = GetViewport().GetVisibleRect().Position.X + GetViewport().GetVisibleRect().Size.X/2;
        return new(
            viewportCenter - viewportRect.Position.X < viewportRect.Size.X/2 ? viewportRect.Position.X - size.X : viewportRect.End.X,
            Mathf.Clamp(viewportRect.Position.Y - (size.Y - viewportRect.Size.Y)/2, 0, GetViewport().GetVisibleRect().Size.Y - size.Y)
        );
    }

    private ContextMenu ShowMenu(Rect2 rect, IEnumerable<(StringName name, Action action)> options)
    {

        ContextMenu menu = ContextMenu.Instantiate(options.Select((o) => o.name), true);
        UserInterface.AddChild(menu);
        menu.Visible = false;
        foreach ((StringName name, Action action) in options)
            menu[name].Pressed += action;
        menu.MenuClosed += () => {
            Cursor.Resume();
            Camera.Target = _prevCameraTarget;
            _prevCameraTarget = null;
        };

        Cursor.Halt(hide:true);
        _prevCameraTarget = Camera.Target;
        Camera.Target = null;

        Callable.From<ContextMenu, Rect2>((m, r) => {
            m.Visible = true;
            if (DeviceManager.Mode != InputMode.Mouse)
                m.GrabFocus();
            m.Position = MenuPosition(r, m.Size);
        }).CallDeferred(menu, rect);

        return menu;
    }

    private ContextMenu ShowMenu(Rect2 rect, params (StringName, Action)[] options) => ShowMenu(rect, (IEnumerable<(StringName, Action)>)options);

    /// <summary>Update the displayed danger zones to reflect the current positions of the enemy <see cref="Unit"/>s.</summary>
    private void UpdateDangerZones()
    {
        Faction player = GetChildren().OfType<Army>().Where((a) => a.Faction.IsPlayer).First().Faction;

        // Update local danger zone
        IEnumerable<Unit> enemies = ZoneUnits.Where((u) => !player.AlliedTo(u));
        IEnumerable<Unit> allies = ZoneUnits.Where(player.AlliedTo);
        if (enemies.Any())
            ZoneLayers[LocalDangerZone] = enemies.SelectMany((u) => u.AttackableCells(u.TraversableCells())).ToImmutableHashSet();
        else
            ZoneLayers.Clear(LocalDangerZone);
        if (allies.Any())
            ZoneLayers[AllyTraversable] = allies.SelectMany((u) => u.TraversableCells()).ToImmutableHashSet();
        else
            ZoneLayers.Clear(AllyTraversable);
        
        // Update global danger zone
        if (ShowGlobalDangerZone)
            ZoneLayers[GlobalDanger] = GetChildren().OfType<Army>()
                .Where((a) => !a.Faction.AlliedTo(player))
                .SelectMany(static (a) => (IEnumerable<Unit>)a)
                .SelectMany(static (u) => u.AttackableCells(u.TraversableCells())).ToImmutableHashSet();
        else
            ZoneLayers.Clear(GlobalDanger);
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

    /// <summary>Whether or not to show the global danger zone relative to the player's <see cref="Army"/>.</summary>
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

    /// <summary>Modulate color for the action range overlay to use during idle state to differentiate from the one displayed while selecting a move path.</summary>
    [ExportGroup("Range Overlay")]
    [Export] public Color ActionRangeIdleModulate = new(1, 1, 1, 0.66f);
#endregion
#region Idle State
    /// <summary>Update the UI when re-entering idle.</summary>
    public void OnIdleEntered() => ActionLayers.Modulate = ActionRangeIdleModulate;

    /// <summary>
    /// Handle events that might occur during idle <see cref="Nodes.StateChart.States.State"/>.
    /// - select: if the cursor is over a <see cref="Unit"/> enemy to the player during the player's turn, toggle its attack range in the local danger zone
    /// - cancel: if the cursor is over a <see cref="Unit"/> enemy to the player during the player's turn, remove its attack range from the local danger zone
    /// </summary>
    /// <param name="event">Name of the event.</param>
    public void OnIdleEventReceived(StringName @event)
    {
        if (_armies.Current.Faction.IsPlayer && Grid.Occupants.GetValueOrDefault(Cursor.Cell) is Unit unit && !_armies.Current.Contains(unit))
        {
            if (@event == SelectEvent)
            {
#pragma warning disable CA1868 // Contains is necessary since the unit has to be added if it isn't in the set and removed if it is
                ZoneUnits = ZoneUnits.Contains(unit) ? ZoneUnits.Remove(unit) : ZoneUnits.Add(unit);
#pragma warning restore CA1868 // Unnecessary call to 'Contains(item)'

                ZoneUpdateSound(ZoneUnits.Contains(unit)).Play();
            }
            else if (@event == CancelEvent && ZoneUnits.Contains(unit))
            {
                ZoneUnits = ZoneUnits.Remove(unit);
                ZoneUpdateSound(ZoneUnits.Contains(unit)).Play();
            }
        }
    }

    /// <summary>Clear the displayed action ranges when moving the <see cref="Object.Cursor"/> to a new cell while in idle <see cref="Nodes.StateChart.States.State"/>.</summary>
    /// <param name="cell">Cell the <see cref="Object.Cursor"/> moved to.</param>
    public void OnIdleCursorMoved(Vector2I cell) => ActionLayers.Clear();

    /// <summary>
    /// When the <see cref="Object.Cursor"/> moves over a <see cref="Unit"/> while in idle <see cref="Nodes.StateChart.States.State"/>, display that <see cref="Unit"/>'s
    /// action ranges.
    /// </summary>
    /// <param name="cell">Cell the <see cref="Object.Cursor"/> moved into.</param>
    public void OnIdleCursorEnteredCell(Vector2I cell)
    {
        if (_armies.Current.Faction.IsPlayer && Grid.Occupants.GetValueOrDefault(cell) is Unit hovered)
            (ActionLayers[MoveLayer], ActionLayers[AttackLayer], ActionLayers[SupportLayer]) = hovered.ActionRanges();
    }

    /// <summary>When the pointer stops moving, display the action range of the unit the cursor is over.</summary>
    /// <param name="position">Position the pointer stopped over.</param>
    public void OnIdlePointerStopped(Vector2 position) => OnIdleCursorEnteredCell(Grid.CellOf(position));

    /// <summary>
    /// Cycle the <see cref="Object.Cursor"/> between units in the same army using <see cref="InputActions.Previous"/> and <see cref="InputActions.Next"/>
    /// while nothing is selected.
    /// </summary>
    public void OnIdleInput(InputEvent @event)
    {
        if (Grid.Occupants.GetValueOrDefault(Cursor.Cell) is Unit unit)
        {
            void SelectUnit(Func<Unit, Unit> selector)
            {
                ActionLayers.Clear();
                Unit selected = selector(unit);
                if (selected is not null)
                    Cursor.Cell = selected.Cell;
            }

            Army army = GetChildren().OfType<Army>().Where((a) => a.Contains(unit)).First();
            if (@event.IsActionPressed(InputActions.Previous))
                SelectUnit(army.Previous);
            if (@event.IsActionPressed(InputActions.Next))
                SelectUnit(army.Next);
        }
    }

    /// <summary>Open the map menu and wait for an item to be selected.</summary>
    public void OnIdleEmptyCellSelected(Vector2I cell)
    {
        void Cancel()
        {
            State.SendEvent(DoneEvent);
            CancelSound.Play();
        }

        SelectSound.Play();
        State.SendEvent(WaitEvent);
        ShowMenu(new() { Position = Pointer.Position, Size = Vector2.Zero },
            ("End Turn", () => {
                State.SendEvent(DoneEvent);
                foreach (Unit unit in (IEnumerable<Unit>)_armies.Current)
                    unit.Finish();
                State.SendEvent(SkipEvent);
                SelectSound.Play();
            }),
            ("Quit Game", () => GetTree().Quit()),
            ("Cancel", Cancel)
        ).MenuCanceled += Cancel;
    }

    /// <summary>Choose a selected <see cref="Unit"/>.</summary>
    public void OnIdleToSelectedTaken()
    {
        ActionLayers.Modulate = Colors.White;
        _selected = Grid.Occupants[Cursor.Cell] as Unit;
    }
#endregion
#region Unit Selected State
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
        (ActionLayers[MoveLayer], ActionLayers[AttackLayer], ActionLayers[SupportLayer]) = _selected.ActionRanges();
        _path = Path.Empty(Grid, ActionLayers[MoveLayer]).Add(_selected.Cell);
        Cursor.SoftRestriction = [.. ActionLayers[MoveLayer]];
        CancelHint.Visible = true;

        // If the camera isn't zoomed out enough to show the whole range, zoom out so it does
        Rect2? zoomRect = Grid.EnclosingRect(ActionLayers.Union());
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

        if (ActionLayers[MoveLayer].Contains(cell))
            PathLayer.Path = _path = _path.Add(cell).Clamp(_selected.Stats.Move);
        else if (Grid.Occupants.GetValueOrDefault(cell) is Unit target)
        {
            IEnumerable<Vector2I> sources = [];
            if (target != _selected && _armies.Current.Faction.AlliedTo(target) && ActionLayers[SupportLayer].Contains(cell))
                sources = _selected.SupportableCells(cell).Where(ActionLayers[MoveLayer].Contains);
            else if (!_armies.Current.Faction.AlliedTo(target) && ActionLayers[AttackLayer].Contains(cell))
                sources = _selected.AttackableCells(cell).Where(ActionLayers[MoveLayer].Contains);
            sources = sources.Where((c) => !Grid.Occupants.ContainsKey(c) || Grid.Occupants[c] == _selected);
            if (sources.Any())
            {
                _target = target;
                if (!sources.Contains(_path[^1]))
                    PathLayer.Path = _path = sources.Select((c) => _path.Add(c).Clamp(_selected.Stats.Move)).OrderBy(
                        (p) => new Vector2I(-(int)p[^1].DistanceTo(cell), (int)p[^1].DistanceTo(_path[^1])),
                        static (a, b) => a < b ? -1 : a > b ? 1 : 0
                    ).First();
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
        if (ActionLayers[MoveLayer].Contains(cell))
        {
            Unit highlighted = Grid.Occupants.GetValueOrDefault(cell) as Unit;
            if (highlighted != _selected && _armies.Current.Faction.AlliedTo(highlighted))
                ErrorSound.Play();
        }
    }

    /// <summary>Deselect the selected <see cref="Unit"/> and clean up after canceling actions.</summary>
    public void OnSelectedToIdleTaken()
    {
        _initialCell = null;
        DeselectUnit();
        ActionLayers.Clear();
        PathLayer.Clear();
    }

    /// <summary>Move the <see cref="Object.Cursor"/> back to the selected <see cref="Unit"/> and then deselect it.</summary>
    public void OnSelectedCanceled()
    {
        _initialCell = null;
        PathLayer.Clear();
        Callable.From<Vector2I>((c) => Cursor.Cell = c).CallDeferred(_selected.Cell);
        DeselectUnit();
    }

    /// <summary>Clean up overlays when movement destination is chosen.</summary>
    public void OnDestinationChosen()
    {
        // Clear out movement/action ranges
        PathLayer.Clear();
        ActionLayers.Clear();
    }

    public void OnSelectedExited()
    {
        Cursor.SoftRestriction.Clear();

        // Restore the camera zoom back to what it was before a unit was selected
        if (Camera.HasZoomMemory())
            Camera.PopZoom();
    }
#endregion
#region Moving State
    private Vector4 _prevDeadzone = Vector4.Zero;

    /// <summary>When the <see cref="Unit"/> finishes moving, move to the next <see cref="Nodes.StateChart.States.State"/>.</summary>
    public void OnUnitDoneMoving()
    {
        _selected.DoneMoving -= OnUnitDoneMoving;
        Callable.From<StringName>(State.SendEvent).CallDeferred(_target is null ? DoneEvent : SkipEvent);
    }

    /// <summary>Begin moving the selected <see cref="Unit"/> and then wait for it to finish moving.</summary>
    public void OnMovingEntered()
    {
        Cursor.Halt();

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
        if (@event.IsActionPressed(InputActions.Cancel))
            _selected.SkipMoving();
    }

    /// <summary>When done moving, restore the <see cref="Camera2DBrain">camera</see> target (most likely to the cursor) and update danger zones.</summary>
    public void OnMovingExited()
    {
        (Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight) = _prevDeadzone;
        Camera.Target = _prevCameraTarget;
        _path = null;
        UpdateDangerZones();
        Cursor.Resume();
    }
#endregion
#region Unit Commanding State
    private ContextMenu _commandMenu = null;

    public void OnCommandingEntered()
    {
        // Show the unit's attack/support ranges
        IEnumerable<Vector2I> attackable = _selected.AttackableCells().Where((c) => !(Grid.Occupants.GetValueOrDefault(c) as Unit)?.Faction.AlliedTo(_selected) ?? false);
        IEnumerable<Vector2I> supportable = _selected.SupportableCells().Where((c) => (Grid.Occupants.GetValueOrDefault(c) as Unit)?.Faction.AlliedTo(_selected) ?? false);
        ActionLayers.Clear(MoveLayer);
        ActionLayers[AttackLayer] = attackable;
        ActionLayers[SupportLayer] = supportable;

        List<(StringName, Action)> options = [];
        if (attackable.Any() || supportable.Any())
            options.Add(("Attack", () => State.SendEvent(SelectEvent)));
        options.Add(("End", () => State.SendEvent(SkipEvent)));
        options.Add(("Cancel", () => State.SendEvent(CancelEvent)));
        _commandMenu = ShowMenu(Grid.CellRect(_selected.Cell), options);
        _commandMenu.MenuCanceled += () => State.SendEvent(CancelEvent);
        _commandMenu.MenuClosed += () => _commandMenu = null;
    }

    public void OnCommandingProcess(float delta) => _commandMenu.Position = MenuPosition(Grid.CellRect(_selected.Cell), _commandMenu.Size);

    /// <summary>Move the selected <see cref="Unit"/> and <see cref="Object.Cursor"/> back to the cell the unit was at before it moved.</summary>
    public void OnCommandingCanceled()
    {
        ActionLayers.Clear();

        // Move the selected unit back to its original cell
        Grid.Occupants.Remove(_selected.Cell);
        _selected.Cell = _initialCell.Value;
        Grid.Occupants[_selected.Cell] = _selected;
        _initialCell = null;
        Callable.From<Vector2I>((c) => Cursor.Cell = c).CallDeferred(_selected.Cell);

        _target = null;
        UpdateDangerZones();
    }

    public void OnTurnEndCommand()
    {
        _target = null;
        ActionLayers.Clear();
    }
#endregion
#region Targeting State
    private ImmutableList<CombatAction> _combatResults = null;

    public void OnTargetingEntered()
    {
        // Show the unit's attack/support ranges
        ImmutableHashSet<Vector2I> attackable = _selected.AttackableCells().Where((c) => !(Grid.Occupants.GetValueOrDefault(c) as Unit)?.Faction.AlliedTo(_selected) ?? false).ToImmutableHashSet();
        ImmutableHashSet<Vector2I> supportable = _selected.SupportableCells().Where((c) => (Grid.Occupants.GetValueOrDefault(c) as Unit)?.Faction.AlliedTo(_selected) ?? false).ToImmutableHashSet();

        // Restrict cursor movement to actionable cells
        if (_target is null)
        {
            Pointer.AnalogTracking = false;
            Cursor.HardRestriction = attackable.Union(supportable);
            Cursor.Wrap = true;
        }
    }

    /// <summary>
    /// Cycle the <see cref="Object.Cursor"/> between targets of the same action (attack, support, etc.) using <see cref="InputActions.Previous"/>
    /// and <see cref="InputActions.Next"/> while choosing targets.
    /// </summary>
    public void OnTargetingInput(InputEvent @event)
    {
        int next = 0;
        if (@event.IsActionPressed(InputActions.Previous))
            next = -1;
        else if (@event.IsActionPressed(InputActions.Next))
            next = 1;

        if (next != 0)
        {
            Vector2I[] cells = [];
            if (ActionLayers[AttackLayer].Contains(Cursor.Cell))
                cells = [.. ActionLayers[AttackLayer]];
            else if (ActionLayers[SupportLayer].Contains(Cursor.Cell))
                cells = [.. ActionLayers[SupportLayer]];
            else
                GD.PushError("Cursor is not on an actionable cell during targeting");
            
            if (cells.Length > 1)
                Cursor.Cell = cells[(Array.IndexOf(cells, Cursor.Cell) + next + cells.Length) % cells.Length];
        }
    }

    /// <summary>If a target is selected, begin combat fighting that target.  Otherwise, just end the selected <see cref="Unit"/>'s turn.</summary>
    /// <param name="cell">Cell being selected.</param>
    public void OnTargetingCellSelected(Vector2I cell)
    {
        if (!Cursor.HardRestriction.IsEmpty)
        {
            if (Grid.CellOf(Pointer.Position) == cell && Grid.Occupants[cell] != _selected)
            {
                if (Grid.Occupants[cell] is Unit target)
                {
                    _target = target;
                    State.SendEvent(DoneEvent);
                }
                else
                    State.SendEvent(SkipEvent);
            }
        }
        else
        {
            SelectSound.Play();
            State.SendEvent(SkipEvent);
        }
    }

    /// <summary>Clean up displayed ranges and restore <see cref="Object.Cursor"/> freedom when exiting targeting <see cref="Nodes.StateChart.States.State"/>.</summary>
    public void OnTargetingExited()
    {
        Pointer.AnalogTracking = true;
        Cursor.HardRestriction = Cursor.HardRestriction.Clear();
        Cursor.Wrap = false;
    }
#endregion
#region In Combat
    public void OnCombatEntered()
    {
        ActionLayers.Clear();
        Cursor.Halt();
        Pointer.StartWaiting(hide:true);
        SceneManager.BeginCombat(_selected, _target, _combatResults = CombatCalculations.CombatResults(_selected, _target));
    }

    /// <summary>Update the map to reflect combat results when it's added back to the tree.</summary>
    public void OnCombatEnteredTree()
    {
        _selected.Health.Value -= CombatCalculations.TotalDamage(_selected, _combatResults);
        _target.Health.Value -= CombatCalculations.TotalDamage(_target, _combatResults);
        _target = null;
        _combatResults = null;
        ActionLayers.Clear();
        SceneManager.Singleton.TransitionCompleted += OnTransitionedFromCombat;
    }

    /// <summary>Finish waiting once the transition back has completed.</summary>
    public void OnTransitionedFromCombat()
    {
        State.SendEvent(DoneEvent);
        SceneManager.Singleton.TransitionCompleted -= OnTransitionedFromCombat;
    }

    public void OnCombatExited()
    {
        Pointer.StopWaiting();
        Cursor.Resume();
    }
#endregion
#region Turn Advancing
    /// <summary>Update the UI turn counter for the current turn and change its color to match the army.</summary>
    private void UpdateTurnCounter()
    {
        TurnLabel.AddThemeColorOverride("font_color", _armies.Current.Faction.Color);
        TurnLabel.Text = $"Turn {Turn}: {_armies.Current.Faction.Name}";
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

        bool advance = !((IEnumerable<Unit>)_armies.Current).Any(static (u) => u.Active);
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
            State.SendEvent(DoneEvent);
            if (advance)
                Callable.From<Vector2I>((c) => Cursor.Cell = c).CallDeferred(((IEnumerable<Unit>)_armies.Current).First().Cell);
        }).CallDeferred();
    }
#endregion
#region State Independent
    /// <summary>When the pointer starts flying, we need to wait for it to finish. Also focus the camera on its target if there's something there.</summary>
    /// <param name="target">Position the pointer is going to fly to.</param>
    public void OnPointerFlightStarted(Vector2 target)
    {
        State.SendEvent(WaitEvent);
        _prevCameraTarget = Camera.Target;
        Camera.Target = Grid.Occupants.ContainsKey(Grid.CellOf(target)) ? Grid.Occupants[Grid.CellOf(target)] : Camera.Target;
    }

    /// <summary>When the pointer finished flying, return to the previous state.</summary>
    public void OnPointerFlightCompleted()
    {
        Camera.Target = _prevCameraTarget;
        State.SendEvent(DoneEvent);
    }

    /// <summary>When a cell is selected, act based on what is or isn't in the cell.</summary>
    /// <param name="cell">Coordinates of the cell selection.</param>
    public void OnCellSelected(Vector2I cell) => Callable.From<Vector2I>((pos) => {
        if (Grid.CellOf(pos) == cell)
            State.SendEvent(SelectEvent);
        else
            State.SendEvent(SkipEvent);
    }).CallDeferred(Pointer.Position);

    /// <summary>Automatically connect to a child <see cref="Army"/>'s <see cref="Node.SignalName.ChildEnteredTree"/> signal so new units in it can be automatically added to the grid.</summary>
    /// <param name="child">Child being added to the tree.</param>
    public void OnChildEnteredTree(Node child)
    {
        if (Engine.IsEditorHint() && child is Army army && !army.IsConnected(Node.SignalName.ChildEnteredTree, new Callable(this, MethodName.OnChildEnteredGroup)))
            army.Connect(Node.SignalName.ChildEnteredTree, new Callable(this, MethodName.OnChildEnteredGroup), (uint)ConnectFlags.Persist);
    }

    /// <summary>When a <see cref="GridNode"/> is added to a group, update its <see cref="GridNode.Grid"/>.</summary>
    /// <param name="child"></param>
    public void OnChildEnteredGroup(Node child)
    {
        if (child is GridNode gd)
            gd.Grid = Engine.IsEditorHint() ? GetNode<Grid>("Grid") : Grid;
    }

    public void OnUnitDefeated(Unit defeated)
    {
        if (_selected == defeated)
            _selected = null;
        defeated.Die();
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        // Make sure there's a map
        int maps = GetChildren().Count(static (c) => c is Grid);
        if (maps < 1)
            warnings.Add("Level does not contain a map.");
        else if (maps > 1)
            warnings.Add($"Level contains too many maps ({maps}).");

        // Make sure there are units to control and to fight.
        if (!GetChildren().Any(static (c) => c is Army))
            warnings.Add("There are not any armies to assign units to.");

        if (GetChildren().Count(static (c) => c is Army army && (army.Faction?.IsPlayer ?? false)) > 1)
            warnings.Add("Multiple armies are player-controlled. Only the first one will be used for zone display.");

        // Make sure there's background music
        if (BackgroundMusic is null)
            warnings.Add("Background music hasn't been added. Whatever's playing will stop.");

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            Grid = GetNode<Grid>("Grid");

            Camera.Limits = new(Vector2I.Zero, (Vector2I)(Grid.Size*Grid.CellSize));
            Pointer.World = Cursor.Grid = Grid;
            Pointer.Bounds = Camera.Limits;
            Pointer.DefaultFlightTime = Camera.DeadZoneSmoothTime;

            foreach (Unit unit in GetChildren().OfType<IEnumerable<Unit>>().Flatten())
            {
                unit.Grid = Grid;
                unit.Cell = Grid.CellOf(unit.GlobalPosition - Grid.GlobalPosition);
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

        if (@event.IsActionPressed(InputActions.Cancel))
            State.SendEvent(CancelEvent);

        if (@event.IsActionPressed(InputActions.ToggleDangerZone))
        {
            if (Grid.Occupants.GetValueOrDefault(Cursor.Cell) is Unit unit)
            {
#pragma warning disable CA1868 // Contains is necessary since the unit has to be added if it isn't in the set and removed if it is
                ZoneUnits = ZoneUnits.Contains(unit) ? ZoneUnits.Remove(unit) : ZoneUnits.Add(unit);
#pragma warning restore CA1868 // Unnecessary call to 'Contains(item)'
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
            State.ExpressionProperties = State.ExpressionProperties
                .SetItem(OccupiedProperty, Grid.Occupants.GetValueOrDefault(Cursor.Cell) switch
                {
                    Unit unit when unit == _selected => SelectedOccuiped,
                    Unit unit when _armies.Current.Faction == unit.Faction => unit.Active ? ActiveAllyOccupied : InActiveAllyOccupied,
                    Unit unit when _armies.Current.Faction.AlliedTo(unit.Faction) => FriendlyOccuipied,
                    Unit => EnemyOccupied,
                    null => NotOccupied,
                    _ => OtherOccupied
                })
                .SetItem(TargetProperty, Grid.Occupants.GetValueOrDefault(Cursor.Cell) is Unit target &&
                                          ((target != _selected && _armies.Current.Faction.AlliedTo(target) && ActionLayers[SupportLayer].Contains(Cursor.Cell)) ||
                                           (!_armies.Current.Faction.AlliedTo(target) && ActionLayers[AttackLayer].Contains(Cursor.Cell))))
                .SetItem(TraversableProperty, ActionLayers[MoveLayer].Contains(Cursor.Cell));
        }
    }
#endregion
}