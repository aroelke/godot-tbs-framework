using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsTemplate.Data;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Level.Control.Behavior;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="UnitBehavior"/>s and the state of the level.</summary>
public partial class AIController : ArmyController
{
    private const int HealthDiffPrecision = 10;

    private readonly record struct VirtualGrid(Vector2I Size, Terrain[][] Terrain, IImmutableDictionary<Vector2I, VirtualUnit> Occupants) : IGrid
    {
        public VirtualGrid(Vector2I size, Terrain terrain, IImmutableDictionary<Vector2I, VirtualUnit> occupants) : this(size, [.. Enumerable.Repeat(Enumerable.Repeat(terrain, size.X).ToArray(), size.Y)], occupants) { }

        public VirtualGrid(Grid grid) : this(
            grid.Size,
            [.. Enumerable.Range(0, grid.Size.Y).Select((r) => Enumerable.Range(0, grid.Size.X).Select((c) => grid.GetTerrain(new(c, r))).ToArray())],
            grid.Occupants.Where((e) => e.Value is Unit).ToImmutableDictionary((e) => e.Key, (e) => new VirtualUnit(e.Value as Unit))
        )
        {}

        public IEnumerable<VirtualAction> GetAvailableActions(Faction faction)
        {
            List<VirtualAction> actions = [];
            foreach (VirtualUnit unit in Occupants.Values.Where((u) => u.Faction == faction && u.Active && u.ExpectedHealth > 0))
                foreach ((StringName action, IEnumerable<Vector2I> targets) in unit.Original.Behavior.Actions(unit, this))
                    foreach (Vector2I target in targets)
                        actions.Add(new(this, unit, action, target));
            return actions;
        }

        public bool Contains(Vector2I cell) => IGrid.Contains(this, cell);
        public bool IsTraversable(Vector2I cell, Faction faction) => !Occupants.TryGetValue(cell, out VirtualUnit unit) || unit.Faction.AlliedTo(faction);
        public IEnumerable<Vector2I> GetCellsAtDistance(Vector2I cell, int distance) => IGrid.GetCellsAtDistance(this, cell, distance);
        public Terrain GetTerrain(Vector2I cell) => Terrain[cell.Y][cell.X];
        public int PathCost(IEnumerable<Vector2I> path) => IGrid.PathCost(this, path);
        public IImmutableDictionary<Vector2I, IUnit> GetOccupantUnits() => Occupants.ToImmutableDictionary((e) => e.Key, (e) => e.Value as IUnit);
    }

    private readonly record struct VirtualUnit(Unit Original, Vector2I Cell, float ExpectedHealth, bool Active) : IUnit
    {
        public VirtualUnit(Unit original) : this(original, original.Cell, original.Health.Value, original.Active) {}

        public Stats Stats => Original.Stats;
        public Faction Faction => Original.Faction;
        public int Health => (int)Math.Round(ExpectedHealth);

        public IEnumerable<Vector2I> TraversableCells(IGrid grid) => IUnit.TraversableCells(this, grid);
        public IEnumerable<Vector2I> AttackableCells(IGrid grid, IEnumerable<Vector2I> sources) => IUnit.GetCellsInRange(grid, sources, Original.AttackRange);
        public IEnumerable<Vector2I> SupportableCells(IGrid grid, IEnumerable<Vector2I> sources) => IUnit.GetCellsInRange(grid, sources, Original.SupportRange);
    }

    private class VirtualAction : IEquatable<VirtualAction>, IComparable<VirtualAction>
    {
        public static bool operator >(VirtualAction a, VirtualAction b) => a.CompareTo(b) > 0;
        public static bool operator <(VirtualAction a, VirtualAction b) => a.CompareTo(b) < 0;

        private VirtualGrid _result;
        private IOrderedEnumerable<VirtualUnit> _enemies;
        private IEnumerable<VirtualUnit> _allies;
        private Vector2I _destination = -Vector2I.One;

        public VirtualAction(VirtualGrid initial, VirtualUnit actor, StringName action, Vector2I target)
        {
            Initial = initial;
            Actor = actor;
            Action = action;
            Target = target;

            _destination = Actor.Cell;
            _result = Initial;
        }

        public VirtualAction(VirtualAction original, VirtualGrid? initial=null, VirtualUnit? actor=null, StringName action=null, Vector2I? target=null, Vector2I? destination=null, VirtualGrid? result=null) : this(
            initial ?? original.Initial,
            actor ?? original.Actor,
            action ?? original.Action,
            target ?? original.Target
        )
        {
            Destination = destination ?? original.Destination;
            Result = result ?? original.Result;
        }

        public VirtualGrid Initial { get; private set; }
        public VirtualUnit Actor { get; private set; }
        public StringName Action { get; private set; }
        public Vector2I Target { get; private set; }

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

