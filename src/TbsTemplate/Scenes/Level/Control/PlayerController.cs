using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes;
using TbsTemplate.Nodes.Components;
using TbsTemplate.Nodes.StateCharts;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Layers;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;
using TbsTemplate.UI;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Controls units based on player input.  Also includes UI elements to facilitate gameplay.</summary>
[Icon("res://icons/PlayerController.svg"), Tool]
public partial class PlayerController : ArmyController
{
    private static readonly StringName SelectEvent  = "select";
    private static readonly StringName PathEvent    = "path";
    private static readonly StringName CommandEvent = "command";
    private static readonly StringName TargetEvent  = "target";
    private static readonly StringName FinishEvent  = "finish";
    private static readonly StringName CancelEvent  = "cancel";
    private static readonly StringName WaitEvent    = "wait";

    private readonly NodeCache _cache = null;
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

    private StateChart             State               => _cache.GetNode<StateChart>("State");
    private ActionLayers      ActionLayers        => _cache.GetNode<ActionLayers>("ActionLayers");
    private TileMapLayer      MoveLayer           => _cache.GetNodeOrNull<TileMapLayer>("ActionLayers/Move");
    private TileMapLayer      AttackLayer         => _cache.GetNodeOrNull<TileMapLayer>("ActionLayers/Attack");
    private TileMapLayer      SupportLayer        => _cache.GetNodeOrNull<TileMapLayer>("ActionLayers/Support");
    private ActionLayers      ZoneLayers          => _cache.GetNode<ActionLayers>("ZoneLayers");
    private TileMapLayer      AllyTraversableZone => _cache.GetNodeOrNull<TileMapLayer>("ZoneLayers/TraversableZone");
    private TileMapLayer      LocalDangerZone     => _cache.GetNodeOrNull<TileMapLayer>("ZoneLayers/LocalDangerZone");
    private TileMapLayer      GlobalDangerZone    => _cache.GetNodeOrNull<TileMapLayer>("ZoneLayers/GlobalDangerZone");
    private PathLayer         PathLayer           => _cache.GetNode<PathLayer>("PathLayer");
    private Cursor            Cursor              => _cache.GetNodeOrNull<Cursor>("Cursor");
    private Sprite2D          CursorSprite        => _cache.GetNodeOrNull<Sprite2D>("Cursor/Sprite");
    private Pointer           Pointer             => _cache.GetNode<Pointer>("Pointer");
    private TextureRect       PointerSprite       => _cache.GetNodeOrNull<TextureRect>("Pointer/Canvas/Mouse");
    private CanvasLayer       UserInterface       => _cache.GetNode<CanvasLayer>("UserInterface");
    private AudioStreamPlayer SelectSoundPlayer   => _cache.GetNodeOrNull<AudioStreamPlayer>("SoundLibrary/SelectSound");
    private AudioStreamPlayer CancelSoundPlayer   => _cache.GetNodeOrNull<AudioStreamPlayer>("SoundLibrary/CancelSound");
    private AudioStreamPlayer ErrorSoundPlayer    => _cache.GetNodeOrNull<AudioStreamPlayer>("SoundLibrary/ErrorSound");
    private AudioStreamPlayer ZoneOnSoundPlayer   => _cache.GetNodeOrNull<AudioStreamPlayer>("SoundLibrary/ZoneOnSound");
    private AudioStreamPlayer ZoneOffSoundPlayer  => _cache.GetNodeOrNull<AudioStreamPlayer>("SoundLibrary/ZoneOffSound");

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
        foreach (TileMapLayer layer in ZoneLayers.GetChildren().OfType<TileMapLayer>())
            layer.TileSet = ts;
    }

    /// <summary>Sprite to use for the grid cursor.</summary>
    [Export, ExportGroup("Control UI")] public Texture2D CursorSpriteTexture
    {
        get => CursorSprite?.Texture;
        set
        {
            if (CursorSprite is not null)
                CursorSprite.Texture = value;
        }
    }

    /// <summary>Offset of the sprite from the origin of the texture to use for positioning it within a cell.</summary>
    [Export(PropertyHint.None, "suffix:px"), ExportGroup("Control UI")] public Vector2 CursorSpriteOffset
    {
        get => CursorSprite?.Offset ?? Vector2.Zero;
        set
        {
            if (CursorSprite is not null)
                CursorSprite.Offset = value;
        }
    }

    /// <summary>Sprite to use for the analog (not mouse) pointer.</summary>
    [Export, ExportGroup("Control UI")] public Texture2D PointerSpriteTexture
    {
        get => PointerSprite?.Texture;
        set
        {
            if (PointerSprite is not null)
                PointerSprite.Texture = value;
        }
    }

    /// <summary>Offset of the analog pointer sprite from the origin of the texture.</summary>
    [Export(PropertyHint.None, "suffix:px"), ExportGroup("Control UI")] public Vector2 PointerSpriteOffset
    {
        get => PointerSprite?.Position ?? Vector2.Zero;
        set
        {
            if (PointerSprite is not null)
                PointerSprite.Position = value;
        }
    }

    /// <summary>Tile set to use for displaying action ranges and danger zones.</summary>
    [Export, ExportGroup("Action Ranges", "ActionRange")] public TileSet ActionRangeTileSet
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
    [Export, ExportGroup("Action Ranges")] public Color ZoneAllyTraversableColor
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
    [Export, ExportGroup("Action Ranges")] public Color ZoneLocalDangerColor
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
    [Export, ExportGroup("Action Ranges")] public Color ZoneGlobalDangerColor
    {
        get => _global;
        set
        {
            _global = value;
            if (GlobalDangerZone is not null)
                GlobalDangerZone.Modulate = _global;
        }
    }

    /// <summary>Sound to play when the cursor moves to a new cell.</summary>
    [Export, ExportGroup("Sounds")] public AudioStream CursorMoveSound
    {
        get => Cursor?.MoveSound;
        set
        {
            if (Cursor is not null)
                Cursor.MoveSound = value;
        }
    }

    /// <summary>Sound to play when making a selection.</summary>
    [Export, ExportGroup("Sounds")] public AudioStream SelectSound
    {
        get => SelectSoundPlayer?.Stream;
        set
        {
            if (SelectSoundPlayer is not null)
                SelectSoundPlayer.Stream = value;
        }
    }

    /// <summary>Sound to play when cancelling a selection.</summary>
    [Export, ExportGroup("Sounds")] public AudioStream CancelSound
    {
        get => CancelSoundPlayer?.Stream;
        set
        {
            if (CancelSoundPlayer is not null)
                CancelSoundPlayer.Stream = value;
        }
    }

    /// <summary>Sound to play when an invalid selection is made.</summary>
    [Export, ExportGroup("Sounds")] public AudioStream ErrorSound
    {
        get => ErrorSoundPlayer?.Stream;
        set
        {
            if (ErrorSoundPlayer is not null)
                ErrorSoundPlayer.Stream = value;
        }
    }

    /// <summary>Sound to play when turning on a danger or movement zone.</summary>
    [Export, ExportGroup("Sounds")] public AudioStream ZoneOnSound
    {
        get => ZoneOnSoundPlayer?.Stream;
        set
        {
            if (ZoneOnSoundPlayer is not null)
                ZoneOnSoundPlayer.Stream = value;
        }
    }

    /// <summary>Sound to play when turning off a danger or movement zone.</summary>
    [Export, ExportGroup("Sounds")] public AudioStream ZoneOffSound
    {
        get => ZoneOffSoundPlayer?.Stream;
        set
        {
            if (ZoneOffSoundPlayer is not null)
                ZoneOffSoundPlayer.Stream = value;
        }
    }

    [Export, ExportGroup("Sounds")] public AudioStream MenuHighlightSound = null;
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

        ContextMenu menu = ContextMenu.Instantiate(options, MenuHighlightSound);
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
    private AudioStreamPlayer ZoneUpdateSound(bool on) => on ? ZoneOnSoundPlayer : ZoneOffSoundPlayer;
