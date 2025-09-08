using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes;
using TbsTemplate.Nodes.Components;
using TbsTemplate.Nodes.StateChart;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Layers;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;
using TbsTemplate.UI;
using TbsTemplate.UI.Controls.Action;
using TbsTemplate.UI.Controls.Device;
using TbsTemplate.UI.HUD;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Controls units based on player input.  Also includes UI elements to facilitate gameplay.</summary>
[Tool]
public partial class PlayerController : ArmyController
{
    private static readonly StringName SelectEvent  = "Select";
    private static readonly StringName PathEvent    = "Path";
    private static readonly StringName CommandEvent = "Command";
    private static readonly StringName TargetEvent  = "Target";
    private static readonly StringName FinishEvent  = "Finish";
    private static readonly StringName CancelEvent  = "Cancel";
    private static readonly StringName WaitEvent    = "Wait";

    private readonly NodeCache _cache = null;
    private readonly DynamicEnumProperties<StringName> _events = new([SelectEvent, PathEvent, CommandEvent, TargetEvent, FinishEvent, CancelEvent, WaitEvent]);
    private Grid _grid = null;
    private TileSet _tileset = null;
    private Color _move    = Colors.Blue  with { A = 100f/256f };
    private Color _attack  = Colors.Red   with { A = 100f/256f };
    private Color _support = Colors.Green with { A = 100f/256f };
    private Color _ally    = new(0, 0.25f, 0.5f, 100f/256f);
    private Color _local   = new(0.5f, 0, 0.25f, 100f/256f);
    private Color _global  = Colors.Black with { A = 100f/256f };

    private Unit _selected = null, _target = null;
    IEnumerable<Vector2I> _traversable = null, _attackable = null, _supportable = null;
    private Path _path;

    private ActionLayers      ActionLayers        => _cache.GetNode<ActionLayers>("ActionLayers");
    private TileMapLayer      MoveLayer           => _cache.GetNodeOrNull<TileMapLayer>("ActionLayers/Move");
    private TileMapLayer      AttackLayer         => _cache.GetNodeOrNull<TileMapLayer>("ActionLayers/Attack");
    private TileMapLayer      SupportLayer        => _cache.GetNodeOrNull<TileMapLayer>("ActionLayers/Support");
    private ActionLayers      ZoneLayers          => _cache.GetNode<ActionLayers>("ZoneLayers");
    private TileMapLayer      AllyTraversableZone => _cache.GetNodeOrNull<TileMapLayer>("ZoneLayers/TraversableZone");
    private TileMapLayer      LocalDangerZone     => _cache.GetNodeOrNull<TileMapLayer>("ZoneLayers/LocalDangerZone");
    private TileMapLayer      GlobalDangerZone    => _cache.GetNodeOrNull<TileMapLayer>("ZoneLayers/GlobalDangerZone");
    private PathLayer         PathLayer           => _cache.GetNode<PathLayer>("PathLayer");
    private Cursor            Cursor              => _cache.GetNode<Cursor>("Cursor");
    private Pointer           Pointer             => _cache.GetNode<Pointer>("Pointer");
    private CanvasLayer       UserInterface       => _cache.GetNode<CanvasLayer>("UserInterface");
    private Godot.Control     HUD                 => _cache.GetNode<Godot.Control>("UserInterface/HUD");
    private ControlHint       CancelHint          => _cache.GetNode<ControlHint>("%CancelHint");
    private AudioStreamPlayer SelectSound         => _cache.GetNode<AudioStreamPlayer>("SoundLibrary/SelectSound");
    private AudioStreamPlayer CancelSound         => _cache.GetNode<AudioStreamPlayer>("SoundLibrary/CancelSound");
    private AudioStreamPlayer ErrorSound          => _cache.GetNode<AudioStreamPlayer>("SoundLibrary/ErrorSound");
    private AudioStreamPlayer ZoneOnSound         => _cache.GetNode<AudioStreamPlayer>("SoundLibrary/ZoneOnSound");
    private AudioStreamPlayer ZoneOffSound        => _cache.GetNode<AudioStreamPlayer>("SoundLibrary/ZoneOffSound");
    private Chart             State               => _cache.GetNode<Chart>("State");

    public override Grid Grid
    {
        get => _grid;
        set
        {
            if (_grid != value)
            {
                _grid = value;
                if (Cursor is not null)
                    Cursor.Grid = _grid;
                if (Pointer is not null)
                    Pointer.World = _grid;
            }
        }
    }

