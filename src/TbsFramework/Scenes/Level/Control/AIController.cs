using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes.Combat;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Actions;
using TbsFramework.Scenes.Rendering;
using TbsFramework.Scenes.Transitions;
using TbsFramework.UI.Controls.Device;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="Behavior"/>s and the state of the level.</summary>
[Icon("uid://bambqlpd7p4kr"), Tool]
public partial class AIController : ArmyController
{
    private const int HealthDiffPrecision = 10;

    /// <summary>Signals that the fast-forward state has changed.</summary>
    /// <param name="enable"><c>true</c> if fast-forwarding is in progress, and <c>false</c> otherwise.</param>
    [Signal] public delegate void FastForwardStateChangedEventHandler(bool enable);

    private class VirtualAction : IEquatable<VirtualAction>, IComparable<VirtualAction>
    {
        public static bool operator >(VirtualAction a, VirtualAction b) => a.CompareTo(b) > 0;
        public static bool operator <(VirtualAction a, VirtualAction b) => a.CompareTo(b) < 0;

        private readonly List<UnitData> _enemies = [];
        private readonly HashSet<UnitData> _allies = [];
        private Vector2I _destination = -Vector2I.One;
        private GridData _result = null;

        public VirtualAction(UnitData actor, UnitAction action, Vector2I destination, Vector2I target, IEnumerable<Vector2I> traversable)
        {
            Actor = actor;
            Action = action;
            Traversable = traversable;
            Destination = destination;
            Target = target;
            Start = actor?.Cell ?? -Vector2I.One;
        }

        private VirtualAction(VirtualAction original) : this(original.Actor.Grid.Clone().Occupants[original.Actor.Cell], original.Action, original.Destination, original.Target, original.Traversable)
        {
            Start = original.Start;

            Result = original.Result;
            SpecialActionsPerformed = original.SpecialActionsPerformed;
            DefeatedEnemies = original.DefeatedEnemies;
            DefeatedAllies = original.DefeatedAllies;
            RemainingActions = original.RemainingActions;
        }

        public UnitData Actor;
        public UnitAction Action;
        public Vector2I Target = -Vector2I.One;
        public IEnumerable<Vector2I> Traversable;
        public Vector2I Start = -Vector2I.One;

        public Vector2I Destination
        {
            get => _destination;
            set
            {
                _destination = value;
                PathCost = Actor.PathCost(Path.Empty(Actor.Grid, Traversable).Add(Start).Add(_destination));
            }
        }

        public int SpecialActionsPerformed = 0;
        public int DefeatedEnemies = 0;
        public int DefeatedAllies = 0;
        public double AllyHealthDifference = 0;
        public double EnemyHealthDifference = 0;
        public int PathCost = 0;
        public int RemainingActions = 0;

        public GridData Result
        {
            get => _result;
            set
            {
                if ((_result = value) is not null)
                {
                    foreach ((_, GridObjectData obj) in _result.Occupants)
                    {
                        if (obj is UnitData unit)
                        {
                            if (Actor.Faction.AlliedTo(unit.Faction))
                            {
                                _allies.Add(unit);
                                if (unit.Health <= 0)
                                    DefeatedAllies++;
                                AllyHealthDifference += unit.Stats.Health - unit.Health;
                            }
                            else
                            {
                                _enemies.Add(unit);
                                if (unit.Health <= 0)
                                    DefeatedEnemies++;
                                EnemyHealthDifference += unit.Stats.Health - unit.Health;
                            }
                        }
                    }

                    _enemies.Sort((a, b) => (int)((a.Health - b.Health)*HealthDiffPrecision));
                }
            }
        }

        public VirtualAction Clone() => new(this);

        public bool Equals(VirtualAction other) => other is not null && Actor == other.Actor && Actor.Grid == other.Actor.Grid && Action == other.Action && Target == other.Target && Destination == other.Destination;
        public override bool Equals(object obj) => Equals(obj as VirtualAction);

