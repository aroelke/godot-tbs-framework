using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;
using TbsTemplate.Nodes;
using TbsTemplate.Scenes.Combat.Data;
using TbsTemplate.UI;
using TbsTemplate.Scenes.Level.Layers;
using TbsTemplate.Scenes.Combat;
using TbsTemplate.Nodes.Components;
using TbsTemplate.Scenes.Level.Control;
using TbsTemplate.Scenes.Transitions;

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
    private static readonly StringName SelectEvent = "Select";
    private static readonly StringName CancelEvent = "Cancel";
    private static readonly StringName SkipEvent   = "Skip";
    private static readonly StringName WaitEvent   = "Wait";
    private static readonly StringName DoneEvent   = "Done";
    // State chart conditions
    private readonly StringName OccupiedProperty    = "occupied";    // Current cell occupant (see below for options)
    private readonly StringName TargetProperty      = "target";      // Current cell contains a potential target (for attack or support)
    private readonly StringName TraversableProperty = "traversable"; // Current cell is traversable
    private readonly StringName ActiveProperty      = "active";      // Number of remaining active units
    // State chart occupied values
    private const string NotOccupied          = "";         // Nothing in the cell
    private const string SelectedOccuiped     = "selected"; // Cell occupied by the selected unit (if there is one)
    private const string ActiveAllyOccupied   = "active";   // Cell occupied by an active unit in this turn's army
    private const string InActiveAllyOccupied = "inactive"; // Cell occupied by an inactive unit in this turn's army
    private const string FriendlyOccuipied    = "friendly"; // Cell occupied by unit in army allied to this turn's army
    private const string EnemyOccupied        = "enemy";    // Cell occupied by unit in enemy army to this turn's army
    private const string OtherOccupied        = "other";    // Cell occupied by something else
#endregion
#region Declarations
    private readonly DynamicEnumProperties<StringName> _events = new([SelectEvent, CancelEvent, SkipEvent, WaitEvent, DoneEvent], @default:"");

    private Path _path = null;
    private Unit _selected = null, _target = null;
    private IEnumerator<Army> _armies = null;
    private Vector2I? _initialCell = null;
    private readonly Stack<BoundedNode2D> _cameraHistory = [];
    private StringName _command = null;
    private bool _ff = false;

    private Grid Grid = null;
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

    /// <summary>Update the UI turn counter for the current turn and change its color to match the army.</summary>
    private void UpdateTurnCounter()
    {
        TurnLabel.AddThemeColorOverride("font_color", _armies.Current.Faction.Color);
        TurnLabel.Text = $"Turn {Turn}: {_armies.Current.Faction.Name}";
    }
#endregion
#region Exports
    [Export(PropertyHint.File, "*.tscn")] public string CombatScenePath = null;

    /// <summary>Background music to play during the level.</summary>
    [Export] public AudioStream BackgroundMusic = null;

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
        _armies.Current.Controller.SelectionCanceled += OnSelectionCanceled;
        _armies.Current.Controller.UnitSelected += _.State.Root.Running.Idle.OnUnitSelected.React;
        _armies.Current.Controller.TurnSkipped += _.State.Root.Running.Idle.OnTurnSkipped.React;
        _armies.Current.Controller.TurnFastForward += OnTurnFastForward;
        _armies.Current.Controller.UnitCommanded += _.State.Root.Running.UnitSelected.OnUnitCommanded.React;
        _armies.Current.Controller.TargetChosen += _.State.Root.Running.UnitSelected.OnTargetChosen.React;
        _armies.Current.Controller.TargetCanceled += OnTargetingCanceled;
        _armies.Current.Controller.PathConfirmed += _.State.Root.Running.UnitSelected.OnPathConfirmed.React;
        _armies.Current.Controller.UnitCommanded += _.State.Root.Running.UnitCommanding.OnUnitCommanded.React;
        _armies.Current.Controller.TargetChosen += _.State.Root.Running.UnitTargeting.OnTargetChosen.React;

        _armies.Current.Controller.InitializeTurn();
        Callable.From<int, Army>((t, a) => LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.TurnBegan, t, a)).CallDeferred(Turn, _armies.Current);
    }

    /// <summary>Perform any updates that the turn has begun that need to happen after upkeep.</summary>
    public void OnBeginTurnExited() => UpdateTurnCounter();