    public PlayerController() : base() { _cache = new(this); }
#region Exports
    private void UpdateActionRangeTileSet(TileSet ts)
    {
        foreach (TileMapLayer layer in ActionLayers.GetChildren().OfType<TileMapLayer>())
            layer.TileSet = ts;
        foreach (TileMapLayer layer in ActionLayers.GetChildren().OfType<TileMapLayer>())
            layer.TileSet = ts;
    }

    /// <summary>Tile set to use for displaying action ranges and danger zones.</summary>
    [Export] public TileSet ActionRangeTileSet
    {
        get => _tileset;
        set => UpdateActionRangeTileSet(_tileset = value);
    }

    /// <summary>Color to use for highlighting which cells a unit can move to.</summary>
    [Export] public Color ActionRangeMoveColor
    {
        get => _move;
        set
        {
            _move = value;
            if (MoveLayer is not null)
                MoveLayer.Modulate = _move;
        }
    }

    /// <summary>Color to use for highlighting which cells a unit can attack, but not move to.</summary>
    [Export] public Color ActionRangeAttackColor
    {
        get => _attack;
        set
        {
            _attack = value;
            if (AttackLayer is not null)
                AttackLayer.Modulate = _attack;
        }
    }

    /// <summary>Color to use for highlighting which cells a unit can support, but not move to or attack.</summary>
    [Export] public Color ActionRangeSupportColor
    {
        get => _support;
        set
        {
            _support = value;
            if (SupportLayer is not null)
                SupportLayer.Modulate = _support;
        }
    }

    /// <summary>Color to modulate the action ranges with while hovering over a unit.</summary>
    [Export] public Color ActionRangeHoverModulate = Colors.White with { A = 0.66f };

    /// <summary>Color to modulate the action ranges with while a unit is selected.</summary>
    [Export] public Color ActionRangeSelectModulate = Colors.White;

    /// <summary>Color to use for highlighting which cells a tracked set of allied units can move to.</summary>
    [Export] public Color ZoneAllyTraversableColor
    {
        get => _ally;
        set
        {
            _ally = value;
            if (AllyTraversableZone is not null)
                AllyTraversableZone.Modulate = _ally;
        }
    }

    /// <summary>Color to use for highlighting which cells a tracked set of enemy units can attack.</summary>
    [Export] public Color ZoneLocalDangerColor
    {
        get => _local;
        set
        {
            _local = value;
            if (LocalDangerZone is not null)
                LocalDangerZone.Modulate = _local;
        }
    }

    /// <summary>Color to use for highlighting which cells any enemy unit can attack.</summary>
    [Export] public Color ZoneGlobalDangerColor
    {
        get => _global;
        set
        {
            _global = value;
            if (GlobalDangerZone is not null)
                GlobalDangerZone.Modulate = _global;
        }
    }
#endregion
#region Menus
    private ContextMenu _menu = null;

    private Vector2 MenuPosition(Rect2 rect, Vector2 size)
    {
        Rect2 viewportRect = Cursor.Grid.GetGlobalTransformWithCanvas()*rect;
        float viewportCenter = GetViewport().GetVisibleRect().Position.X + GetViewport().GetVisibleRect().Size.X/2;
        return new(
            viewportCenter - viewportRect.Position.X < viewportRect.Size.X/2 ? viewportRect.Position.X - size.X : viewportRect.End.X,
            Mathf.Clamp(viewportRect.Position.Y - (size.Y - viewportRect.Size.Y)/2, 0, GetViewport().GetVisibleRect().Size.Y - size.Y)
        );
    }

    private ContextMenu ShowMenu(Rect2 rect, IEnumerable<ContextMenuOption> options)
    {
        Cursor.Halt(hide:true);
        Pointer.StartWaiting(hide:false);
        CancelHint.Visible = true;

        ContextMenu menu = ContextMenu.Instantiate(options);
        menu.Wrap = true;
        UserInterface.AddChild(menu);
        menu.Visible = false;
        menu.MenuClosed += () => LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.RevertCameraFocus);

        LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.FocusCamera, (BoundedNode2D)null);

        Callable.From<ContextMenu, Rect2>((m, r) => {
            m.Visible = true;
            if (DeviceManager.Mode != InputMode.Mouse)
                m.GrabFocus();
            m.Position = MenuPosition(r, m.Size);
        }).CallDeferred(menu, rect);

        return menu;
    }
