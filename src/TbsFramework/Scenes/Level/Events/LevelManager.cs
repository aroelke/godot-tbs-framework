using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Level.Map;
using TbsFramework.Scenes.Level.Object;
using TbsFramework.Scenes.Level.Object.Group;
using TbsFramework.Nodes;
using TbsFramework.Scenes.Combat.Data;
using TbsFramework.UI;
using TbsFramework.Scenes.Level.Layers;
using TbsFramework.Scenes.Combat;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes.Level.Control;
using TbsFramework.Nodes.StateCharts;
using TbsFramework.Nodes.StateCharts.Reactions;
using TbsFramework.Scenes.Level.Events.Reactions;
using TbsFramework.Data;

namespace TbsFramework.Scenes.Level.Events;

/// <summary>
/// Manages the setup of and objects inside a level and provides them information about it.  Is "global" in a way in that
/// all objects in the level may be able to see it and request information from it, but each level has its own manager.
/// </summary>
[Tool]
public partial class LevelManager : Node
{
#region Constants
    // State chart events
    private static readonly StringName SelectEvent = "select";
    private static readonly StringName CancelEvent = "cancel";
    private static readonly StringName SkipEvent   = "skip";
    private static readonly StringName WaitEvent   = "wait";
    private static readonly StringName DoneEvent   = "done";
    // State chart conditions
    private readonly StringName TraversableProperty = "traversable"; // Current cell is traversable
    private readonly StringName ActiveProperty      = "active";      // Number of remaining active units
#endregion
#region Declarations
    private readonly NodeCache _cache = null;

    private Path _path = null;
    private UnitData _selected = null, _target = null;
    private IEnumerator<Army> _armies = null;
    private Vector2I? _initialCell = null;
    private readonly Stack<BoundedNode2D> _cameraHistory = [];
    private StringName _command = null;
    private bool _ff = false;

    private GridData _grid = null;
    private Camera2DController   Camera                            => _cache.GetNode<Camera2DController>("Camera");
    private CanvasLayer          UserInterface                     => _cache.GetNode<CanvasLayer>("UserInterface");
    private StateChart           State                             => _cache.GetNode<StateChart>("State");
    private ActionReaction       OnSkipTurnReaction                => _cache.GetNode<ActionReaction>("State/Root/Running/Skippable/OnFastForward");
    private Vector2IReaction     OnUnitSelectedReaction            => _cache.GetNode<Vector2IReaction>("State/Root/Running/Skippable/Idle/OnUnitSelected");
    private PathReaction         OnPathConfirmedReaction           => _cache.GetNode<PathReaction>("State/Root/Running/Skippable/UnitSelected/OnPathConfirmed");
    private CommandReaction      OnSelectedUnitCommandedReaction   => _cache.GetNode<CommandReaction>("State/Root/Running/Skippable/UnitSelected/OnUnitCommanded");
    private TargetReaction       OnSelectedTargetChosenReaction    => _cache.GetNode<TargetReaction>("State/Root/Running/Skippable/UnitSelected/OnTargetChosen");
    private CommandReaction      OnCommandingUnitCommandedReaction => _cache.GetNode<CommandReaction>("State/Root/Running/Skippable/UnitCommanding/OnUnitCommanded");
    private TargetReaction       OnTargetingTargetChosenReaction   => _cache.GetNode<TargetReaction>("State/Root/Running/Skippable/UnitTargeting/OnTargetChosen");
    private Timer                TurnAdvance                       => _cache.GetNode<Timer>("TurnAdvance");

    public LevelManager() : base() { _cache = new(this); }
#endregion
#region Exports
    /// <summary>Path to the scene used to play combat animations.</summary>
    [Export(PropertyHint.File, "*.tscn")] public string CombatScenePath = null;

