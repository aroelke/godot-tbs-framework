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
    private Unit _selected = null, _target = null;
    private IEnumerator<Army> _armies = null;
    private Vector2I? _initialCell = null;
    private readonly Stack<BoundedNode2D> _cameraHistory = [];
    private StringName _command = null;
    private bool _ff = false;

    private Grid Grid = null;
    private Camera2DController   Camera                            => _cache.GetNode<Camera2DController>("Camera");
    private CanvasLayer     UserInterface                     => _cache.GetNode<CanvasLayer>("UserInterface");
    private StateChart           State                             => _cache.GetNode<StateChart>("State");
    private ActionReaction  OnSkipTurnReaction                => _cache.GetNode<ActionReaction>("State/Root/Running/Skippable/OnFastForward");
    private UnitReaction    OnUnitSelectedReaction            => _cache.GetNode<UnitReaction>("State/Root/Running/Skippable/Idle/OnUnitSelected");
    private PathReaction    OnPathConfirmedReaction           => _cache.GetNode<PathReaction>("State/Root/Running/Skippable/UnitSelected/OnPathConfirmed");
    private CommandReaction OnSelectedUnitCommandedReaction   => _cache.GetNode<CommandReaction>("State/Root/Running/Skippable/UnitSelected/OnUnitCommanded");
    private TargetReaction  OnSelectedTargetChosenReaction    => _cache.GetNode<TargetReaction>("State/Root/Running/Skippable/UnitSelected/OnTargetChosen");
    private CommandReaction OnCommandingUnitCommandedReaction => _cache.GetNode<CommandReaction>("State/Root/Running/Skippable/UnitCommanding/OnUnitCommanded");
    private TargetReaction  OnTargetingTargetChosenReaction   => _cache.GetNode<TargetReaction>("State/Root/Running/Skippable/UnitTargeting/OnTargetChosen");
    private Timer           TurnAdvance                       => _cache.GetNode<Timer>("TurnAdvance");

    public LevelManager() : base() { _cache = new(this); }
#endregion
#region Helper Properties and Methods
    private Vector2 MenuPosition(Rect2 rect, Vector2 size)
    {
        Rect2 viewportRect = Grid.GetGlobalTransformWithCanvas()*rect;
        float viewportCenter = GetViewport().GetVisibleRect().Position.X + GetViewport().GetVisibleRect().Size.X/2;
        return new(
            viewportCenter - viewportRect.Position.X < viewportRect.Size.X/2 ? viewportRect.Position.X - size.X : viewportRect.End.X,
            Mathf.Clamp(viewportRect.Position.Y - (size.Y - viewportRect.Size.Y)/2, 0, GetViewport().GetVisibleRect().Size.Y - size.Y)
        );
    }
#endregion
#region Exports
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
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.UnitSelected, Callable.From<Unit>(OnUnitSelectedReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.PathConfirmed, Callable.From<Unit, Godot.Collections.Array<Vector2I>>(OnPathConfirmedReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.UnitCommanded, Callable.From<Unit, StringName>(OnSelectedUnitCommandedReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.TargetChosen, Callable.From<Unit, Unit>(OnSelectedTargetChosenReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.TargetCanceled, Callable.From<Unit>(OnTargetingCanceled));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.UnitCommanded, Callable.From<Unit, StringName>(OnCommandingUnitCommandedReaction.React));
        _armies.Current.Controller.ConnectForTurn(ArmyController.SignalName.TargetChosen, Callable.From<Unit, Unit>(OnTargetingTargetChosenReaction.React));

        _armies.Current.Controller.InitializeTurn();
        Callable.From<int, Army>((t, a) => LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.TurnBegan, t, a)).CallDeferred(Turn, _armies.Current);
    }