#endregion
#region Danger Zone
    private readonly HashSet<Unit> _tracked = [];
    private bool _showGlobalDangerZone = false;

    private void UpdateDangerZones()
    {
        IEnumerable<Unit> allies = _tracked.Where(Army.Faction.AlliedTo);
        IEnumerable<Unit> enemies = _tracked.Where((u) => !Army.Faction.AlliedTo(u));

        if (allies.Any())
            ZoneLayers[AllyTraversableZone.Name] = allies.SelectMany(static (u) => u.TraversableCells());
        else
            ZoneLayers.Clear(AllyTraversableZone.Name);
        if (enemies.Any())
            ZoneLayers[LocalDangerZone.Name] = enemies.SelectMany(static (u) => u.AttackableCells(u.TraversableCells()));
        else
            ZoneLayers.Clear(LocalDangerZone.Name);
        
        if (_showGlobalDangerZone)
            ZoneLayers[GlobalDangerZone.Name] = Grid.Occupants.Values.OfType<Unit>().Where((u) => !Army.Faction.AlliedTo(u)).SelectMany(static (u) => u.AttackableCells(u.TraversableCells()));
        else
            ZoneLayers.Clear(GlobalDangerZone.Name);
    }

    /// <returns>The audio player that plays the "zone on" or "zone off" sound depending on <paramref name="on"/>.</returns>
    private AudioStreamPlayer ZoneUpdateSound(bool on) => on ? ZoneOnSound : ZoneOffSound;
#endregion
#region Initialization and Finalization
    public override void InitializeTurn()
    {
        HUD.Visible = true;
        UpdateDangerZones();
        ZoneLayers.Visible = true;

        Cursor.Cell = ((IEnumerable<Unit>)Army).First().Cell;

        Cursor.Resume();
        Pointer.StopWaiting();
    }

    public override void FinalizeAction() => UpdateDangerZones();

    public override void FinalizeTurn()
    {
        base.FinalizeTurn();

        ZoneLayers.Visible = false;
        Cursor.Halt(hide:true);
        Pointer.StartWaiting(hide:true);
        HUD.Visible = false;
    }
#endregion
#region State Independent
    public void OnCancel() => CancelSound.Play();
    public void OnFinish() => SelectSound.Play();

    public void OnPointerFlightStarted(Vector2 target)
    {
        State.SendEvent(_events[WaitEvent]);
        LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.FocusCamera, target);
    }

    public void OnPointerFlightCompleted()
    {
        State.SendEvent(_events[FinishEvent]);
        LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.RevertCameraFocus);
    }

    public void OnUnitDefeated(Unit defeated)
    {
        if (_tracked.Remove(defeated) || _showGlobalDangerZone)
            UpdateDangerZones();
    }

    public override void FastForwardTurn() => throw new NotImplementedException("Fast forward doesn't make sense for the player controller yet");
#endregion
#region Active
    public void OnActiveInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputActions.Cancel))
        {
            State.SendEvent(_events[CancelEvent]);
            EmitSignal(SignalName.SelectionCanceled);
        }

        if (@event.IsActionPressed(InputActions.ToggleDangerZone))
        {
            if (Cursor.Grid.Occupants.TryGetValue(Cursor.Cell, out GridNode node) && node is Unit unit)
            {
                if (!_tracked.Remove(unit))
                    _tracked.Add(unit);
                ZoneUpdateSound(_tracked.Contains(unit)).Play();
            }
            else
                ZoneUpdateSound(_showGlobalDangerZone = !_showGlobalDangerZone).Play();
            UpdateDangerZones();
        }
    }