                _enemies = _result.Occupants.Values.Where((u) => !Actor.Faction.AlliedTo(u.Faction)).OrderBy(static (u) => u.ExpectedHealth);
                _allies = _result.Occupants.Values.Where((u) => Actor.Faction.AlliedTo(u.Faction));

                DefeatedEnemies = _enemies.Count(static (u) => u.ExpectedHealth <= 0);
                DefeatedAllies = _allies.Count(static (u) => u.ExpectedHealth <= 0);
                AllyHealthDifference = _allies.Sum(static (u) => u.Stats.Health - u.ExpectedHealth);
                EnemyHealthDifference = _enemies.Sum(static (u) => u.Stats.Health - u.ExpectedHealth);
            }
        }

        public int DefeatedEnemies { get; private set; } = 0;
        public int DefeatedAllies { get; private set; } = 0;
        public float AllyHealthDifference { get; private set; } = 0;
        public float EnemyHealthDifference { get; private set; } = 0;
        public int PathCost { get; private set; } = 0;

        public bool Equals(VirtualAction other) => other is not null && Initial == other.Initial && Actor == other.Actor && Action == other.Action && Target == other.Target && Destination == other.Destination;
        public override bool Equals(object obj) => Equals(obj as VirtualAction);

        // Positive is better
        public int CompareTo(VirtualAction other)
        {
            int diff;

            if ((diff = DefeatedEnemies - other.DefeatedEnemies) != 0)
                return diff;
            if ((diff = other.DefeatedAllies - DefeatedAllies) != 0)
                return diff;

            if ((diff = (int)((other.AllyHealthDifference - AllyHealthDifference)*HealthDiffPrecision)) != 0)
                return diff;
            foreach ((VirtualUnit me, VirtualUnit you) in _enemies.Zip(other._enemies))
                if (me.ExpectedHealth != you.ExpectedHealth)
                    return (int)((you.ExpectedHealth - me.ExpectedHealth)*HealthDiffPrecision);
            if ((diff = (int)((EnemyHealthDifference - other.EnemyHealthDifference) * HealthDiffPrecision)) != 0)
                return diff;

            return other.PathCost - PathCost;
        }

        public override string ToString() => $"Move {Actor.Faction.Name}@{Actor.Cell} to {Destination} and {Action} {Target}";
        public override int GetHashCode() => HashCode.Combine(Initial, Actor, Action, Target, Destination);
    }

    private static VirtualAction EvaluateAction(VirtualAction action, Dictionary<VirtualGrid, VirtualAction> decisions, int left)
    {
        if (decisions.TryGetValue(action.Initial, out VirtualAction decision))
            return decision;

        VirtualUnit target = action.Initial.Occupants[action.Target];
        HashSet<Vector2I> destinations = [.. action.Actor.Original.Behavior.Destinations(action.Actor, action.Initial)];
        if (action.Action == "Attack")
            destinations = [.. destinations.Intersect(action.Actor.AttackableCells(action.Initial, [action.Target]))];
        else if (action.Action == "Support")
            destinations = [.. destinations.Intersect(action.Actor.SupportableCells(action.Initial, [action.Target]))];
        else
            throw new InvalidOperationException($"Unsupported action {action.Action}");

        IEnumerable<Vector2I> retaliatable = destinations.Intersect(target.AttackableCells(action.Initial, [target.Cell]));
        if (!destinations.All(retaliatable.Contains))
            foreach (Vector2I cell in retaliatable)
                destinations.Remove(cell);
        List<Vector2I> choices = [];
        if (destinations.Contains(action.Actor.Cell))
            choices.Add(action.Actor.Cell);
        if (!destinations.Contains(action.Actor.Cell) || destinations.Count > 1)
            choices.Add(destinations.Where((c) => c != action.Actor.Cell).MinBy((c) => action.Actor.Cell.ManhattanDistanceTo(c)));

        return decisions[action.Initial] = choices.Select((c) => {
            float targetHealth = target.ExpectedHealth, actorHealth = action.Actor.ExpectedHealth;
            if (action.Action == "Attack")
            {
                static float ExpectedDamage(VirtualUnit a, VirtualUnit b) => Math.Max(0f, a.Original.Stats.Accuracy - b.Original.Stats.Evasion)/100f*(a.Original.Stats.Attack - b.Original.Stats.Defense);
                targetHealth -= ExpectedDamage(action.Actor, target);
                if (targetHealth > 0)
                {
                    if (retaliatable.Contains(c))
                        actorHealth -= ExpectedDamage(target, action.Actor);
                    if (actorHealth > 0 && action.Actor.Original.Stats.Agility > target.Original.Stats.Agility)
                        targetHealth -= ExpectedDamage(action.Actor, target);
                    else if (targetHealth > 0 && retaliatable.Contains(c) && target.Original.Stats.Agility > action.Actor.Original.Stats.Agility)
                        actorHealth -= ExpectedDamage(target, action.Actor);
                }
            }
            else if (action.Action == "Support")
                targetHealth = Math.Min(targetHealth + action.Actor.Stats.Healing, target.Stats.Health);
            VirtualUnit actor = action.Actor with { Cell = c, ExpectedHealth = actorHealth, Active = false };
            VirtualUnit updated = target with { ExpectedHealth = targetHealth };
            VirtualGrid after = action.Initial with { Occupants = action.Initial.Occupants.Remove(action.Actor.Cell).Add(c, actor).Remove(target.Cell).Add(updated.Cell, updated) };
            VirtualAction result = new(action, destination:c, result:after);

            if (left == 0 || left > 1)
            {
                left = Math.Max(0, left - 1);
                IEnumerable<VirtualAction> further = after.GetAvailableActions(actor.Faction);
                if (further.Any())
                    result = new(result, result:further.Select((a) => EvaluateAction(a, decisions, left)).Max().Result);
            }
            return result;
        }).Max();
    }

    private (VirtualUnit selected, Vector2I destination, StringName action, VirtualUnit? target) ComputeAction(IEnumerable<VirtualUnit> available, VirtualGrid grid)
    {
        VirtualUnit? selected = null;
        Vector2I destination = -Vector2I.One;
        StringName action = null;
        VirtualUnit? target = null;

        IEnumerable<VirtualAction> actions = grid.GetAvailableActions(Army.Faction);
        if (actions.Any())
        {
            VirtualAction result = Task.WhenAll([.. actions.Select((a) => Task.Run(() => EvaluateAction(a, MaxSearchDepth)))]).Result.Max();
            selected = result.Actor;
            destination = result.Destination;
            action = result.Action;
            target = grid.Occupants[result.Target];
        }
        else
        {
            IEnumerable<VirtualUnit> enemies = grid.GetOccupantUnits().Values.Where((u) => !u.Faction.AlliedTo(Army.Faction)).OfType<VirtualUnit>();

            selected = enemies.Any() ? available.MinBy((u) => enemies.Select((e) => u.Cell.DistanceTo(e.Cell)).Min()) : available.First();
            action = "End";

            IEnumerable<VirtualUnit> ordered = enemies.OrderBy((u) => u.Cell.DistanceTo(selected.Value.Cell));
            if (ordered.Any())
                destination = selected.Value.Original.Behavior.Destinations(selected.Value, grid).OrderBy((c) => selected.Value.Original.Behavior.GetPath(selected.Value, grid, c).Cost).OrderBy((c) => c.DistanceTo(ordered.First().Cell)).First();
            else
                destination = selected.Value.Cell;
        }

        return (selected.Value, destination, action, target);
    }

    private static VirtualAction EvaluateAction(VirtualAction action, int depth) => EvaluateAction(action, [], depth);

    private Grid _grid = null;
    private Unit _selected = null;
    private Vector2I _destination = -Vector2I.One;
    private StringName _action = null;
    private Unit _target = null;

    public override Grid Grid { get => _grid; set => _grid = value; }

    [Export] public int MaxSearchDepth = 0;

    public override void InitializeTurn()
    {
        _selected = null;
        _destination = -Vector2I.One;
        _action = null;
        _target = null;
    }

    public (Unit selected, Vector2I destination, StringName action, Unit target) ComputeAction(IEnumerable<Unit> available, IEnumerable<Unit> enemies, Grid grid)
    {
        VirtualGrid virtualGrid = new(grid);
        IEnumerable<VirtualUnit> virtualAvailable = available.Select((u) => new VirtualUnit(u));

        (VirtualUnit selected, Vector2I destination, StringName action, VirtualUnit? target) = ComputeAction(virtualAvailable, virtualGrid);

        return (selected.Original, destination, action, target?.Original);
    }

    public override async void SelectUnit()
    {
        // Compute this outside the task because it calls Node.GetChildren(), which has to be called on the same thread as that node.
        // Also, use a collection expression to immediately evaluated it rather than waiting until later, because that will be in the
        // wrong thread.
        VirtualGrid grid = new(Grid);
        IEnumerable<VirtualUnit> available = [.. ((IEnumerable<Unit>)Army).Where(static (u) => u.Active).Select(static (u) => new VirtualUnit(u))];

        (VirtualUnit selected, _destination, _action, VirtualUnit? target) = await Task.Run<(VirtualUnit, Vector2I, StringName, VirtualUnit?)>(() => ComputeAction(available, grid));
        _selected = selected.Original;
        _target = target?.Original;

        EmitSignal(SignalName.UnitSelected, _selected);
    }

    public override void MoveUnit(Unit unit)
    {
        EmitSignal(SignalName.PathConfirmed, unit, new Godot.Collections.Array<Vector2I>(unit.Behavior.GetPath(unit, unit.Grid, _destination)));
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
        EmitSignal(SignalName.TargetChosen, source, _target);
    }

    public override void FinalizeAction() {}

    // Don't resume the cursor.  The player controller will be responsible for that.
    public override void FinalizeTurn() {}
}