        // Positive is better
        public int CompareTo(VirtualAction other)
        {
            int diff;

            if ((diff = SpecialActionsPerformed - other.SpecialActionsPerformed) != 0)
                return diff;

            if ((diff = DefeatedEnemies - other.DefeatedEnemies) != 0)
                return diff;
            if ((diff = other.DefeatedAllies - DefeatedAllies) != 0)
                return diff;

            if (Action.AffectsHealth && (diff = (int)((other.AllyHealthDifference - AllyHealthDifference)*HealthDiffPrecision)) != 0)
                return diff;

            int smaller = Math.Min(_enemies.Count, other._enemies.Count);
            for (int i = 0; i < smaller; i++)
                if (_enemies[i].Health != other._enemies[i].Health)
                    return (int)((other._enemies[i].Health - _enemies[i].Health)*HealthDiffPrecision);

            if ((diff = (int)((EnemyHealthDifference - other.EnemyHealthDifference)*HealthDiffPrecision)) != 0)
                return diff;

            if ((diff = (int)((other.AllyHealthDifference - AllyHealthDifference)*HealthDiffPrecision)) != 0)
                return diff;

            if ((diff = RemainingActions - other.RemainingActions) != 0)
                return diff;

            return other.PathCost - PathCost;
        }

        public override string ToString() => $"Move {Actor.Faction.Name}@{Start} to {Destination} and {Action} {Target}";
        public override int GetHashCode() => HashCode.Combine(Actor, Actor.Grid, Action, Target, Destination);
    }

    private static List<VirtualAction> GetAvailableActions(GridData grid, Faction faction, IEnumerable<UnitAction> available)
    {
        List<VirtualAction> actions = [];
        foreach ((_, UnitData unit) in grid.Occupants)
        {
            if (unit.Faction == faction && unit.Active && unit.Health > 0)
            {
                foreach (ActionInfo action in unit.Behavior.Actions(unit, available))
                {
                    IEnumerable<Vector2I> destinations = action.Action.GetSourceCells(unit, action.Target).Intersect(action.Traversable);
                    UnitData target = action.Action.RequiresTarget ? unit.Grid.Occupants[action.Target] : null;

                    // If the action allows for retaliation, prioritize cells that the target can't retaliate on
                    if (action.Action.RequiresTarget && action.Action.RetaliationAllowed)
                    {
                        IEnumerable<Vector2I> safe = destinations.Where((c) => !action.Action.CanPerform(target, target.Cell, c));
                        if (safe.Any())
                            destinations = safe;
                    }

                    // Prioritize the destination closest to the actor's current cell, but if that cell is the actor's current cell then
                    // also try the next-best one in case moving another unit to that cell afterward is overall better
                    Vector2I best = destinations.MinBy((c) => c.ManhattanDistanceTo(unit.Cell));
                    actions.Add(new(unit, action.Action, best, action.Target, action.Traversable));
                    if (best == unit.Cell && destinations.Count() > 1)
                        actions.Add(new(unit, action.Action, destinations.Where((c) => c != best).MinBy((c) => c.ManhattanDistanceTo(unit.Cell)), action.Target, action.Traversable));
                }
            }
        }

        return actions;
    }

    private static VirtualAction EvaluateAction(IEnumerable<VirtualAction> actions, VirtualAction action, Dictionary<GridData, VirtualAction> decisions, IEnumerable<UnitAction> available, int remaining)
    {
        action.Result = action.Action.Simulate(action.Actor, action.Destination, action.Target);
        action.Result.Occupants[action.Destination].Active = false;

        // If this action results in a board state that was already explored, skip the rest of this branch and use that result
        if (decisions.TryGetValue(action.Result, out VirtualAction decision))
            return decision;

        IEnumerable<VirtualAction> further = GetAvailableActions(action.Result, action.Actor.Faction, available);
        if (remaining == 0 || remaining > 1)
        {
            remaining = Math.Max(0, remaining - 1);
            IEnumerable<VirtualAction> reduced = further.Where((a) => {
                // Evaluate a if a was not present in the previous set of actions that were evaluated (the other ones will either reappear or be evaluated later)
                if (!actions.Any((b) => a.Actor == b.Actor && a.Target == b.Target))
                    return true;
                // Evaluate a if a or this action was not an attack action
                if (!action.Action.RequiresTarget)
                    return true;
                // Don't evaluate a if a's target is defeated
                if (a.Actor.Grid.Occupants[a.Target].Health <= 0)
                    return false;
                // Evaluate a if this action defeated its target to see if more enemies can be defeated down this branch
                if (action.Result.Occupants[action.Target].Health <= 0)
                    return true;
                // Evaluate a if it has the same target as this action
                if (a.Target == action.Target)
                    return true;
                return false;
            });
            if (reduced.Any())
            {
                decisions[action.Result] = action.Clone();
                IEnumerable<VirtualAction> results = reduced.Select((a) => EvaluateAction(reduced, a, decisions, available, remaining));
                decisions[action.Result].Result = results.Max().Result;
            }
        }
        if (!decisions.TryGetValue(action.Result, out VirtualAction value))
        {
            decisions[action.Result] = value = action;
            decisions[action.Result].RemainingActions = further.Count();
        }
        return value;
    }