#endregion
#region Idle State
    /// <summary>Update the UI when re-entering idle.</summary>
    public void OnIdleEntered() => Callable.From(_armies.Current.Controller.SelectUnit).CallDeferred();

    public void OnIdleUnitSelected(Unit unit)
    {
        if (unit.Army.Faction != _armies.Current.Faction)
            throw new InvalidOperationException($"Cannot select unit not from army {_armies.Current.Name}");
        if (!unit.Active)
            throw new InvalidOperationException($"Cannot select inactive unit {unit.Name}");

        _selected = unit;
        State.ExpressionProperties = State.ExpressionProperties.SetItem(OccupiedProperty, ActiveAllyOccupied);

        State.SendEvent(_events[SelectEvent]);
    }

    public void OnIdleTurnSkipped()
    {
        foreach (Unit unit in (IEnumerable<Unit>)_armies.Current)
            unit.Finish();
        State.SendEvent(_events[SkipEvent]);
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
        _path = Path.Empty(Grid, _selected.TraversableCells());

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
        if (path.Any((c) => Grid.Occupants.ContainsKey(c) && (!(Grid.Occupants[c] as Unit)?.Army.Faction.AlliedTo(_selected) ?? false)))
            throw new InvalidOperationException("The chosen path must only contain traversable cells.");
        if (Grid.Occupants.ContainsKey(path[^1]) && Grid.Occupants[path[^1]] != unit)
            throw new InvalidOperationException("The chosen path must not end on an occupied cell.");

        State.ExpressionProperties = State.ExpressionProperties.SetItem(TraversableProperty, true);
        if (_ff)
        {
            Grid.Occupants.Remove(_selected.Cell);
            _selected.Cell = path[^1];
            Grid.Occupants[path[^1]] = _selected;
            State.SendEvent(_events[SkipEvent]);
        }
        else
        {
            _path = _path.SetTo(path);
            State.SendEvent(_events[SelectEvent]);
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
        Action FinishMoving(StringName @event) => () => {
            if (SkipTurnTransition.Active)
                SkipTurnTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => State.SendEvent(_events[@event]), (uint)ConnectFlags.OneShot);
            else
                State.SendEvent(_events[@event]);
        };

        // Track the unit as it's moving
        _prevDeadzone = new(Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight);
        (Camera.DeadZoneTop, Camera.DeadZoneLeft, Camera.DeadZoneBottom, Camera.DeadZoneRight) = Vector4.Zero;
        PushCameraFocus(_selected.MotionBox);

        // Move the unit
        Grid.Occupants.Remove(_selected.Cell);
        _selected.Connect(Unit.SignalName.DoneMoving, FinishMoving(_target is null ? DoneEvent : SkipEvent), (uint)ConnectFlags.OneShot);
        Grid.Occupants[_path[^1]] = _selected;
        _selected.MoveAlong(_path); // must be last in case it fires right away
    }

    /// <summary>Press the cancel button during movement to skip to the end.</summary>
    public void OnMovingEventReceived(StringName @event)
    {
        if (@event == _events[CancelEvent])
            _selected.SkipMoving();
    }

    /// <summary>When done moving, restore the <see cref="Camera2DBrain">camera</see> target (most likely to the cursor) and update danger zones.</summary>
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
                    State.SendEvent(_events[SelectEvent]);
                }));
            }
        }
        AddActionOption(UnitActions.AttackAction, _selected.AttackableCells().Where((c) => !(Grid.Occupants.GetValueOrDefault(c) as Unit)?.Army.Faction.AlliedTo(_selected) ?? false));
        AddActionOption(UnitActions.SupportAction, _selected.SupportableCells().Where((c) => (Grid.Occupants.GetValueOrDefault(c) as Unit)?.Army.Faction.AlliedTo(_selected) ?? false));
        foreach (SpecialActionRegion region in Grid.SpecialActionRegions)
        {
            if (region.HasSpecialAction(_selected, _selected.Cell))
            {
                _options.Add(new(region.Name, () => {
                    region.PerformSpecialAction(_selected, _selected.Cell);
                    State.SendEvent(_events[SkipEvent]);
                }));
            }
        }
        _options.Add(new(UnitActions.EndAction, () => State.SendEvent(_events[SkipEvent])));
        _options.Add(new("Cancel", () => State.SendEvent(_events[CancelEvent])));

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
        Grid.Occupants.Remove(_selected.Cell);
        _selected.Cell = _initialCell.Value;
        Grid.Occupants[_selected.Cell] = _selected;
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
        if ((_command == UnitActions.AttackAction && target.Army.Faction.AlliedTo(_selected)) || (_command == UnitActions.SupportAction && !target.Army.Faction.AlliedTo(_selected)))
            throw new ArgumentException($"{_selected.Name} cannot {_command} {target.Name}");
        _target = target;
        State.SendEvent(_events[DoneEvent]);
    }

    public void OnTargetingCanceled(Unit source) => State.SendEvent(_events[CancelEvent]);
