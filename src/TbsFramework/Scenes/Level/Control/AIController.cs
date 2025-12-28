using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsFramework.Data;
using TbsFramework.Extensions;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes.Combat.Data;
using TbsFramework.Scenes.Level.Layers;
using TbsFramework.Scenes.Level.Map;
using TbsFramework.Scenes.Level.Object;
using TbsFramework.Scenes.Level.Object.Group;
using TbsFramework.Scenes.Transitions;
using TbsFramework.UI.Controls.Device;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="UnitBehavior"/>s and the state of the level.</summary>
[Icon("res://icons/AIController.svg"), Tool]
public partial class AIController : ArmyController
{
    private const int HealthDiffPrecision = 10;

    /// <summary>Signals that the fast-forward state has changed.</summary>
    /// <param name="enable"><c>true</c> if fast-forwarding is in progress, and <c>false</c> otherwise.</param>
    [Signal] public delegate void FastForwardStateChangedEventHandler(bool enable);

    private readonly record struct VirtualSpecialActionRegion(SpecialActionRegion Original, StringName Action, ImmutableHashSet<Vector2I> Cells) : ISpecialActionRegion
    {
        ISet<Vector2I> ISpecialActionRegion.Cells => Cells;

        public VirtualSpecialActionRegion(SpecialActionRegion original) : this(original, original.Action, [.. original.GetUsedCells()]) {}

        public bool IsAllowed(IUnit unit) => unit is VirtualUnit u && Original.IsAllowed(u.Original);
    }

    private readonly record struct VirtualGrid(Vector2I Size, Terrain[][] Terrain, IImmutableDictionary<Vector2I, VirtualUnit> Occupants, IImmutableSet<VirtualSpecialActionRegion> Regions) : IGrid
    {
        public VirtualGrid(Vector2I size, Terrain terrain, IImmutableDictionary<Vector2I, VirtualUnit> occupants) : this(size, [.. Enumerable.Repeat(Enumerable.Repeat(terrain, size.X).ToArray(), size.Y)], occupants, []) {}

        public VirtualGrid(Grid grid) : this(
            grid.Size,
            [.. Enumerable.Range(0, grid.Size.Y).Select((r) => Enumerable.Range(0, grid.Size.X).Select((c) => grid.GetTerrain(new(c, r))).ToArray())],
            grid.Occupants.Where((e) => e.Value is Unit).ToImmutableDictionary((e) => e.Key, (e) => new VirtualUnit(e.Value as Unit)),
            [.. grid.SpecialActionRegions.Select((r) => new VirtualSpecialActionRegion(r))]
        ) {}

        public IEnumerable<VirtualAction> GetAvailableActions(Faction faction, int specials=0)
        {
            List<VirtualAction> actions = [];
            foreach (VirtualUnit unit in Occupants.Values.Where((u) => u.Faction == faction && u.Active && u.ExpectedHealth > 0))
                foreach (UnitAction action in unit.Original.Behavior.Actions(unit, this))
                        actions.Add(new(this, unit, action.Name, action.Source, action.Target, action.Traversable, specials));
            return actions;
        }

        public bool Contains(Vector2I cell) => IGrid.Contains(this, cell);
        public bool IsTraversable(Vector2I cell, Faction faction) => !Occupants.TryGetValue(cell, out VirtualUnit unit) || unit.Faction.AlliedTo(faction);
        public IEnumerable<Vector2I> GetCellsAtDistance(Vector2I cell, int distance) => IGrid.GetCellsAtDistance(this, cell, distance);
        public Terrain GetTerrain(Vector2I cell) => Terrain[cell.Y][cell.X];
        public int PathCost(IEnumerable<Vector2I> path) => IGrid.PathCost(this, path);
        public IImmutableDictionary<Vector2I, IUnit> GetOccupantUnits() => Occupants.ToImmutableDictionary((e) => e.Key, (e) => e.Value as IUnit);
        public IEnumerable<ISpecialActionRegion> GetSpecialActionRegions() => [.. Regions];
    }