#endregion
#region Unit Selection
    public override void SelectUnit()
    {
        Cursor.Resume();
        Pointer.StopWaiting();
        CancelHint.Visible = false;

        ActionLayers.Clear();
        ActionLayers.Modulate = ActionRangeHoverModulate;

        OnSelectCursorCellEntered(Cursor.Cell = Grid.CellOf(Pointer.Position));
        Callable.From(() => State.SendEvent(_events[SelectEvent])).CallDeferred();
    }

    private void ConfirmCursorSelection(Vector2I cell)
    {
        if (Cursor.Grid.Occupants.TryGetValue(cell, out GridNode node) && node is Unit unit)
        {
            if (unit.Army.Faction == Army.Faction && unit.Active)
            {
                State.SendEvent(_events[FinishEvent]);
                EmitSignal(SignalName.UnitSelected, unit);
            }
            else if (unit.Army.Faction != Army.Faction)
            {
                if (!_tracked.Remove(unit))
                    _tracked.Add(unit);
                ZoneUpdateSound(_tracked.Contains(unit)).Play();
                UpdateDangerZones();
            }
        }
        else
        {
            void Cancel()
            {
                CancelSound.Play();
                Cursor.Resume();
                Pointer.StopWaiting();
            }

            SelectSound.Play();
            ContextMenu menu = ShowMenu(Cursor.Grid.CellRect(cell), [
                new("End Turn", () => {
                    // Cursor is already halted
                    Pointer.StartWaiting(hide:true);

                    foreach (Unit unit in (IEnumerable<Unit>)Army)
                        unit.Finish();
                    State.SendEvent(_events[FinishEvent]);
                    EmitSignal(SignalName.TurnFastForward);
                    SelectSound.Play();
                }),
                new("Quit Game", () => GetTree().Quit()),
                new("Cancel", Cancel)
            ]);
            menu.MenuCanceled += Cancel;
            menu.MenuClosed += () => CancelHint.Visible = false;
        }
    }

    public void OnSelectEntered() => Cursor.CellSelected += ConfirmCursorSelection;

    public void OnSelectInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputActions.Previous) || @event.IsActionPressed(InputActions.Next))
        {
            if (Cursor.Grid.Occupants.GetValueOrDefault(Cursor.Cell) is Unit hovered)
            {
                if (@event.IsActionPressed(InputActions.Previous) && hovered.Army.Previous(hovered) is Unit prev)
                    Cursor.Cell = prev.Cell;
                if (@event.IsActionPressed(InputActions.Next) && hovered.Army.Next(hovered) is Unit next)
                    Cursor.Cell = next.Cell;
            }
            else
            {
                IEnumerable<Unit> units = Army.GetChildren().OfType<Unit>().Where((u) => u.Active);
                if (!units.Any())
                    units = Army.GetChildren().OfType<Unit>();
                if (units.Any())
                    Cursor.Cell = units.OrderBy((u) => u.Cell.DistanceTo(Cursor.Cell)).First().Cell;
            }
        }

        if (@event.IsActionPressed(InputActions.Cancel) && Cursor.Grid.Occupants.TryGetValue(Cursor.Cell, out GridNode node) && node is Unit untrack)
        {
            if (_tracked.Remove(untrack))
            {
                ZoneUpdateSound(false).Play();
                UpdateDangerZones();
            }
        }
    }

    public void OnSelectCursorCellChanged(Vector2I cell) => ActionLayers.Clear();

    public void OnSelectCursorCellEntered(Vector2I cell)
    {
        if (Grid.Occupants.GetValueOrDefault(cell) is Unit unit)
            (ActionLayers[MoveLayer.Name], ActionLayers[AttackLayer.Name], ActionLayers[SupportLayer.Name]) = unit.ActionRanges();
    }

    public void OnSelectedPointerStopped(Vector2 position) => OnSelectCursorCellEntered(Grid.CellOf(position));

    public void OnSelectExited() => Cursor.CellSelected -= ConfirmCursorSelection;