#endregion
#region In Combat
    private void ApplyCombatResults()
    {
        foreach (CombatAction action in _combatResults)
            if (action.Actor.Health.Value > 0 && action.Hit)
                action.Target.Health.Value -= action.Damage;
        _target = null;
        _combatResults = null;
    }

    public void OnCombatEntered()
    {
        if (_command == UnitActions.AttackAction)
            _combatResults = CombatCalculations.AttackResults(_selected, _target);
        else if (_command == UnitActions.SupportAction)
            _combatResults = [CombatCalculations.CreateSupportAction(_selected, _target)];
        else
            throw new NotSupportedException($"Unknown action {_command}");

        if (_ff)
        {
            ApplyCombatResults();
            State.SendEvent(_events[DoneEvent]);
        }
        else
        {
            SceneManager.Singleton.Connect<CombatScene>(SceneManager.SignalName.SceneLoaded, (s) => s.Initialize(_selected, _target, _combatResults.ToImmutableList()), (uint)ConnectFlags.OneShot);
            SceneManager.CallScene(CombatScenePath);
        }
    }

    /// <summary>Update the map to reflect combat results when it's added back to the tree.</summary>
    public void OnCombatEnteredTree()
    {
        ApplyCombatResults();
        SceneManager.Singleton.Connect(SceneManager.SignalName.TransitionCompleted, () => State.SendEvent(_events[DoneEvent]), (uint)ConnectFlags.OneShot);
    }
#endregion
#region End Action State
    /// <summary>Signal that a unit's action has ended.</summary>
    public void OnEndActionEntered()
    {
        _armies.Current.Controller.FinalizeAction();
        _selected.Finish();
        State.ExpressionProperties = State.ExpressionProperties.SetItem(ActiveProperty, ((IEnumerable<Unit>)_armies.Current).Count((u) => u.Active));

        Callable.From<Unit>((u) => LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.ActionEnded, u)).CallDeferred(_selected);
    }

    /// <summary>Clean up at the end of the unit's turn.</summary>
    public void OnEndActionExited()
    {
        if (_selected.Health.Value <= 0)
            _selected.Die();
        _selected = null;
    }