    private readonly record struct VirtualUnit(Unit Original, Vector2I Cell, double ExpectedHealth, bool Active) : IUnit
    {
        public VirtualUnit(Unit original) : this(original, original.Cell, original.Health.Value, original.Active) {}

        public Stats Stats => Original.Stats;
        public Faction Faction => Original.Faction;

        public double Health
        {
            get => Math.Round(ExpectedHealth);
            set => throw new NotImplementedException("VirtualUnit is read-only.");
        }

        public IEnumerable<Vector2I> TraversableCells(IGrid grid) => IUnit.TraversableCells(this, grid);
        public IEnumerable<Vector2I> AttackableCells(IGrid grid, IEnumerable<Vector2I> sources) => IUnit.GetCellsInRange(grid, sources, Original.Stats.AttackRange);
        public IEnumerable<Vector2I> SupportableCells(IGrid grid, IEnumerable<Vector2I> sources) => IUnit.GetCellsInRange(grid, sources, Original.Stats.SupportRange);
    }

    private class VirtualAction : IEquatable<VirtualAction>, IComparable<VirtualAction>
    {
        public static bool operator >(VirtualAction a, VirtualAction b) => a.CompareTo(b) > 0;
        public static bool operator <(VirtualAction a, VirtualAction b) => a.CompareTo(b) < 0;

        private VirtualGrid _result;
        private readonly List<VirtualUnit> _enemies = [];
        private readonly HashSet<VirtualUnit> _allies = [];
        private Vector2I _destination = -Vector2I.One;

        public VirtualAction(VirtualGrid initial, VirtualUnit actor, StringName action, IEnumerable<Vector2I> sources, Vector2I target, IEnumerable<Vector2I> traversable, int specials)
        {
            Initial = initial;
            Actor = actor;
            Action = action;
            Sources = sources;
            Target = target;
            Traversable = traversable;
            SpecialActionsPerformed = specials;

            _destination = Actor.Cell;
            Result = Initial;
            RemainingActions = 0;
        }

        public VirtualAction(VirtualAction original, 
            VirtualGrid? initial=null,
            VirtualUnit? actor=null,
            StringName action=null,
            IEnumerable<Vector2I> sources=null,
            Vector2I? target=null,
            IEnumerable<Vector2I> traversable=null,
            Vector2I? destination=null,
            VirtualGrid? result=null,
            int? specials=null,
            int? remaining=null
        ) : this(
            initial ?? original.Initial,
            actor ?? original.Actor,
            action ?? original.Action,
            sources ?? original.Sources,
            target ?? original.Target,
            traversable ?? original.Traversable,
            specials ?? original.SpecialActionsPerformed
        )
        {
            Destination = destination ?? original.Destination;
            Result = result ?? original.Result;
            RemainingActions = remaining ?? original.RemainingActions;
        }

        public VirtualGrid Initial { get; private set; }
        public VirtualUnit Actor { get; private set; }
        public StringName Action { get; private set; }
        public IEnumerable<Vector2I> Sources { get; private set; }
        public Vector2I Target { get; private set; }
        public IEnumerable<Vector2I> Traversable { get; private set; }

        public Vector2I Destination
        {
            get => _destination;
            set => PathCost = Path.Empty(Initial, Actor.TraversableCells(Initial)).Add(Actor.Cell).Add(_destination = value).Cost;
        }

        public VirtualGrid Result
        {
            get => _result;
            set
            {
                _result = value;

                DefeatedAllies = DefeatedEnemies = 0;
                foreach ((_, VirtualUnit unit) in _result.Occupants)
                {
                    if (Actor.Faction.AlliedTo(unit.Faction))
                    {
                        _allies.Add(unit);
                        if (unit.ExpectedHealth <= 0)
                            DefeatedAllies++;
                        AllyHealthDifference += unit.Stats.Health - unit.ExpectedHealth;
                    }
                    else
                    {
                        _enemies.Add(unit);
                        if (unit.ExpectedHealth <= 0)
                            DefeatedEnemies++;
                        EnemyHealthDifference += unit.Stats.Health - unit.ExpectedHealth;
                    }
                }

                _enemies.Sort((a, b) => (int)((a.ExpectedHealth - b.ExpectedHealth)*HealthDiffPrecision));
            }
        }

        public int SpecialActionsPerformed = 0;
        public int DefeatedEnemies { get; private set; } = 0;
        public int DefeatedAllies { get; private set; } = 0;
        public double AllyHealthDifference { get; private set; } = 0;
        public double EnemyHealthDifference { get; private set; } = 0;
        public int PathCost { get; private set; } = 0;
        public int RemainingActions { get; private set; } = 0;