    public (UnitData selected, Vector2I destination, UnitAction action, Vector2I target) ComputeAction(IEnumerable<UnitData> available, IEnumerable<UnitAction> actions)
    {
        UnitData selected = null;
        Vector2I destination = -Vector2I.One;
        UnitAction action = null;
        Vector2I target;

        IEnumerable<UnitAction> targetable = actions.Where((a) => a.RequiresTarget);
        List<VirtualAction> potential = [.. GetAvailableActions(Grid.Data, Faction, targetable)];
        if (potential.Count != 0)
        {
            VirtualAction result;
            if (EvaluateWithThreads)
                result = Task.WhenAll([.. potential.Select((a) => Task.Run(() => EvaluateAction(potential, a, [], targetable, MaxSearchDepth)))]).Result.Max();
            else
            {
                Dictionary<GridData, VirtualAction> decisions = [];
                result = potential.Max((a) => EvaluateAction(potential, a, decisions, targetable, MaxSearchDepth));
            }
            selected = result.Actor;
            destination = result.Destination;
            action = result.Action;
            target = result.Target;
        }
        else
        {
            IEnumerable<UnitData> enemies = Grid.Data.Occupants.Values.Where((o) => o is UnitData u && !u.Faction.AlliedTo(Faction)).OfType<UnitData>();

            selected = enemies.Any() ? available.MinBy((u) => enemies.Min((e) => u.Cell.DistanceTo(e.Cell))) : available.First();
            action = actions.FirstOrDefault((a) => !a.RequiresTarget);

            IEnumerable<UnitData> ordered = enemies.OrderBy((u) => u.Cell.DistanceTo(selected.Cell));
            if (ordered.Any())
                destination = selected.Behavior.Destinations(selected).OrderBy((c) => selected.PathCost(selected.Behavior.GetPath(selected, c))).OrderBy((c) => c.DistanceTo(ordered.First().Cell)).First();
            else
                destination = selected.Cell;
            target = -Vector2I.One;
        }

        return (selected, destination, action, target);
    }

    private readonly NodeCache _cache = null;
    private UnitData _selected = null;
    private Vector2I _destination = -Vector2I.One;
    private UnitAction _action = null;
    private UnitData _target = null;
    private bool _ff = false;

    private Sprite2D              Pseudocursor          => _cache.GetNode<Sprite2D>("Pseudocursor");
    private FadeToBlackTransition FastForwardTransition => _cache.GetNode<FadeToBlackTransition>("CanvasLayer/FastForwardTransition");
    private Timer                 IndicatorTimer        => _cache.GetNode<Timer>("IndicatorTimer");

    public override Grid Grid { get; set; } = null;

    /// <summary>Sprite to use for the pseudocursor.</summary>
    [Export] public Texture2D CursorSprite
    {
        get => Pseudocursor?.Texture;
        set
        {
            if (Pseudocursor is not null)
                Pseudocursor.Texture = value;
        }
    }

    /// <summary>Pseudocursor sprite offset from the origin of the texture to use for positioning it within a cell.</summary>
    [Export] public Vector2 CursorOffset
    {
        get => Pseudocursor?.Offset ?? Vector2.Zero;
        set
        {
            if (Pseudocursor is not null)
                Pseudocursor.Offset = value;
        }
    }

    /// <summary>Time in seconds to hold the cursor over an indicated cell before acting on it.</summary>
    [Export(PropertyHint.None, "suffix:s")] public float IndicationTime = 0.5f;

    /// <summary>Whether or not this army's turn can be skipped.</summary>
    [Export] public bool EnableTurnSkipping = true;