#endregion
#region Path Selection
    private StringName _command = null;

    public override void MoveUnit(Unit unit)
    {
        Cursor.Resume();
        Pointer.StopWaiting();
        CancelHint.Visible = true;
        ActionLayers.Modulate = ActionRangeSelectModulate;

        _target = null;
        Callable.From(() => {
            State.SendEvent(_events[PathEvent]);

            _selected = unit;
            (ActionLayers[MoveLayer.Name], ActionLayers[AttackLayer.Name], ActionLayers[SupportLayer.Name]) = (_traversable, _attackable, _supportable) = _selected.ActionRanges();
            Cursor.SoftRestriction = [.. _traversable];
            Cursor.Cell = _selected.Cell;
            UpdatePath(Path.Empty(Cursor.Grid, _traversable).Add(_selected.Cell));
        }).CallDeferred();
    }

    private void UpdatePath(Path path) => PathLayer.Path = [.. _path = path];

    private void AddToPath(Vector2I cell)
    {
        // If the previous cell was an ally that could be supported and moved through, add it to the path as if it
        // had been added in the previous movement
        if (_target is not null && _supportable.Contains(_target.Cell) && _traversable.Contains(_target.Cell))
            UpdatePath(_path.Add(_target.Cell));

        _target = null;
        _command = null;

        IEnumerable<Vector2I> sources = [];
        if (Cursor.Grid.Occupants.GetValueOrDefault(cell) is Unit target)
        {
            // Compute cells the highlighted unit could be targeted from (friend or foe)
            if (target != _selected && Army.Faction.AlliedTo(target) && _supportable.Contains(cell))
                sources = _selected.SupportableCells(cell).Where(_traversable.Contains);
            else if (!Army.Faction.AlliedTo(target) && _attackable.Contains(cell))
                sources = _selected.AttackableCells(cell).Where(_traversable.Contains);
            sources = sources.Where((c) => !Cursor.Grid.Occupants.ContainsKey(c) || Cursor.Grid.Occupants[c] == _selected);

            if (sources.Any())
            {
                _target = target;

                // Store the action command related to selecting the target as if it were the command state
                if (_attackable.Contains(cell))
                    _command = UnitActions.AttackAction;
                else if (_supportable.Contains(cell))
                    _command = UnitActions.SupportAction;

                // If the end of the path isn't a cell that could act on the target, find the furthest one that can and add
                // it to the path
                if (!sources.Contains(_path[^1]))
                {
                    UpdatePath(sources.Select((c) => _path.Add(c).Clamp(_selected.Stats.Move)).OrderBy(
                        (p) => new Vector2I(-(int)p[^1].DistanceTo(cell), (int)p[^1].DistanceTo(_path[^1])),
                        static (a, b) => a < b ? -1 : a > b ? 1 : 0
                    ).First());
                }
            }
        }
        if (!sources.Any() && _traversable.Contains(cell))
            UpdatePath(_path.Add(cell).Clamp(_selected.Stats.Move));
    }

    private void ConfirmPathSelection(Vector2I cell)
    {
        bool occupied = Cursor.Grid.Occupants.TryGetValue(cell, out GridNode occupant);
        if (!_traversable.Contains(cell) && !occupied)
        {
            State.SendEvent(_events[CancelEvent]);
            EmitSignal(SignalName.SelectionCanceled);
        }
        else if (!occupied || occupant == _selected)
        {
            State.SendEvent(_events[FinishEvent]);
            EmitSignal(SignalName.PathConfirmed, _selected, new Godot.Collections.Array<Vector2I>(_path));
        }
        else if (occupied && occupant is Unit target && (_attackable.Contains(target.Cell) || _supportable.Contains(target.Cell)))
        {
            State.SendEvent(_events[FinishEvent]);
            EmitSignal(SignalName.UnitCommanded, _selected, _command);
            EmitSignal(SignalName.TargetChosen, _selected, target);
            EmitSignal(SignalName.PathConfirmed, _selected, new Godot.Collections.Array<Vector2I>(_path));
        }
        else
            ErrorSound.Play();
    }

    private void CleanUpPath()
    {
        Cursor.SoftRestriction.Clear();
        PathLayer.Clear();
        ActionLayers.Clear();
        ActionLayers.Modulate = ActionRangeHoverModulate;
    }

    public void OnPathEntered()
    {
        Cursor.CellChanged += AddToPath;
        Cursor.CellSelected += ConfirmPathSelection;
    }

    public void OnPathCanceled() => CleanUpPath();

    public void OnPathFinished()
    {
        Cursor.Halt(hide:false);
        Pointer.StartWaiting(hide:false);
        CancelHint.Visible = false;
        _selected.Connect(Unit.SignalName.DoneMoving, UpdateDangerZones, (uint)ConnectFlags.OneShot);
        CleanUpPath();
    }

    public void OnPathExited()
    {
        Cursor.CellChanged -= AddToPath;
        Cursor.CellSelected -= ConfirmPathSelection;
    }
#endregion
#region Command Selection
    public override void CommandUnit(Unit source, Godot.Collections.Array<StringName> commands, StringName cancel)
    {
        ActionLayers.Clear(MoveLayer.Name);
        ActionLayers[AttackLayer.Name] = source.AttackableCells().Where((c) => !(Grid.Occupants.GetValueOrDefault(c) as Unit)?.Army.Faction.AlliedTo(source) ?? false);
        ActionLayers[SupportLayer.Name] = source.SupportableCells().Where((c) => (Grid.Occupants.GetValueOrDefault(c) as Unit)?.Army.Faction.AlliedTo(source) ?? false);

        Callable.From(() => {
            State.SendEvent(_events[CommandEvent]);

            _selected = source;
            _menu = ShowMenu(Cursor.Grid.CellRect(source.Cell), commands.Select((c) => new ContextMenuOption() { Name = c, Action = () => {
                ActionLayers.Keep(c);
                State.SendEvent(_events[FinishEvent]);
                EmitSignal(SignalName.UnitCommanded, source, c);
            }}));
            _menu.MenuCanceled += () => EmitSignal(SignalName.UnitCommanded, source, cancel);
            _menu.MenuClosed += () => _menu = null;
        }).CallDeferred();
    }

    public void OnCommandEntered() => CancelHint.Visible = true;

    public void OnCommandProcess(double delta) => _menu.Position = MenuPosition(Cursor.Grid.CellRect(_selected.Cell), _menu.Size);