    /// <summary>
    /// <see cref="Army"/> that gets the first turn and is controlled by the player. If null, use the first <see cref="Army"/>
    /// in the child list. After that, go down the child list in order, wrapping when at the end.
    /// </summary>
    [ExportGroup("Turn Control")]
    [Export] public Army StartingArmy = null;

    /// <summary>Turn count (including current turn, so it starts at 1).</summary>
    [ExportGroup("Turn Control")]
    [Export] public int Turn = 1;

    /// <summary>Modulate color for the action range overlay to use during idle state to differentiate from the one displayed while selecting a move path.</summary>
    [ExportGroup("Range Overlay")]
    [Export] public Color ActionRangeIdleModulate = new(1, 1, 1, 0.66f);
#endregion
#region Begin Turn State
    /// <summary>Signal that a turn is about to begin.</summary>
    public void OnBeginTurnEntered()
    {
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.SelectionCanceled, Callable.From(OnSelectionCanceled));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.TurnFastForward, Callable.From(OnSkipTurnReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.UnitSelected, Callable.From<Vector2I>(OnUnitSelectedReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.PathConfirmed, Callable.From<Vector2I, Godot.Collections.Array<Vector2I>>(OnPathConfirmedReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.UnitCommanded, Callable.From<Vector2I, StringName>(OnSelectedUnitCommandedReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.TargetChosen, Callable.From<Vector2I, Vector2I>(OnSelectedTargetChosenReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.TargetCanceled, Callable.From<Vector2I>(OnTargetingCanceled));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.UnitCommanded, Callable.From<Vector2I, StringName>(OnCommandingUnitCommandedReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.TargetChosen, Callable.From<Vector2I, Vector2I>(OnTargetingTargetChosenReaction.React));

        _armies.Current.Controller.InitializeTurn();
        Callable.From<int, Faction>(LevelEvents.BeginTurn).CallDeferred(Turn, _armies.Current.Faction);
    }
#endregion
#region Idle State
    /// <summary>Update the UI when re-entering idle.</summary>
    public void OnIdleEntered() => Callable.From(_armies.Current.Controller.SelectUnit).CallDeferred();

    public void OnIdleUnitSelected(Vector2I cell)
    {
        _selected = _grid.Occupants[cell];

        if (_selected.Faction != _armies.Current.Faction)
            throw new InvalidOperationException($"Cannot select unit not from army {_selected.Faction.Name}");
        if (!_selected.Active)
            throw new InvalidOperationException($"Cannot select inactive unit at {cell}");

        State.SendEvent(SelectEvent);
    }
#endregion
#region Unit Selected State
    /// <summary>Set the selected <see cref="Unit"/> to its idle state and the deselect it.</summary>
    private void DeselectUnit()
    {
        _initialCell = null;
        _selected.Renderer.Deselect();
        _selected = null;
    }

    /// <summary>Display the total movement, attack, and support ranges of the selected <see cref="Unit"/> and begin drawing the path arrow for it to move on.</summary>
    public void OnSelectedEntered()
    {
        _selected.Renderer.Select();
        _initialCell = _selected.Cell;
        _command = null;
        _target = null;
        _armies.Current.Controller.MoveUnit(_selected);
    }

    public void OnSelectedUnitCommanded(Vector2I cell, StringName command)
    {
        if (_grid.Occupants[cell] != _selected)
            throw new InvalidOperationException($"Cannot command unselected unit at {cell} ({_selected.Faction.Name} unit at {_selected.Cell} is selected)");
        _command = command;
    }

    public void OnSelectedTargetChosen(Vector2I source, Vector2I target)
    {
        if (_grid.Occupants[source] != _selected)
            throw new InvalidOperationException($"Cannot choose action target for unselected unit at {source} ({_selected.Faction.Name} unit at {_selected.Cell} is selected)");
        _target = _grid.Occupants[target];
    }

    public void OnSelectedPathConfirmed(Vector2I cell, Godot.Collections.Array<Vector2I> path)
    {
        UnitData unit = _grid.Occupants[cell];
        if (unit != _selected)
            throw new InvalidOperationException($"Cannot confirm path for unselected unit at {cell} ({_selected.Faction.Name} unit at {_selected.Cell} is selected)");
        if (path.Any((c) => _grid.Occupants.ContainsKey(c) && !_grid.Occupants[c].Faction.AlliedTo(_selected)))
            throw new InvalidOperationException("The chosen path must only contain traversable cells.");
        if (_grid.Occupants.ContainsKey(path[^1]) && _grid.Occupants[path[^1]] != unit)
            throw new InvalidOperationException("The chosen path must not end on an occupied cell.");

        State.SetVariable(TraversableProperty, true);
        if (_ff)
        {
            _selected.Cell = path[^1];
            State.SendEvent(SkipEvent);
        }
        else
        {
            _path = Path.Empty(_grid, _selected.GetTraversableCells()).AddRange(path);
            State.SendEvent(SelectEvent);
        }
    }

    public void OnSelectedCanceled()
    {
        _initialCell = null;
        _selected.Renderer.Deselect();
        _selected = null;
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

    /// <summary>Begin moving the selected <see cref="Unit"/> and then wait for it to finish moving.</summary>
    public void OnMovingEntered()
    {
        // Track the unit as it's moving
        _prevDeadzone = new(Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight);
        (Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight) = Vector4.Zero;
        PushCameraFocus(_selected.Renderer.MotionBox);

        // Move the unit
        _selected.WhenDoneMoving(_path[^1], _target is null ? () => State.SendEvent(DoneEvent) : () => State.SendEvent(SkipEvent));
        _selected.Renderer.MoveAlong(_path); // must be last in case it fires right away
    }

    /// <summary>When done moving, restore the <see cref="Camera2DController">camera</see> target (most likely to the cursor) and update danger zones.</summary>
    public void OnMovingExited()
    {
        (Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight) = _prevDeadzone;
        PopCameraFocus();
        _path = null;
    }
#endregion
#region Unit Commanding State
    private List<ContextMenuOption> _options = [];
    private IEnumerable<Vector2I> _targets = [];

    public void OnCommandingEntered()
    {
        _targets = [];
        _options = [];
        void AddActionOption(StringName name, IEnumerable<Vector2I> range)
        {
            if (range.Any())
            {
                _options.Add(new(name, () => {
                    _targets = range;
                    _command = name;
                    State.SendEvent(SelectEvent);
                }));
            }
        }
        AddActionOption(UnitAction.AttackAction, _selected.GetAttackableCells().Where((c) => !_grid.Occupants.GetValueOrDefault(c)?.Faction.AlliedTo(_selected) ?? false));
        AddActionOption(UnitAction.SupportAction, _selected.GetSupportableCells().Where((c) => _grid.Occupants.GetValueOrDefault(c)?.Faction.AlliedTo(_selected) ?? false));
        foreach (SpecialActionRegionData region in _grid.SpecialActionRegions)
        {
            if (region.CanPerform(_selected) && region.Cells.Contains(_selected.Cell))
            {
                _options.Add(new(region.Action, () => {
                    region.Perform(_selected, _selected.Cell);
                    State.SendEvent(SkipEvent);
                }));
            }
        }
        _options.Add(new(UnitAction.EndAction, () => State.SendEvent(SkipEvent)));
        _options.Add(new("Cancel", () => State.SendEvent(CancelEvent)));

        _armies.Current.Controller.CommandUnit(_selected, [.. _options.Select(static (o) => o.Name)], "Cancel");
    }

    public void OnCommandingUnitCommanded(Vector2I cell, StringName command)
    {
        if (_grid.Occupants[cell] != _selected)
            throw new InvalidOperationException($"Cannon command unselected unit at {cell} ({_selected.Faction.Name} unit at {_selected.Cell} is selected)");
        foreach (ContextMenuOption option in _options)
            if (option.Name == command)
                option.Action();
    }

    /// <summary>Move the selected <see cref="Unit"/> and <see cref="Cursor"/> back to the cell the unit was at before it moved.</summary>
    public void OnCommandingCanceled()
    {
        _command = null;

        _selected.Cell = _initialCell.Value;
        _initialCell = null;

        _target = null;
    }

    public void OnTurnEndCommand() => _target = null;
#endregion
#region Targeting State
    private List<CombatAction> _combatResults = null;

    public void OnTargetingEntered()
    {
        _armies.Current.Controller.SelectTarget(_selected, _targets);
    }

    public void OnTargetChosen(Vector2I source, Vector2I target)
    {
        _target = _grid.Occupants[target];
        if (_grid.Occupants[source] != _selected)
            throw new InvalidOperationException($"Cannot choose target for unselected unit at {source} ({_selected.Faction.Name} unit at {_selected.Cell} is selected)");
        if ((_command == UnitAction.AttackAction && _target.Faction.AlliedTo(_selected)) || (_command == UnitAction.SupportAction && !_target.Faction.AlliedTo(_selected)))
            throw new ArgumentException($"{_selected.Faction.Name} unit at {_selected.Cell} cannot {_command} unit at {target}");
        State.SendEvent(DoneEvent);
    }

    public void OnTargetingCanceled(Vector2I source) => State.SendEvent(CancelEvent);
#endregion
#region In Combat
    private void ApplyCombatResults()
    {
        foreach (CombatAction action in _combatResults)
        {
            if (action.Hit)
            {
                action.Target.Health -= action.Damage;
                if (action.Target.Health <= 0)
                    action.Target.Renderer.Die();
            }
        }
        _target = null;
        _combatResults = null;
    }

    public void OnCombatEntered()
    {
        if (_command == UnitAction.AttackAction)
            _combatResults = CombatCalculations.AttackResults(_selected, _target, false);
        else if (_command == UnitAction.SupportAction)
            _combatResults = [CombatCalculations.CreateSupportAction(_selected, _target)];
        else
            throw new NotSupportedException($"Unknown action {_command}");

        if (_ff)
        {
            ApplyCombatResults();
            State.SendEvent(DoneEvent);
        }
        else
        {
            SceneManager.Singleton.Connect<CombatScene>(SceneManager.SignalName.SceneLoaded, (s) => s.Initialize(_selected, _target, [.. _combatResults]), (uint)ConnectFlags.OneShot);
            SceneManager.CallScene(CombatScenePath);
        }
    }

    /// <summary>Update the map to reflect combat results when it's added back to the tree.</summary>
    public void OnCombatEnteredTree()
    {
        ApplyCombatResults();
        SceneManager.Singleton.Connect(SceneManager.SignalName.TransitionCompleted, () => State.SendEvent(DoneEvent), (uint)ConnectFlags.OneShot);
    }
#endregion
#region End Action State
    /// <summary>Signal that a unit's action has ended.</summary>
    public void OnEndActionEntered()
    {
        _armies.Current.Controller.FinalizeAction();
        _selected.Active = false;
        State.SetVariable(ActiveProperty, ((IEnumerable<Unit>)_armies.Current).Count(static (u) => u.UnitData.Active));

        UnitData selected = _selected;
        Callable.From(() => LevelEvents.EndAction(selected)).CallDeferred();
    }

    /// <summary>Clean up at the end of the unit's turn.</summary>
    public void OnEndActionExited()
    {
        _selected = null;
    }
#endregion
#region End Turn State
    public void Turnover() => Callable.From<int, Faction>(LevelEvents.EndTurn).CallDeferred(Turn, _armies.Current.Faction);

    /// <summary>After a delay, signal that the turn is ending and wait for a response.</summary>
    public void OnEndTurnEntered() => TurnAdvance.Start();

    /// <summary>Refresh all the units in the army whose turn just ended so they aren't gray anymore and are animated.</summary>
    public void OnEndTurnExited()
    {
        _ff = false;
        _armies.Current.Controller.FinalizeTurn();

        foreach (Unit unit in (IEnumerable<Unit>)_armies.Current)
            unit.UnitData.Active = true;

        do
        {
            if (_armies.MoveNext() && _armies.Current == StartingArmy)
                Turn++;
        } while (!((IEnumerable<Unit>)_armies.Current).Any());
    }
#endregion
#region State Independent
    public void OnSelectionCanceled() => State.SendEvent(CancelEvent);

    public void OnTurnFastForward()
    {
        // Reuse this signal for skipping to the end of the current army's turn, which should only happen for player-controlled armies
        if (!((IEnumerable<Unit>)_armies.Current).Any(static (u) => u.UnitData.Active))
            State.SendEvent(SkipEvent);
        else if (!_ff)
        {
            // Reuse this signal for fast-forwarding through an army's turn, which should only happen for AI-controlled armies
            _armies.Current.Controller.FastForwardTurn();
            _ff = true;
        }
    }

    /// <summary>Change the camera focus to a new object and save the previous one for reverting focus.</summary>
    /// <param name="target">New camera focus target. Use <c>null</c> to not focus on anything.</param>
    public void PushCameraFocus(BoundedNode2D target)
    {
        _cameraHistory.Push(Camera.Target);
        Camera.Target = target;
    }

    /// <summary>Focus the camera back on the previous target.</summary>
    public void PopCameraFocus() => Camera.Target = _cameraHistory.Pop();

    /// <summary>When an event is completed, go to the next state.</summary>
    public void OnEventComplete()
    {
        // Events should only occur on the map, so if this node isn't in the scene tree and receives an event completion signal, then the level must have
        // completed and this LevelManager is no longer needed
        if (IsInsideTree())
            State.SendEvent(DoneEvent);
        else
            QueueFree();
    }

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
        if (Engine.IsEditorHint())
            if (child is GridNode gd)
                gd.Grid = GetNode<Grid>("Grid");
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        if (!Engine.IsEditorHint())
        {
            LevelEvents.EventCompleted += OnEventComplete;
            LevelEvents.CameraFocused += PushCameraFocus;
            LevelEvents.CameraFocusReverted += PopCameraFocus;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            Grid grid = GetNode<Grid>("Grid");
            _grid = grid.Data;

            Camera.Limits = new(Vector2I.Zero, (Vector2I)(_grid.Size*grid.CellSize));
            LevelEvents.UpdateCameraBounds(Camera.Limits);

            foreach (Army army in GetChildren().OfType<Army>())
            {
                army.Controller.Grid = grid;
                foreach (Unit unit in (IEnumerable<Unit>)army)
                    unit.Grid = grid;
            }

            _armies = GetChildren().OfType<Army>().GetCyclicalEnumerator();
            if (StartingArmy is null)
                StartingArmy = _armies.Current;
            else // Advance the army enumerator until it's pointing at StartingArmy
                while (_armies.Current != StartingArmy)
                    if (!_armies.MoveNext())
                        break;

            Callable.From(() => State.SendEvent(DoneEvent)).CallDeferred();
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (!Engine.IsEditorHint())
        {
            LevelEvents.EventCompleted -= OnEventComplete;
            LevelEvents.CameraFocused -= PushCameraFocus;
            LevelEvents.CameraFocusReverted -= PopCameraFocus;
        }
    }
#endregion
#region Editor
    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        // Make sure there's a map
        int maps = GetChildren().Count(static (c) => c is Grid);
        if (maps < 1)
            warnings.Add("Level does not contain a map.");
        else if (maps > 1)
            warnings.Add($"Level contains too many maps ({maps}).");

        // Make sure there are units to control and to fight.
        if (!GetChildren().Any(static (c) => c is Army))
            warnings.Add("There are not any armies to assign units to.");

        return [.. warnings];
    }
#endregion
}