#endregion
#region Idle State
    /// <summary>Update the UI when re-entering idle.</summary>
    public void OnIdleEntered() => Callable.From(_armies.Current.Controller.SelectUnit).CallDeferred();

    public void OnIdleUnitSelected(Unit unit)
    {
        if (unit.Army.Faction != _armies.Current.Faction)
            throw new InvalidOperationException($"Cannot select unit not from army {_armies.Current.Name}");
        if (!unit.UnitData.Active)
            throw new InvalidOperationException($"Cannot select inactive unit {unit.Name}");

        _selected = unit;
        State.SendEvent(SelectEvent);
    }
#endregion
#region Unit Selected State
    /// <summary>Set the selected <see cref="Unit"/> to its idle state and the deselect it.</summary>
    private void DeselectUnit()
    {
        _initialCell = null;
        _selected.Deselect();
        _selected = null;
    }

    /// <summary>Display the total movement, attack, and support ranges of the selected <see cref="Unit"/> and begin drawing the path arrow for it to move on.</summary>
    public void OnSelectedEntered()
    {
        _selected.Select();
        _initialCell = _selected.Cell;
        _command = null;
        _target = null;

        // Compute move/attack/support ranges for selected unit
        _path = Path.Empty(Grid.Data, _selected.UnitData.GetTraversableCells());

        // If the camera isn't zoomed out enough to show the whole range, zoom out so it does
        Rect2? zoomRect = null; // Grid.EnclosingRect(ActionLayers.Union());
        if (zoomRect is not null)
        {
            Vector2 zoomTarget = Grid.GetViewportRect().Size/zoomRect.Value.Size;
            zoomTarget = Vector2.One*Mathf.Min(zoomTarget.X, zoomTarget.Y);
            if (Camera.Zoom > zoomTarget)
                Camera.PushZoom(zoomTarget);
        }

        Callable.From<Unit>(_armies.Current.Controller.MoveUnit).CallDeferred(_selected);
    }

    public void OnSelectedUnitCommanded(Unit unit, StringName command)
    {
        if (unit != _selected)
            throw new InvalidOperationException($"Cannot command unselected unit {unit.Name} ({_selected.Name} is selected)");
        _command = command;
    }

    public void OnSelectedTargetChosen(Unit source, Unit target)
    {
        if (source != _selected)
            throw new InvalidOperationException($"Cannot choose action target for unselected unit {source.Name} ({_selected.Name} is selected)");
        _target = target;
    }

    public void OnSelectedPathConfirmed(Unit unit, Godot.Collections.Array<Vector2I> path)
    {
        if (unit != _selected)
            throw new InvalidOperationException($"Cannot confirm path for unselected unit {unit.Name} ({_selected.Name} is selected)");
        if (path.Any((c) => Grid.Data.Occupants.ContainsKey(c) && (!(Grid.Data.Occupants[c] as UnitData)?.Faction.AlliedTo(_selected.UnitData) ?? false)))
            throw new InvalidOperationException("The chosen path must only contain traversable cells.");
        if (Grid.Data.Occupants.ContainsKey(path[^1]) && Grid.Data.Occupants[path[^1]] != unit.Data)
            throw new InvalidOperationException("The chosen path must not end on an occupied cell.");

        State.SetVariable(TraversableProperty, true);
        if (_ff)
        {
            _selected.Cell = path[^1];
            State.SendEvent(SkipEvent);
        }
        else
        {
            _path = _path.SetTo(path);
            State.SendEvent(SelectEvent);
        }
    }

    public void OnSelectedCanceled()
    {
        _initialCell = null;
        _selected.Deselect();
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
        PushCameraFocus(_selected.MotionBox);

        // Move the unit
//        Grid.Data.Occupants.Remove(_selected.Cell);
        _selected.Connect(Unit.SignalName.DoneMoving, _target is null ? () => State.SendEvent(DoneEvent) : () => State.SendEvent(SkipEvent), (uint)ConnectFlags.OneShot);
//        Grid.Data.Occupants[_path[^1]] = _selected.Data;
        _selected.MoveAlong(_path); // must be last in case it fires right away
    }

    /// <summary>Press the cancel button during movement to skip to the end.</summary>
    public void OnMovingEventReceived(StringName @event)
    {
        if (@event == CancelEvent)
            _selected.SkipMoving();
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
        AddActionOption(UnitAction.AttackAction, _selected.UnitData.GetAttackableCells().Where((c) => !(Grid.Data.Occupants.GetValueOrDefault(c) as UnitData)?.Faction.AlliedTo(_selected.UnitData) ?? false));
        AddActionOption(UnitAction.SupportAction, _selected.UnitData.GetSupportableCells().Where((c) => (Grid.Data.Occupants.GetValueOrDefault(c) as UnitData)?.Faction.AlliedTo(_selected.UnitData) ?? false));
        foreach (SpecialActionRegion region in Grid.SpecialActionRegions)
        {
            if (region.HasSpecialAction(_selected, _selected.Cell))
            {
                _options.Add(new(region.Name, () => {
                    region.PerformSpecialAction(_selected, _selected.Cell);
                    State.SendEvent(SkipEvent);
                }));
            }
        }
        _options.Add(new(UnitAction.EndAction, () => State.SendEvent(SkipEvent)));
        _options.Add(new("Cancel", () => State.SendEvent(CancelEvent)));

        Callable.From<Unit, Godot.Collections.Array<StringName>, StringName>(_armies.Current.Controller.CommandUnit).CallDeferred(_selected, new Godot.Collections.Array<StringName>(_options.Select((o) => o.Name)), "Cancel");
    }

    public void OnCommandingUnitCommanded(Unit unit, StringName command)
    {
        if (unit != _selected)
            throw new InvalidOperationException($"Cannon command unselected unit {unit.Name} ({_selected.Name} is selected)");
        foreach (ContextMenuOption option in _options)
            if (option.Name == command)
                option.Action();
    }

    /// <summary>Move the selected <see cref="Unit"/> and <see cref="Object.Cursor"/> back to the cell the unit was at before it moved.</summary>
    public void OnCommandingCanceled()
    {
        _command = null;

        // Move the selected unit back to its original cell
//        Grid.Data.Occupants.Remove(_selected.Cell);
        _selected.Cell = _initialCell.Value;
//        Grid.Data.Occupants[_selected.Cell] = _selected;
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

    public void OnTargetChosen(Unit source, Unit target)
    {
        if (source != _selected)
            throw new InvalidOperationException($"Cannot choose target for unselected unit {source.Name} ({_selected.Name} is selected)");
        if ((_command == UnitAction.AttackAction && target.Army.Faction.AlliedTo(_selected.UnitData)) || (_command == UnitAction.SupportAction && !target.Army.Faction.AlliedTo(_selected.UnitData)))
            throw new ArgumentException($"{_selected.Name} cannot {_command} {target.Name}");
        _target = target;
        State.SendEvent(DoneEvent);
    }

    public void OnTargetingCanceled(Unit source) => State.SendEvent(CancelEvent);
#endregion
#region In Combat
    private void ApplyCombatResults()
    {
        foreach (CombatAction action in _combatResults)
            if (action.Hit)
                action.Target.Health -= action.Damage;
        _target = null;
        _combatResults = null;
    }

    public void OnCombatEntered()
    {
        if (_command == UnitAction.AttackAction)
            _combatResults = CombatCalculations.AttackResults(_selected.UnitData, _target.UnitData, false);
        else if (_command == UnitAction.SupportAction)
            _combatResults = [CombatCalculations.CreateSupportAction(_selected.UnitData, _target.UnitData)];
        else
            throw new NotSupportedException($"Unknown action {_command}");

        if (_ff)
        {
            ApplyCombatResults();
            State.SendEvent(DoneEvent);
        }
        else
        {
            SceneManager.Singleton.Connect<CombatScene>(SceneManager.SignalName.SceneLoaded, (s) => s.Initialize(_selected.UnitData, _target.UnitData, _combatResults.ToImmutableList()), (uint)ConnectFlags.OneShot);
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
        _selected.Finish();
        State.SetVariable(ActiveProperty, ((IEnumerable<Unit>)_armies.Current).Count(static (u) => u.UnitData.Active));

        Callable.From<Unit>((u) => LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.ActionEnded, u)).CallDeferred(_selected);
    }

    /// <summary>Clean up at the end of the unit's turn.</summary>
    public void OnEndActionExited()
    {
        if (_selected.UnitData.Health <= 0)
            _selected.Die();
        _selected = null;
    }
#endregion
#region End Turn State
    public void Turnover() => Callable.From<int, Army>((t, a) => LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.TurnEnded, t, a)).CallDeferred(Turn, _armies.Current);

    /// <summary>After a delay, signal that the turn is ending and wait for a response.</summary>
    public void OnEndTurnEntered() => TurnAdvance.Start();

    /// <summary>Refresh all the units in the army whose turn just ended so they aren't gray anymore and are animated.</summary>
    public void OnEndTurnExited()
    {
        _ff = false;
        _armies.Current.Controller.FinalizeTurn();

        foreach (Unit unit in (IEnumerable<Unit>)_armies.Current)
            unit.Refresh();

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

    /// <summary>When the pointer starts flying, we need to wait for it to finish. Also focus the camera on its target if there's something there.</summary>
    /// <param name="target">Position the pointer is going to fly to.</param>
    public void OnPointerFlightStarted(Vector2 target)
    {
        State.SendEvent(WaitEvent);
        PushCameraFocus(Grid.Data.Occupants.ContainsKey(Grid.CellOf(target)) ? (Grid.Data.Occupants[Grid.CellOf(target)] as UnitData).Renderer : Camera.Target);
    }

    /// <summary>When the pointer finished flying, return to the previous state.</summary>
    public void OnPointerFlightCompleted()
    {
        PopCameraFocus();
        State.SendEvent(DoneEvent);
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
        if (child is GridNode gd)
            gd.Grid = Engine.IsEditorHint() ? GetNode<Grid>("Grid") : Grid;
    }

    public void OnUnitDefeated(Unit defeated)
    {
        // If the dead unit is the currently-selected one, it will be cleared away at the end of its action.
        if (_selected != defeated)
            defeated.Die();
        else // Otherwise, pretend it's dead by removing it from the scene tree and making it invisible until the action is over
        {
            _armies.Current.RemoveChild(_selected);
            defeated.Visible = false;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            Grid = GetNode<Grid>("Grid");

            Camera.Limits = new(Vector2I.Zero, (Vector2I)(Grid.Data.Size*Grid.CellSize));
            LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.CameraBoundsUpdated, Camera.Limits);

            foreach (Army army in GetChildren().OfType<Army>())
            {
                army.Controller.Grid = Grid;
                foreach (Unit unit in (IEnumerable<Unit>)army)
                {
                    unit.Grid = Grid;
                    unit.Cell = Grid.CellOf(unit.GlobalPosition - Grid.GlobalPosition);
                }
            }
            LevelEvents.Singleton.Connect<Unit>(LevelEvents.SignalName.UnitDefeated, OnUnitDefeated);

            _armies = GetChildren().OfType<Army>().GetCyclicalEnumerator();
            if (StartingArmy is null)
                StartingArmy = _armies.Current;
            else // Advance the army enumerator until it's pointing at StartingArmy
                while (_armies.Current != StartingArmy)
                    if (!_armies.MoveNext())
                        break;

            LevelEvents.Singleton.Connect<BoundedNode2D>(LevelEvents.SignalName.FocusCamera, PushCameraFocus);
            LevelEvents.Singleton.Connect(LevelEvents.SignalName.RevertCameraFocus, PopCameraFocus);
            LevelEvents.Singleton.Connect(LevelEvents.SignalName.EventComplete, OnEventComplete);
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