#endregion
#region Initialization and Finalization
    public override void InitializeTurn()
    {
        UpdateDangerZones();
        ZoneLayers.Visible = true;

        Cursor.Cell = ((IEnumerable<Unit>)Army).Any() ? ((IEnumerable<Unit>)Army).First().Cell : Vector2I.Zero;

        Cursor.Resume();
        Pointer.StopWaiting();
    }

    public override void FinalizeAction()
    {
        UpdateDangerZones();
        EmitSignal(SignalName.ProgressUpdated, ((IEnumerable<Unit>)Army).Count((u) => !u.Active) + 1, ((IEnumerable<Unit>)Army).Count((u) => u.Active) - 1); // Add one to account for the unit that just finished
        EmitSignal(SignalName.EnabledInputActionsUpdated, Array.Empty<StringName>());
    }

    public override void FinalizeTurn()
    {
        base.FinalizeTurn();

        ZoneLayers.Visible = false;
        Cursor.Halt(hide:true);
        Pointer.StartWaiting(hide:true);
    }
#endregion
#region State Independent
    public void OnCancel() => CancelSoundPlayer.Play();
    public void OnFinish() => SelectSoundPlayer.Play();

    public void OnPointerFlightStarted(Vector2 target)
    {
        State.SendEvent(WaitEvent);
        LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.FocusCamera, target);
    }

    public void OnPointerFlightCompleted()
    {
        State.SendEvent(FinishEvent);
        LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.RevertCameraFocus);
    }

    public void OnUnitDefeated(Unit defeated)
    {
        if (_tracked.Remove(defeated) || _showGlobalDangerZone)
            UpdateDangerZones();
    }

    public override void FastForwardTurn() => throw new NotImplementedException("Fast forward doesn't make sense for the player controller yet");