        public bool Equals(VirtualAction other) => other is not null && Initial == other.Initial && Actor == other.Actor && Action == other.Action && Target == other.Target && Destination == other.Destination;
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

            if ((diff = (int)((other.AllyHealthDifference - AllyHealthDifference)*HealthDiffPrecision)) != 0)
                return diff;

            int smaller = Math.Min(_enemies.Count, other._enemies.Count);
            for (int i = 0; i < smaller; i++)
                if (_enemies[i].ExpectedHealth != other._enemies[i].ExpectedHealth)
                    return (int)((other._enemies[i].ExpectedHealth - _enemies[i].ExpectedHealth)*HealthDiffPrecision);

            if ((diff = (int)((EnemyHealthDifference - other.EnemyHealthDifference)*HealthDiffPrecision)) != 0)
                return diff;

            if ((diff = RemainingActions - other.RemainingActions) != 0)
                return diff;

            return other.PathCost - PathCost;
        }

        public override string ToString() => $"Move {Actor.Faction.Name}@{Actor.Cell} to {Destination} and {Action} {Target}";
        public override int GetHashCode() => HashCode.Combine(Initial, Actor, Action, Target, Destination);
    }

    private static VirtualAction EvaluateAction(IEnumerable<VirtualAction> actions, VirtualAction action, Dictionary<VirtualGrid, VirtualAction> decisions, int remaining)
    {
        VirtualUnit? target = null;
        HashSet<Vector2I> destinations = [.. action.Sources];
        if (action.Action == UnitAction.AttackAction)
        {
            target = action.Initial.Occupants[action.Target];
            IEnumerable<Vector2I> safeCells = destinations.Where((c) => !target.Value.Original.Stats.AttackRange.Contains(c.ManhattanDistanceTo(target.Value.Cell)));
            if (safeCells.Any())
                destinations = [.. safeCells];
        }
        else if (action.Action == UnitAction.SupportAction)
            target = action.Initial.Occupants[action.Target];
        else if (action.Action == UnitAction.EndAction)
            throw new InvalidOperationException($"End actions cannot be evaluated");

        List<Vector2I> choices = [];
        if (destinations.Contains(action.Actor.Cell))
            choices.Add(action.Actor.Cell);
        if (!destinations.Contains(action.Actor.Cell) || destinations.Count > 1)
            choices.Add(destinations.Where((c) => c != action.Actor.Cell).MinBy((c) => c.ManhattanDistanceTo(action.Actor.Cell)));

        return choices.Max((c) => {
            VirtualUnit actor;
            VirtualUnit? updated = null;
            VirtualGrid after;
            bool special = false;
            if (action.Action == UnitAction.AttackAction)
            {
                actor = action.Actor with { Cell = c };
                updated = target;
                after = action.Initial with { Occupants = action.Initial.Occupants.Remove(action.Actor.Cell).Add(actor.Cell, actor) };
                List<CombatAction> actions = CombatCalculations.AttackResults(actor, updated.Value, after, true);
                foreach (CombatAction combat in actions)
                {
                    if (combat.Target.Cell == actor.Cell)
                        actor = actor with { ExpectedHealth =  actor.ExpectedHealth - combat.Damage };
                    else if (combat.Target.Cell == updated.Value.Cell)
                        updated = updated.Value with { ExpectedHealth = updated.Value.ExpectedHealth - combat.Damage };
                    else
                        GD.PushWarning($"Combat participant at cell {combat.Target.Cell} is not this action's target");
                }
                after = after with { Occupants = after.Occupants.SetItem(actor.Cell, actor).SetItem(updated.Value.Cell, updated.Value) };
            }
            else if (action.Action == UnitAction.SupportAction)
            {
                double targetHealth = target.Value.ExpectedHealth, actorHealth = action.Actor.ExpectedHealth;
                targetHealth = Math.Min(targetHealth + action.Actor.Stats.Healing, target.Value.Stats.Health);
                actor = action.Actor with { Cell = c, ExpectedHealth = actorHealth, Active = false };
                updated = target.Value with { ExpectedHealth = targetHealth };
                after = action.Initial with { Occupants = action.Initial.Occupants.Remove(action.Actor.Cell).Add(c, actor).Remove(target.Value.Cell).Add(updated.Value.Cell, updated.Value) };
            }
            else
            {
                actor = action.Actor with { Cell = c, Active = false };
                after = action.Initial with { Occupants = action.Initial.Occupants.Remove(action.Actor.Cell).Add(c, actor) };
                special = true;
            }

            // If this action results in a board state that was already explored, skip the rest of this branch and use that result
            if (decisions.TryGetValue(after, out VirtualAction decision))
                return decision;

            IEnumerable<VirtualAction> further = after.GetAvailableActions(actor.Faction, special ? action.SpecialActionsPerformed + 1 : action.SpecialActionsPerformed);
            if (remaining == 0 || remaining > 1)
            {
                remaining = Math.Max(0, remaining - 1);
                IEnumerable<VirtualAction> reduced = further.Where((a) => {
                    // Evaluate a if a was not present in the previous set of actions that were evaluated (the other ones will either reappear or be evaluated later)
                    if (!actions.Any((b) => a.Actor == b.Actor && a.Target == b.Target))
                        return true;
                    // Evaluate a if a or this action was not an attack action
                    if (action.Action != UnitAction.AttackAction || a.Action != UnitAction.AttackAction)
                        return true;
                    // Don't evaluate a if a's target is defeated
                    if (a.Initial.Occupants[a.Target].ExpectedHealth <= 0)
                        return false;
                    // Evaluate a if this action defeated its target to see if more enemies can be defeated down this branch
                    if (updated.Value.ExpectedHealth <= 0)
                        return true;
                    // Evaluate a if it has the same target as this action
                    if (a.Target == action.Target)
                        return true;
                    return false;
                });
                if (reduced.Any())
                    decisions[after] = new(action, destination:c, result:reduced.Max((a) => EvaluateAction(reduced, a, decisions, remaining)).Result);
            }
            if (!decisions.ContainsKey(after))
                decisions[after] = new(action, destination:c, result:after, specials:special ? action.SpecialActionsPerformed + 1 : action.SpecialActionsPerformed, remaining:further.Count());
            return decisions[after];
        });
    }

    private (VirtualUnit selected, Vector2I destination, StringName action, Vector2I target) ComputeAction(IEnumerable<VirtualUnit> available, VirtualGrid grid)
    {
        VirtualUnit? selected = null;
        Vector2I destination = -Vector2I.One;
        StringName action = null;
        Vector2I target;

        List<VirtualAction> actions = [.. grid.GetAvailableActions(Army.Faction)];
        if (actions.Count != 0)
        {
            VirtualAction result;
            if (EvaluateWithThreads)
                result = Task.WhenAll([.. actions.Select((a) => Task.Run(() => EvaluateAction(actions, a, [], MaxSearchDepth)))]).Result.Max();
            else
            {
                Dictionary<VirtualGrid, VirtualAction> decisions = [];
                result = actions.Select((a) => EvaluateAction(actions, a, decisions, MaxSearchDepth)).Max();
            }
            selected = result.Actor;
            destination = result.Destination;
            action = result.Action;
            target = result.Target;
        }
        else
        {
            IEnumerable<VirtualUnit> enemies = grid.GetOccupantUnits().Values.Where((u) => !u.Faction.AlliedTo(Army.Faction)).OfType<VirtualUnit>();

            selected = enemies.Any() ? available.MinBy((u) => enemies.Select((e) => u.Cell.DistanceTo(e.Cell)).Min()) : available.First();
            action = UnitAction.EndAction;

            IEnumerable<VirtualUnit> ordered = enemies.OrderBy((u) => u.Cell.DistanceTo(selected.Value.Cell));
            if (ordered.Any())
            {
                destination = selected.Value.Original.Behavior.Destinations(selected.Value, grid)
                    .OrderBy((c) => selected.Value.Original.Behavior.GetPath(selected.Value, grid, c).Cost)
                    .OrderBy((c) => c.DistanceTo(ordered.First().Cell)).First();
            }
            else
                destination = selected.Value.Cell;
            target = -Vector2I.One;
        }

        return (selected.Value, destination, action, target);
    }

    private readonly NodeCache _cache = null;
    private Grid _grid = null;
    private Unit _selected = null;
    private Vector2I _destination = -Vector2I.One;
    private StringName _action = null;
    private Unit _target = null;
    private bool _ff = false;

    private Sprite2D              Pseudocursor          => _cache.GetNode<Sprite2D>("Pseudocursor");
    private FadeToBlackTransition FastForwardTransition => _cache.GetNode<FadeToBlackTransition>("CanvasLayer/FastForwardTransition");
    private Timer                 IndicatorTimer        => _cache.GetNode<Timer>("IndicatorTimer");

    public override Grid Grid { get => _grid; set => _grid = value; }

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

        EmitSignal(SignalName.ProgressUpdated, 0, ((IEnumerable<Unit>)Army).Count());
        EmitSignal(SignalName.EnabledInputActionsUpdated, new StringName[] {InputManager.Skip});
    }

    public (Unit selected, Vector2I destination, StringName action, Unit target) ComputeAction(IEnumerable<Unit> available, IEnumerable<Unit> enemies, Grid grid)
    {
        VirtualGrid virtualGrid = new(grid);
        IEnumerable<VirtualUnit> virtualAvailable = available.Select((u) => new VirtualUnit(u));

        (VirtualUnit selected, Vector2I destination, StringName action, Vector2I target) = ComputeAction(virtualAvailable, virtualGrid);

        return (selected.Original, destination, action, virtualGrid.Occupants.TryGetValue(target, out VirtualUnit occupant) ? occupant.Original : null);
    }

    /// <inheritdoc/>
    /// <remarks>Unit actions will still be calculated and the results updated. The screen will be blacked out while computing actions.</remarks>
    public override void FastForwardTurn()
    {
        EmitSignal(SignalName.EnabledInputActionsUpdated, Array.Empty<StringName>());
        FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => EmitSignal(SignalName.FastForwardStateChanged, _ff = true), (uint)ConnectFlags.OneShot);
        FastForwardTransition.TransitionOut();
    }

    public override async void SelectUnit()
    {
        // Compute this outside the task because it calls Node.GetChildren(), which has to be called on the same thread as that node.
        // Also, use a collection expression to immediately evaluated it rather than waiting until later, because that will be in the
        // wrong thread.
        VirtualGrid grid = new(Grid);
        IEnumerable<VirtualUnit> available = [.. ((IEnumerable<Unit>)Army).Where(static (u) => u.Active).Select(static (u) => new VirtualUnit(u))];

        (VirtualUnit selected, _destination, _action, Vector2I target) = await Task.Run<(VirtualUnit, Vector2I, StringName, Vector2I)>(() => ComputeAction(available, grid));
        _selected = selected.Original;
        if (grid.Occupants.TryGetValue(target, out VirtualUnit occupant))
            _target = occupant.Original;
        else
            _target = null;

        EmitSignal(SignalName.UnitSelected, _selected);
    }

    public override void MoveUnit(Unit unit)
    {
        void ConfirmMove() => EmitSignal(SignalName.PathConfirmed, unit, new Godot.Collections.Array<Vector2I>(unit.Behavior.GetPath(unit, unit.Grid, _destination)));
        if (FastForwardTransition.Active)
            FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, ConfirmMove, (uint)ConnectFlags.OneShot);
        else
            ConfirmMove();
    }

    public override void CommandUnit(Unit source, Godot.Collections.Array<StringName> commands, StringName cancel)
    {
        EmitSignal(SignalName.UnitCommanded, source, _action);
    }

    public override void SelectTarget(Unit source, IEnumerable<Vector2I> targets)
    {
        if (_target is null)
            throw new InvalidOperationException($"{source.Name}'s target has not been determined");
        if (!targets.Contains(_target.Cell))
            throw new InvalidOperationException($"{source.Name} can't target {_target}");

        Pseudocursor.Position = Grid.PositionOf(_target.Cell);
        Pseudocursor.Visible = true;

        if (_ff)
            EmitSignal(SignalName.TargetChosen, source, _target);
        else if (FastForwardTransition.Active)
            FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => EmitSignal(SignalName.TargetChosen, source, _target), (uint)ConnectFlags.OneShot);
        else
        {
            IndicatorTimer.Connect(Timer.SignalName.Timeout, () => EmitSignal(SignalName.TargetChosen, source, _target), (uint)ConnectFlags.OneShot);
            IndicatorTimer.WaitTime = IndicationTime;
            IndicatorTimer.Start();
        }
    }

    public override void FinalizeAction()
    {
        Pseudocursor.Visible = false;
        EmitSignal(SignalName.ProgressUpdated, ((IEnumerable<Unit>)Army).Count((u) => !u.Active) + 1, ((IEnumerable<Unit>)Army).Count((u) => u.Active) - 1); // Add one to account for the unit that just finished
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