    /// <summary>Maximum number of levels in the action tree to search for the best action.</summary>
    /// <remarks><b>Warning</b>: Be careful of increasing this higher than 3, or even 2 in some cases, as it can significantly hurt performance.</remarks>
    [Export(PropertyHint.Range, "1,3,or_greater,or_less")] public int MaxSearchDepth = 3;

    /// <summary>Performance option to evaluate moves using threads.</summary>
    /// <remarks>Note: The exact number of threads is based on the C# <see cref="Task"/> pool.</remarks>
    [Export] public bool EvaluateWithThreads = true;

    public AIController() : base() { _cache = new(this); }

    public override void InitializeTurn()
    {
        _selected = null;
        _destination = -Vector2I.One;
        _action = null;
        _target = null;

        EmitSignal(SignalName.ProgressUpdated, 0, Faction.GetUnits(Grid.Data).Count());
        EmitSignal(SignalName.EnabledInputActionsUpdated, new StringName[] {InputManager.Skip});
    }

    /// <inheritdoc/>
    /// <remarks>Unit actions will still be calculated and the results updated. The screen will be blacked out while computing actions.</remarks>
    public override void FastForwardTurn()
    {
        EmitSignal(SignalName.EnabledInputActionsUpdated, Array.Empty<StringName>());
        FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => EmitSignal(SignalName.FastForwardStateChanged, _ff = true), (uint)ConnectFlags.OneShot);
        FastForwardTransition.TransitionOut();
    }

    public override async void SelectUnit(UnitAction[] actions)
    {
        (_selected, _destination, _action, Vector2I target) = await Task.Run(() => ComputeAction(Faction.GetUnits(Grid.Data).Where(static (u) => u.Active), actions));
        if (Grid.Data.Occupants.TryGetValue(target, out UnitData unit))
            _target = unit;
        else
            _target = null;

        EmitSignal(SignalName.UnitSelected, _selected.Cell);
    }

    public override void MoveUnit(UnitData unit, UnitAction[] actions)
    {
        void ConfirmMove() => EmitSignal(SignalName.PathConfirmed, unit.Cell, new Godot.Collections.Array<Vector2I>(unit.Behavior.GetPath(unit, _destination)));
        if (FastForwardTransition.Active)
            FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, ConfirmMove, (uint)ConnectFlags.OneShot);
        else
            ConfirmMove();
    }

    public override void CommandUnit(UnitData source, UnitAction[] commands, UnitAction cancel) => EmitSignal(SignalName.UnitCommanded, source.Cell, _action);

    public override void SelectTarget(UnitData source, IEnumerable<Vector2I> targets)
    {
        if (_target is null)
            throw new InvalidOperationException($"{source.Renderer.Name}'s target has not been determined");
        if (!targets.Contains(_target.Cell))
            throw new InvalidOperationException($"{source.Renderer.Name} can't target {_target.Renderer}");

        Pseudocursor.Position = Grid.PositionOf(_target.Cell);
        Pseudocursor.Visible = true;

        if (_ff)
            EmitSignal(SignalName.TargetChosen, source.Cell, _target.Cell);
        else if (FastForwardTransition.Active)
            FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => EmitSignal(SignalName.TargetChosen, source.Cell, _target.Cell), (uint)ConnectFlags.OneShot);
        else
        {
            IndicatorTimer.Connect(Timer.SignalName.Timeout, () => EmitSignal(SignalName.TargetChosen, source.Cell, _target.Cell), (uint)ConnectFlags.OneShot);
            IndicatorTimer.WaitTime = IndicationTime;
            IndicatorTimer.Start();
        }
    }

    public override void FinalizeAction()
    {
        Pseudocursor.Visible = false;
        IEnumerable<UnitData> units = Faction.GetUnits(Grid.Data);
        EmitSignal(
            SignalName.ProgressUpdated,
            units.Count(static (u) => !u.Active) + 1, // Add one to account for the unit that just finished
            units.Count(static (u) => u.Active) - 1
        );
    }

    public override void FinalizeTurn()
    {
        base.FinalizeTurn();
        FastForwardTransition.TransitionIn();
        EmitSignal(SignalName.FastForwardStateChanged, _ff = false);
    }


    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (!Engine.IsEditorHint() && EnableTurnSkipping && @event.IsActionPressed(InputManager.Cancel))
            EmitSignal(SignalName.TurnFastForward);
    }
}