#endregion
#region End Turn State
    public void Turnover() => Callable.From<int, Army>((t, a) => LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.TurnEnded, t, a)).CallDeferred(Turn, _armies.Current);

    /// <summary>After a delay, signal that the turn is ending and wait for a response.</summary>
    public void OnEndTurnEntered()
    {
        if (_ff)
        {
            SkipTurnTransition.Connect(SceneTransition.SignalName.TransitionedIn, () => TurnAdvance.Start(), (uint)ConnectFlags.OneShot);
            SkipTurnTransition.TransitionIn();
        }
        else
            TurnAdvance.Start();
    }

    /// <summary>Refresh all the units in the army whose turn just ended so they aren't gray anymore and are animated.</summary>
    public void OnEndTurnExited()
    {
        _ff = false;

        _armies.Current.Controller.SelectionCanceled -= OnSelectionCanceled;
        _armies.Current.Controller.UnitSelected -= _.State.Root.Running.Idle.OnUnitSelected.React;
        _armies.Current.Controller.TurnSkipped -= _.State.Root.Running.Idle.OnTurnSkipped.React;
        _armies.Current.Controller.TurnFastForward -= OnTurnFastForward;
        _armies.Current.Controller.UnitCommanded -= _.State.Root.Running.UnitSelected.OnUnitCommanded.React;
        _armies.Current.Controller.TargetChosen -= _.State.Root.Running.UnitSelected.OnTargetChosen.React;
        _armies.Current.Controller.PathConfirmed -= _.State.Root.Running.UnitSelected.OnPathConfirmed.React;
        _armies.Current.Controller.UnitCommanded -= _.State.Root.Running.UnitCommanding.OnUnitCommanded.React;
        _armies.Current.Controller.TargetChosen -= _.State.Root.Running.UnitTargeting.OnTargetChosen.React;
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
    public void OnSelectionCanceled() => State.SendEvent(_events[CancelEvent]);

    public void OnTurnFastForward()
    {
        SkipTurnTransition.TransitionOut();
        SkipTurnTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => _ff = true, (uint)ConnectFlags.OneShot);
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
    public void OnEventComplete() => State.SendEvent(_events[DoneEvent]);

    /// <summary>When the pointer starts flying, we need to wait for it to finish. Also focus the camera on its target if there's something there.</summary>
    /// <param name="target">Position the pointer is going to fly to.</param>
    public void OnPointerFlightStarted(Vector2 target)
    {
        State.SendEvent(_events[WaitEvent]);
        PushCameraFocus(Grid.Occupants.ContainsKey(Grid.CellOf(target)) ? Grid.Occupants[Grid.CellOf(target)] : Camera.Target);
    }

    /// <summary>When the pointer finished flying, return to the previous state.</summary>
    public void OnPointerFlightCompleted()
    {
        PopCameraFocus();
        State.SendEvent(_events[DoneEvent]);
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

    public override void _EnterTree()
    {
        base._EnterTree();
        if (!Engine.IsEditorHint())
        {
            MusicController.Resume(BackgroundMusic);
            MusicController.FadeIn(SceneManager.CurrentTransition.TransitionTime/2);
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            Grid = GetNode<Grid>("Grid");

            Camera.Limits = new(Vector2I.Zero, (Vector2I)(Grid.Size*Grid.CellSize));
            LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.CameraBoundsUpdated, Camera.Limits);

            foreach (Army army in GetChildren().OfType<Army>())
            {
                army.Controller.Grid = Grid;
                foreach (Unit unit in (IEnumerable<Unit>)army)
                {
                    unit.Grid = Grid;
                    unit.Cell = Grid.CellOf(unit.GlobalPosition - Grid.GlobalPosition);
                    Grid.Occupants[unit.Cell] = unit;
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

            MusicController.ResetPlayback();
        }
    }
#endregion
#region Editor
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

        // Make sure there's background music
        if (BackgroundMusic is null)
            warnings.Add("Background music hasn't been added. Whatever's playing will stop.");

        if (_events[SelectEvent].IsEmpty)
            warnings.Add("The \"select\" state chart event is not set. Units can't be selected.");
        if (_events[CancelEvent].IsEmpty)
            warnings.Add("The \"cancel\" state chart event is not set. Selections can't be canceled.");
        if (_events[SkipEvent].IsEmpty)
            warnings.Add("The \"skip\" state chart event is not set. Certain command shortcuts can't be made.");
        if (_events[WaitEvent].IsEmpty)
            warnings.Add("The \"wait\" state chart event is not set. The level won't block for processes.");
        if (_events[DoneEvent].IsEmpty)
            warnings.Add("The \"done\" state chart event is not set. The level won't block for processes.");

        return [.. warnings];
    }
#endregion
}