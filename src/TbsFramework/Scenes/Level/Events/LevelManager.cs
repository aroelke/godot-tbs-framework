using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Nodes;
using TbsFramework.UI;
using TbsFramework.Scenes.Combat;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes.Level.Control;
using TbsFramework.Nodes.StateCharts;
using TbsFramework.Nodes.StateCharts.Reactions;
using TbsFramework.Scenes.Level.Events.Reactions;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Rendering;
using TbsFramework.Scenes.Level.Actions;

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
    private readonly StringName TurnoverProperty    = "over";        // Advancing armies caused a round turnover
#endregion
#region Declarations
    private readonly NodeCache _cache = null;
    private CombatController _combat = null;

    private Path _path = null;
    private UnitData _selected = null, _target = null;
    private IEnumerator<Army> _armies = null;
    private Vector2I? _initialCell = null;
    private readonly Stack<BoundedNode2D> _cameraHistory = [];
    private UnitAction _command = null;
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
    /// <summary>List of all possible actions that can be performed by units in this level.</summary>
    [Export] public UnitAction[] AvailableActions = [];

    /// <summary>
    /// <see cref="Army"/> that gets the first turn and is controlled by the player. If null, use the first <see cref="Army"/>
    /// in the child list. After that, go down the child list in order, wrapping when at the end.
    /// </summary>
    [ExportGroup("Turn Control")]
    [Export] public Army StartingArmy = null;

    /// <summary>Turn count (including current turn, so it starts at 1).</summary>
    [ExportGroup("Turn Control")]
    [Export] public int Turn = 1;

    /// <summary>Path to the scene used to play combat animations. If <c>null</c>, animations will play on map.</summary>
    [ExportGroup("Combat Control")]
    [Export(PropertyHint.File, "*.tscn")] public string CombatScenePath = null;

    /// <summary>Force playing combat animations using map contents rather than in a separate scene.</summary>
    [ExportGroup("Combat Control")]
    [Export] public bool PlayCombatOnMap = false;

    /// <summary>Skip playing combat animations entirely.</summary>
    [ExportGroup("Combat Control")]
    [Export] public bool SkipCombat = false;