#endregion
#region Ready
    public void OnReadyInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputManager.Cancel) && (_selected?.IsMoving ?? false))
            _selected.SkipMoving();
    }
#endregion
#region Active
    public void OnActiveInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputManager.Cancel))
        {
            State.SendEvent(CancelEvent);
            EmitSignal(SignalName.SelectionCanceled);
        }

        if (@event.IsActionPressed(InputManager.ToggleDangerZone))
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
        ActionLayers.Clear();
        ActionLayers.Modulate = ActionRangeHoverModulate;

        OnSelectCursorCellEntered(Cursor.Cell = Grid.CellOf(Pointer.Position));
        Callable.From(() => State.SendEvent(SelectEvent)).CallDeferred();
    }

    private void ConfirmCursorSelection(Vector2I cell)
    {
        if (Cursor.Grid.Occupants.TryGetValue(cell, out GridNode node) && node is Unit unit)
        {
            if (unit.Army.Faction == Army.Faction && unit.Active)
            {
                State.SendEvent(FinishEvent);
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
                CancelSoundPlayer.Play();
                Cursor.Resume();
                Pointer.StopWaiting();
            }

            SelectSoundPlayer.Play();
            EmitSignal(SignalName.EnabledInputActionsUpdated, new StringName[] {
                InputManager.DigitalMoveUp, InputManager.DigitalMoveDown,
                InputManager.AnalogMoveUp, InputManager.AnalogMoveDown,
                InputManager.UiHome, InputManager.UiHome,
                InputManager.Select, InputManager.UiAccept, InputManager.Cancel
            });
            ContextMenu menu = ShowMenu(Cursor.Grid.CellRect(cell), [
                new("End Turn", () => {
                    // Cursor is already halted
                    Pointer.StartWaiting(hide:true);

                    foreach (Unit unit in (IEnumerable<Unit>)Army)
                        unit.Finish();
                    State.SendEvent(FinishEvent);
                    EmitSignal(SignalName.TurnFastForward);
                    SelectSoundPlayer.Play();
                }),
                new("Quit Game", () => GetTree().Quit()),
                new("Cancel", Cancel)
            ]);
            menu.MenuCanceled += Cancel;
            menu.MenuClosed += () => EmitSignal(SignalName.EnabledInputActionsUpdated, new StringName[] {
                InputManager.DigitalMoveUp, InputManager.DigitalMoveLeft, InputManager.DigitalMoveRight, InputManager.DigitalMoveDown,
                InputManager.AnalogMoveUp, InputManager.AnalogMoveLeft, InputManager.AnalogMoveRight, InputManager.AnalogMoveDown,
                InputManager.Previous, InputManager.Next,
                InputManager.Select,
                InputManager.ToggleDangerZone
            });
        }
    }

    public void OnSelectEntered()
    {
        Cursor.CellSelected += ConfirmCursorSelection;

        EmitSignal(SignalName.EnabledInputActionsUpdated, new StringName[] {
            InputManager.DigitalMoveUp, InputManager.DigitalMoveLeft, InputManager.DigitalMoveRight, InputManager.DigitalMoveDown,
            InputManager.AnalogMoveUp, InputManager.AnalogMoveLeft, InputManager.AnalogMoveRight, InputManager.AnalogMoveDown,
            InputManager.Previous, InputManager.Next,
            InputManager.Select,
            InputManager.ToggleDangerZone
        });
    }

    public void OnSelectInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputManager.Previous) || @event.IsActionPressed(InputManager.Next))
        {
            if (Cursor.Grid.Occupants.GetValueOrDefault(Cursor.Cell) is Unit hovered)
            {
                if (@event.IsActionPressed(InputManager.Previous) && hovered.Army.Previous(hovered) is Unit prev)
                    Cursor.Cell = prev.Cell;
                if (@event.IsActionPressed(InputManager.Next) && hovered.Army.Next(hovered) is Unit next)
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

        if (@event.IsActionPressed(InputManager.Cancel) && Cursor.Grid.Occupants.TryGetValue(Cursor.Cell, out GridNode node) && node is Unit untrack)
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
        ActionLayers.Modulate = ActionRangeSelectModulate;

        _target = null;
        Callable.From(() => {
            State.SendEvent(PathEvent);

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
                    _command = UnitAction.AttackAction;
                else if (_supportable.Contains(cell))
                    _command = UnitAction.SupportAction;

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
            State.SendEvent(CancelEvent);
            EmitSignal(SignalName.SelectionCanceled);
        }
        else if (!occupied || occupant == _selected)
        {
            State.SendEvent(FinishEvent);
            EmitSignal(SignalName.EnabledInputActionsUpdated, new StringName[] {InputManager.Skip, InputManager.Accelerate});
            EmitSignal(SignalName.PathConfirmed, _selected, new Godot.Collections.Array<Vector2I>(_path));
        }
        else if (occupied && occupant is Unit target && (_attackable.Contains(target.Cell) || _supportable.Contains(target.Cell)))
        {
            State.SendEvent(FinishEvent);
            EmitSignal(SignalName.EnabledInputActionsUpdated, new StringName[] {InputManager.Skip, InputManager.Accelerate});
            EmitSignal(SignalName.UnitCommanded, _selected, _command);
            EmitSignal(SignalName.TargetChosen, _selected, target);
            EmitSignal(SignalName.PathConfirmed, _selected, new Godot.Collections.Array<Vector2I>(_path));
        }
        else
            ErrorSoundPlayer.Play();
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

        EmitSignal(SignalName.EnabledInputActionsUpdated, new StringName[] {
            InputManager.DigitalMoveUp, InputManager.DigitalMoveLeft, InputManager.DigitalMoveRight, InputManager.DigitalMoveDown,
            InputManager.AnalogMoveUp, InputManager.AnalogMoveLeft, InputManager.AnalogMoveRight, InputManager.AnalogMoveDown,
            InputManager.Select, InputManager.Cancel,
            InputManager.ToggleDangerZone
        });
    }

    public void OnPathCanceled() => CleanUpPath();

    public void OnPathFinished()
    {
        Cursor.Halt(hide:false);
        Pointer.StartWaiting(hide:false);
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
            State.SendEvent(CommandEvent);

            _selected = source;
            _menu = ShowMenu(Cursor.Grid.CellRect(source.Cell), commands.Select((c) => new ContextMenuOption() { Name = c, Action = () => {
                ActionLayers.Keep(c);
                State.SendEvent(FinishEvent);
                EmitSignal(SignalName.UnitCommanded, source, c);
            }}));
            _menu.MenuCanceled += () => EmitSignal(SignalName.UnitCommanded, source, cancel);
            _menu.MenuClosed += () => _menu = null;
        }).CallDeferred();
    }

    public void OnCommandEntered()
    {
        EmitSignal(SignalName.EnabledInputActionsUpdated, new StringName[] {
            InputManager.DigitalMoveUp, InputManager.DigitalMoveDown,
            InputManager.AnalogMoveUp, InputManager.AnalogMoveDown,
            InputManager.UiHome, InputManager.UiHome,
            InputManager.Select, InputManager.UiAccept, InputManager.Cancel
        });
    }

    public void OnCommandProcess(double delta) => _menu.Position = MenuPosition(Cursor.Grid.CellRect(_selected.Cell), _menu.Size);