#endregion
#region Target Selection
    private IEnumerable<Vector2I> _targets = null;

    public override void SelectTarget(Unit source, IEnumerable<Vector2I> targets)
    {
        Cursor.Resume();
        Pointer.StopWaiting();
        CancelHint.Visible = true;

        Pointer.AnalogTracking = false;
        Cursor.Wrap = true;

        Callable.From(() => {
            State.SendEvent(_events[TargetEvent]);

            _selected = source;
            Cursor.HardRestriction = [.. _targets=targets];
        }).CallDeferred();
    }

    private void ConfirmTargetSelection(Vector2I cell)
    {
        if (Cursor.Cell != Grid.CellOf(Pointer.Position))
        {
            State.SendEvent(_events[CancelEvent]);
            EmitSignal(SignalName.TargetCanceled, _selected);
        }
        else if (Cursor.Grid.Occupants.TryGetValue(cell, out GridNode node) && node is Unit target)
        {
            Cursor.Halt(hide:false);
            Pointer.StartWaiting(hide:false);
            CancelHint.Visible = false;

            State.SendEvent(_events[FinishEvent]);
            EmitSignal(SignalName.TargetChosen, _selected, target);
        }
    }

    public void OnTargetEntered() => Cursor.CellSelected += ConfirmTargetSelection;

    public void OnTargetInput(InputEvent @event)
    {
        int next = 0;
        if (@event.IsActionPressed(InputActions.Previous))
            next = -1;
        else if (@event.IsActionPressed(InputActions.Next))
            next = 1;

        if (next != 0)
        {
            if (_targets.Contains(Cursor.Cell))
            {
                if (_targets.Count() > 1)
                {
                    Vector2I[] cells = [.. _targets];
                    Cursor.Cell = cells[(Array.IndexOf(cells, Cursor.Cell) + next + cells.Length) % cells.Length];
                }
            }
            else
                GD.PushError("Cursor is not on an actionable cell during targeting");
        }
    }

    public void OnTargetCompleted()
    {
        ActionLayers.Clear();

        Pointer.AnalogTracking = true;
        Cursor.HardRestriction = Cursor.HardRestriction.Clear();
        Cursor.Wrap = false;
    }

    public void OnTargetExited() => Cursor.CellSelected -= ConfirmTargetSelection;
#endregion
#region Engine Events
    public override void _Ready()
    {
        base._Ready();

        UpdateActionRangeTileSet(_tileset);
        MoveLayer.Modulate           = _move;
        AttackLayer.Modulate         = _attack;
        SupportLayer.Modulate        = _support;
        AllyTraversableZone.Modulate  = _ally;
        LocalDangerZone.Modulate  = _local;
        GlobalDangerZone.Modulate = _global;

        if (!Engine.IsEditorHint())
        {
            LevelEvents.Singleton.Connect<Rect2I>(LevelEvents.SignalName.CameraBoundsUpdated, (b) => Pointer.Bounds = b);
            LevelEvents.Singleton.Connect<Unit>(LevelEvents.SignalName.UnitDefeated, OnUnitDefeated);
            Callable.From(() => LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.FocusCamera, Pointer)).CallDeferred();
            Cursor.Halt();
        }
    }
#endregion
#region Properties
    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = base._GetPropertyList() ?? [];
        properties.AddRange(_events.GetPropertyList(State.Events));
        return properties;
    }

    public override Variant _Get(StringName property)
    {
        if (_events.TryGetPropertyValue(property, out StringName value))
            return value;
        else
            return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (value.VariantType == Variant.Type.StringName && _events.SetPropertyValue(property, value.AsStringName()))
            return true;
        else
            return base._Set(property, value);
    }

    public override bool _PropertyCanRevert(StringName property)
    {
        if (_events.PropertyCanRevert(property, out bool revert))
            return revert;
        else
            return base._PropertyCanRevert(property);
    }

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (_events.TryPropertyGetRevert(property, out StringName revert))
            return revert;
        else
            return base._PropertyGetRevert(property);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_menu is not null)
            _menu.Position = MenuPosition(Cursor.Grid.CellRect(_selected.Cell), _menu.Size);
    }
}
#endregion