#endregion
#region Begin Turn State
    /// <summary>Signal that a turn is about to begin.</summary>
    public void OnBeginTurnEntered()
    {
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.SelectionCanceled, Callable.From(OnSelectionCanceled));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.TurnFastForward, Callable.From(OnSkipTurnReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.UnitSelected, Callable.From<Vector2I>(OnUnitSelectedReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.PathConfirmed, Callable.From<Vector2I, Godot.Collections.Array<Vector2I>>(OnPathConfirmedReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.UnitCommanded, Callable.From<Vector2I, UnitAction>(OnSelectedUnitCommandedReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.TargetChosen, Callable.From<Vector2I, Vector2I>(OnSelectedTargetChosenReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.TargetCanceled, Callable.From<Vector2I>(OnTargetingCanceled));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.UnitCommanded, Callable.From<Vector2I, UnitAction>(OnCommandingUnitCommandedReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.TargetChosen, Callable.From<Vector2I, Vector2I>(OnTargetingTargetChosenReaction.React));

        _armies.Current.Controller.InitializeTurn();
        Callable.From<int, Faction>(LevelEvents.BeginTurn).CallDeferred(Turn, _armies.Current.Faction);
    }
#endregion
#region Idle State
    /// <summary>Update the UI when re-entering idle.</summary>
    public void OnIdleEntered() => _armies.Current.Controller.SelectUnit(AvailableActions);

    /// <summary>When a unit is selected, move to the next state (choosing its destination).</summary>
    /// <param name="cell">Cell containing hte selected unit.</param>
    /// <exception cref="InvalidOperationException">If the cell doesn't contain an active unit of the current turn's army's faction.</exception>
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
        _target = null;
        _armies.Current.Controller.MoveUnit(_selected, AvailableActions);
    }

    /// <summary>
    /// Store the path chosen for the selected unit to move along, then go to the next state (path movement). If fast-forwarding, just send the unit to
    /// its destination.
    /// </summary>
    /// <param name="cell">Cell containing the unit to move.</param>
    /// <param name="path">Path along which the unit should move</param>
    /// <exception cref="InvalidOperationException">
    /// If <paramref name="cell"/> doesn't contain the selected unit, any element of <paramref name="path"/> is not traversable by the selected unit, or
    /// the last element of <paramref name="path"/> is occupied (units can pass through other allied units).
    /// </exception>
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

    /// <summary>When selection is canceled, revert stored data.</summary>
    public void OnSelectedCanceled()
    {
        _initialCell = null;
        _selected.Renderer.Deselect();
        _selected = null;
    }

    /// <summary>Clean up after finishing destination decision.</summary>
    public void OnSelectedExited()
    {
        // Restore the camera zoom back to what it was before a unit was selected
        if (Camera.HasZoomMemory())
            Camera.PopZoom();
    }

    /*
     * It is possible to directly command a unit while choosing its destination by selecting a valid target at that time. When this happens, the selected
     * will move to the cell at the end of the arrow and perform the action corresponding to the target, bypassing command and target selection states.
     */

    /// <summary>Store the command corresponding to the target chosen while selecting movement destination.</summary>
    /// <param name="cell">Cell containing the unit to perform the command.</param>
    /// <param name="command">Command to perform.</param>
    /// <exception cref="InvalidOperationException">If <paramref name="cell"/> doesn't contain the selected unit.</exception>
    public void OnSelectedUnitCommanded(Vector2I cell, UnitAction command)
    {
        if (_grid.Occupants[cell] != _selected)
            throw new InvalidOperationException($"Cannot command unselected unit at {cell} ({_selected.Faction.Name} unit at {_selected.Cell} is selected)");
        _command = command;
    }

    /// <summary>Store the unit at the cell containing the target for the action chosen while selecting movement destination.</summary>
    /// <param name="source">Cell containing the unit to perform the action.</param>
    /// <param name="target">Cell containing the target of the action.</param>
    /// <exception cref="InvalidOperationException">If <paramref name="source"/> doesn't contain the selected unit.</exception>
    public void OnSelectedTargetChosen(Vector2I source, Vector2I target)
    {
        if (_grid.Occupants[source] != _selected)
            throw new InvalidOperationException($"Cannot choose action target for unselected unit at {source} ({_selected.Faction.Name} unit at {_selected.Cell} is selected)");
        _target = _grid.Occupants[target];
    }
#endregion
#region Moving State
    private Vector4 _prevDeadzone = Vector4.Zero;

    /// <summary>Begin moving the selected unit and wait for it to finish moving.</summary>
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
    // These represent components for an "action" wrapping a QoS control like cancel or deselect (or "End," which could be considered a real action)
    private partial class InternalActionDomain(IEnumerable<Vector2I> allowed) : ActionDomain
    {
        public override bool Contains(Vector2I cell) => allowed.Contains(cell);
    }

    private partial class InternalActionExecute(StateChart state, StringName @event) : ActionExecute
    {
        public override object Perform(UnitData unit, Vector2I target) => throw new InvalidOperationException("Internal actions don't have results");
        public override void UpdateGrid(GridData grid, UnitData actor, Vector2I target, object result) => state.SendEvent(@event);
        public override GridData Simulate(UnitData unit, Vector2I source, Vector2I target) => throw new InvalidOperationException("Internal actions can't be simulated");

    }

    private IEnumerable<Vector2I> _targets = [];

    /// <summary>
    /// Tell the current army controller what valid commands are available and the actions that correspond to them. Then wait for it to make
    /// a selection.
    /// </summary>
    public void OnCommandingEntered()
    {
        _targets = [];
        UnitAction deselect = new() { Name = ActionInfo.Deselect, DomainComponents = [new InternalActionDomain([_initialCell.Value])], ExecuteComponent = new InternalActionExecute(State, SkipEvent) };
        UnitAction end = new() { Name = ActionInfo.EndAction, AlwaysShow = true, ExecuteComponent = new InternalActionExecute(State, DoneEvent) };
        UnitAction cancel = new() { Name = ActionInfo.Cancel, AlwaysShow = true, ExecuteComponent = new InternalActionExecute(State, CancelEvent) };
        _armies.Current.Controller.CommandUnit(_selected, [..AvailableActions, deselect, end], cancel);
    }

    /// <summary>Initiate the command chosen by the selected unit.  See <see cref="OnCommandingEntered"/> for effects of commands.</summary>
    /// <param name="cell">Cell containing the unit being commanded.</param>
    /// <param name="command">Command to perform.</param>
    /// <exception cref="InvalidOperationException">If <paramref name="cell"/> does not contain the selected unit.</exception>
    /// <exception cref="ArgumentException">If <paramref name="command"/> is not a recognized command or <see cref="CancelCommand"/></exception>
    public void OnCommandingUnitCommanded(Vector2I cell, UnitAction command)
    {
        if (_grid.Occupants[cell] != _selected)
            throw new InvalidOperationException($"Cannot command unselected unit at {cell} ({_selected.Faction.Name} unit at {_selected.Cell} is selected)");
        if (command.ExecuteComponent is InternalActionExecute)
            command.ExecuteComponent.UpdateGrid(_grid, _selected, -Vector2I.One, null);
        else
        {
            _targets = command.GetTargetCells(_selected, _selected.Cell);
            _command = command;
            State.SendEvent(SelectEvent);
        }
    }

    /// <summary>Go back to selecting a destination, moving the selected unit and cursor back the unit's original cell.</summary>
    public void OnCommandingCanceled()
    {
        _command = null;

        _selected.Cell = _initialCell.Value;
        _initialCell = null;

        _target = null;
    }
#endregion
#region Targeting State
    private UnitActionResult _result = default;

    /// <summary>Instruct the current army's controller to choose a target for its action or skip to combat if there is none.</summary>
    public void OnTargetingEntered()
    {
        if (_command.RequiresTarget)
            _armies.Current.Controller.SelectTarget(_selected, _targets);
        else
        {
            _target = _selected;
            State.SendEvent(DoneEvent);
        }
    }

    /// <summary>Save the chosen target and then begin combat.</summary>
    /// <param name="source">Cell containing the unit that is performing the action.</param>
    /// <param name="target">Cell containing the unit that is the target of the action.</param>
    /// <exception cref="InvalidOperationException">If <paramref name="source"/> doesn't contain the selected unit.</exception>
    /// <exception cref="ArgumentException">If the action is being performed on an illegal target.</exception>
    public void OnTargetChosen(Vector2I source, Vector2I target)
    {
        _target = _grid.Occupants[target];
        if (_grid.Occupants[source] != _selected)
            throw new InvalidOperationException($"Cannot choose target for unselected unit at {source} ({_selected.Faction.Name} unit at {_selected.Cell} is selected)");
        if (!_command.CanPerform(_selected, source, target))
            throw new ArgumentException($"{_selected.Faction.Name} unit at {source} cannot {_command.Name} unit at {target}");
        State.SendEvent(DoneEvent);
    }

    /// <summary>Go back to choosing a command.</summary>
    public void OnTargetingCanceled(Vector2I source) => State.SendEvent(CancelEvent);
#endregion
#region In Combat
    /// <summary>
    /// Compute the results of combat and then begin the combat choreography unless <see cref="SkipCombat"/> is <c>true</c> or the turn
    /// is being fast-forwarded (see <see cref="OnTurnFastForward"/>).
    /// </summary>
    /// <exception cref="NotSupportedException">If the selected unit is not performing an action with an animation.</exception>
    /// <remarks>
    /// Note that combat results are not actually applied until returning from the combat scene if <see cref="PlayCombatOnMap"/>
    /// is <c>false.</c>
    /// </remarks>
    public void OnCombatEntered()
    {
        _result = _command.Perform(_selected, _target.Cell);

        void skip()
        {
            _result.UpdateGrid(_selected.Grid);
            State.SendEvent(DoneEvent);
        }

        if (_ff || SkipCombat)
            skip();
        else if (!_command.AnimateOnMap && !string.IsNullOrEmpty(CombatScenePath) && !PlayCombatOnMap)
        {
            SceneManager.Singleton.Connect<CombatController>(SceneManager.SignalName.SceneLoaded, (s) => s.Initialize(_selected, _target, _result), (uint)ConnectFlags.OneShot);
            SceneManager.CallScene(CombatScenePath);
        }
        else if (_combat is not null)
        {
            _combat.Initialize(_selected, _target, _result);
            _combat.Connect(CombatController.SignalName.CombatEnded, skip, (uint)ConnectFlags.OneShot);
            _combat.Start();
        }
        else
            skip();
    }

    /// <summary>Update the map to reflect combat results when it's added back to the tree.</summary>
    public void OnCombatEnteredTree()
    {
        _result.UpdateGrid(_selected.Grid);
        SceneManager.Singleton.Connect(SceneManager.SignalName.TransitionCompleted, () => State.SendEvent(DoneEvent), (uint)ConnectFlags.OneShot);
    }

    /// <summary>Clear out the command and result when combat is over.</summary>
    public void OnCombatExited()
    {
        _result = default;
        _command = null;
    }
#endregion
#region End Action State
    /// <summary>Signal that a unit's action has ended.</summary>
    public void OnEndActionEntered()
    {
        _armies.Current.Controller.FinalizeAction();
        _selected.Active = false;
        State.SetVariable(ActiveProperty, _armies.Current.Count(static (u) => u.UnitData.Active));

        UnitData selected = _selected;
        LevelEvents.EndAction(selected);
    }

    /// <summary>Clean up at the end of the unit's turn.</summary>
    public void OnEndActionExited()
    {
        _selected = null;
    }
#endregion
#region End Turn State
    private Army ended = null;

    /// <summary>Signal that the turn is ending.</summary>
    public void Turnover()
    {
        ended = _armies.Current;

        // Compute the next army here so we know if the round needs to end
        do
        {
            if (_armies.MoveNext() && _armies.Current == StartingArmy)
                State.SetVariable(TurnoverProperty, true);
        } while (!_armies.Current.Any());

        LevelEvents.EndTurn(Turn, ended.Faction);
    }

    /// <summary>After a delay, signal that the turn is ending and wait for a response.</summary>
    public void OnEndTurnEntered() => TurnAdvance.Start();

    /// <summary>Perform end-of-turn cleanup.</summary>
    public void OnEndTurnExited()
    {
        foreach (Unit unit in (IEnumerable<Unit>)ended)
            unit.UnitData.Active = true;

        _ff = false;
        ended.Controller.FinalizeTurn();
        ended = null;
    }
#endregion
#region End Round State
    /// <summary>Signal that the round is ending.</summary>
    public void OnRoundEndEntered() => LevelEvents.EndRound(Turn);

    /// <summary>Finish ending the round by incrementing the turn counter.</summary>
    public void OnRoundEndExited()
    {
        State.SetVariable(TurnoverProperty, false);
        Turn++;
    }
#endregion
#region State Independent
    public void OnSelectionCanceled() => State.SendEvent(CancelEvent);

    /// <summary>
    /// If the current state is skippable, either immediately end the turn for player-controlled armies or compute the end result of the turn
    /// for AI-controlled armies and apply it to the map, then end the turn.
    /// </summary>
    public void OnTurnFastForward()
    {
        // Reuse this signal for skipping to the end of the current army's turn, which should only happen for player-controlled armies
        if (!_armies.Current.Any(static (u) => u.UnitData.Active))
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

            _combat = GetChildren().OfType<CombatController>().FirstOrDefault();

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

            foreach (UnitAction action in AvailableActions)
                action.Initialize(this);
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