#endregion
#region Target Selection
    private IEnumerable<Vector2I> _targets = null;

    public override void SelectTarget(Unit source, IEnumerable<Vector2I> targets)
    {
        Cursor.Resume();
        Pointer.StopWaiting();

        Pointer.AnalogTracking = false;
        Cursor.Wrap = true;

        Callable.From(() => {
            State.SendEvent(TargetEvent);

            _selected = source;
            Cursor.HardRestriction = [.. _targets=targets];
        }).CallDeferred();
    }

    private void ConfirmTargetSelection(Vector2I cell)
    {
        if (Cursor.Cell != Grid.CellOf(Pointer.Position))
        {
            State.SendEvent(CancelEvent);
            EmitSignal(SignalName.TargetCanceled, _selected);
        }
        else if (Cursor.Grid.Occupants.TryGetValue(cell, out GridNode node) && node is Unit target)
        {
            Cursor.Halt(hide:false);
            Pointer.StartWaiting(hide:false);

            State.SendEvent(FinishEvent);
            EmitSignal(SignalName.TargetChosen, _selected, target);
        }
    }

    public void OnTargetEntered()
    {
        Cursor.CellSelected += ConfirmTargetSelection;

        EmitSignal(SignalName.EnabledInputActionsUpdated, new StringName[] {
            InputManager.DigitalMoveUp, InputManager.DigitalMoveLeft, InputManager.DigitalMoveRight, InputManager.DigitalMoveDown,
            InputManager.AnalogMoveUp, InputManager.AnalogMoveLeft, InputManager.AnalogMoveRight, InputManager.AnalogMoveDown,
            InputManager.Previous, InputManager.Next,
            InputManager.Select, InputManager.Cancel,
            InputManager.ToggleDangerZone
        });
    }

    public void OnTargetInput(InputEvent @event)
    {
        int next = 0;
        if (@event.IsActionPressed(InputManager.Previous))
            next = -1;
        else if (@event.IsActionPressed(InputManager.Next))
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

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_menu is not null)
            _menu.Position = MenuPosition(Cursor.Grid.CellRect(_selected.Cell), _menu.Size);
    }
}
#endregion