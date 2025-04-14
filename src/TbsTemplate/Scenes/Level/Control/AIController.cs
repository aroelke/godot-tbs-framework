using System;
using System.Collections;
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
    private readonly record struct VirtualGrid(Vector2I Size, Terrain[][] Terrain, IImmutableDictionary<Vector2I, VirtualUnit> Occupants) : IGrid
    {
        public VirtualGrid(Vector2I size, Terrain terrain, IImmutableDictionary<Vector2I, VirtualUnit> occupants) : this(
            size,
            [.. Enumerable.Repeat(Enumerable.Repeat(terrain, size.X).ToArray(), size.Y)],
            occupants
        ) {}

        public VirtualGrid(Grid grid) : this(
            grid.Size,
            [.. Enumerable.Range(1, grid.Size.X).Select((c) => Enumerable.Range(1, grid.Size.Y).Select((r) => grid.GetTerrain(new(r, c))).ToArray())],
            grid.Occupants.Where((e) => e.Value is Unit).ToImmutableDictionary((e) => e.Key, (e) => new VirtualUnit(e.Value as Unit))
        ) {}

        public int CellId(Vector2I cell) => cell.X*Size.X + cell.Y;

        public bool Contains(Vector2I cell) => cell.X >= 0 && cell.X < Size.X && cell.Y >= 0 && cell.Y < Size.Y;
        public Terrain GetTerrain(Vector2I cell) => Contains(cell) ? Terrain[cell.Y][cell.X] : null;
        public bool IsTraversable(Vector2I cell, Faction faction) => !Occupants.TryGetValue(cell, out VirtualUnit occupant) || occupant.Original.Army.Faction.AlliedTo(faction);

        public IEnumerable<Vector2I> GetNeighbors(Vector2I cell) => ((IGrid)this).GetNeighbors(cell);
        public bool IsAdjacent(Vector2I a, Vector2I b) => ((IGrid)this).IsAdjacent(a, b);
        public IEnumerable<Vector2I> TraversableCells(Faction faction, Vector2I start, int move) => ((IGrid)this).TraversableCells(faction, start, move);
        public IEnumerable<Vector2I> CellsAtDistance(Vector2I cell, int distance) => ((IGrid)this).CellsAtDistance(cell, distance);
        public int Cost(IEnumerable<Vector2I> path) => ((IGrid)this).Cost(path);
    }

    private abstract class VirtualUnitBehavior
    {
        public abstract IEnumerable<Vector2I> Destinations(VirtualGrid grid, VirtualUnit unit);

        public abstract Dictionary<StringName, IEnumerable<Vector2I>> Actions(VirtualGrid grid, VirtualUnit unit);

        public virtual Path GetPath(VirtualGrid grid, VirtualUnit unit, Vector2I from, Vector2I to)
        {
            IEnumerable<Vector2I> traversable = unit.TraversableCells(grid);
            if (!traversable.Contains(from) || !traversable.Contains(to))
                throw new ArgumentException($"Cannot compute path from {from} to {to}; at least one is not traversable.");
            return Path.Empty(grid, traversable).Add(from).Add(to);
        }

        public Path GetPath(VirtualGrid grid, VirtualUnit unit, Vector2I to) => GetPath(grid, unit, unit.Cell, to);
    }

    private class VirtualStandBehavior(bool AttackInRange=false) : VirtualUnitBehavior
    {
        public override IEnumerable<Vector2I> Destinations(VirtualGrid grid, VirtualUnit unit) => [unit.Cell];

        public override Dictionary<StringName, IEnumerable<Vector2I>> Actions(VirtualGrid grid, VirtualUnit unit)
        {
            if (AttackInRange)
            {
                Dictionary<StringName, IEnumerable<Vector2I>> actions = [];

                IEnumerable<Vector2I> attackable = unit.AttackableCells(grid, [unit.Cell]);
                IEnumerable<VirtualUnit> targets = grid.Occupants.Where((p) => attackable.Contains(p.Key) && !unit.Original.Army.Faction.AlliedTo(p.Value.Original)).Select((p) => p.Value);
                if (targets.Any())
                    actions["Attack"] = targets.Select((u) => u.Cell);

                return actions;
            }
            else
                return [];
        }
    }

    private static readonly VirtualStandBehavior VirtualStandBehaviorCantAttack = new(false);
    private static readonly VirtualStandBehavior VirtualStandBehaviorCanAttack  = new(true);

    private class VirtualMoveBehavior : VirtualUnitBehavior
    {
        public override IEnumerable<Vector2I> Destinations(VirtualGrid grid, VirtualUnit unit) => unit.TraversableCells(grid).Where((c) => !grid.Occupants.ContainsKey(c) || grid.Occupants[c] == unit);

        public override Dictionary<StringName, IEnumerable<Vector2I>> Actions(VirtualGrid grid, VirtualUnit unit)
        {
            IEnumerable<Vector2I> enemies = unit.AttackableCells(grid, unit.TraversableCells(grid)).Where((c) => grid.Occupants.ContainsKey(c) && !grid.Occupants[c].Original.Army.Faction.AlliedTo(unit.Original));
            if (enemies.Any())
                return new() { {"Attack", enemies} };
            else
                return [];
        }
    }

    private static readonly VirtualMoveBehavior VirtualMoveBehaviorInst = new();

    private readonly record struct VirtualUnit(Unit Original, Vector2I Cell, float Health, VirtualUnitBehavior Behavior)
    {
        private static ImmutableHashSet<Vector2I> GetCellsInRange(VirtualGrid grid, IEnumerable<Vector2I> sources, IEnumerable<int> ranges) => [.. sources.SelectMany((c) => ranges.SelectMany((r) => grid.CellsAtDistance(c, r)))];

        public VirtualUnit(Unit original) : this(original, original.Cell, original.Health.Value, original.Behavior switch {
            StandBehavior b => b.AttackInRange ? VirtualStandBehaviorCanAttack : VirtualStandBehaviorCantAttack,
            MoveBehavior  b => VirtualMoveBehaviorInst,
            _ => null
        }) {}

        public IEnumerable<Vector2I> TraversableCells(VirtualGrid grid) => grid.TraversableCells(Original.Army.Faction, Cell, Original.Stats.Move);

        public IEnumerable<Vector2I> AttackableCells(VirtualGrid grid, IEnumerable<Vector2I> sources) => GetCellsInRange(grid, sources, Original.AttackRange);
    }

    /// <summary>Acts as a "value" for a grid which can be compared to other values and evaluate grids against each other.</summary>
    /// <param name="Source">Faction evaluating the grid.</param>
    /// <param name="Grid">Grid to be evaluated.</param>
    private readonly record struct GridValue(Faction Source, VirtualGrid Grid) : IEquatable<GridValue>, IComparable<GridValue>
    {
        public static bool operator>(GridValue a, GridValue b) => a.CompareTo(b) > 0;
        public static bool operator<(GridValue a, GridValue b) => a.CompareTo(b) < 0;

        /// <summary>Enemy units of <see cref="Source"/>, sorted in increasing order of current health.</summary>
        private readonly IOrderedEnumerable<VirtualUnit> _enemies = Grid.Occupants.Values.Where((u) => !Source.AlliedTo(u.Original.Army.Faction)).OrderBy(static (u) => u.Health);
        private readonly IEnumerable<VirtualUnit> _allies = Grid.Occupants.Values.Where((u) => Source.AlliedTo(u.Original.Army.Faction));

        public int DeadAllies => _allies.Where(static (u) => u.Health <= 0).Count();

        /// <summary>Difference between enemy units' current and maximum health, summed over all enemy units. Higher is better.</summary>
        public float EnemyHealthDifference => _enemies.Select(static (u) => u.Original.Stats.Health - u.Health).Sum();

        /// <summary>Difference between ally units' current and maximum health, summed over all allied units. Lower is better.</summary>
        public float AllyHealthDifference => _allies.Select(static (u) => u.Original.Stats.Health - u.Health).Sum();

        public readonly int CompareTo(GridValue other)
        {
            // Less dead allies is greater
            int diff = other.DeadAllies - DeadAllies;
            if (diff != 0)
                return diff;

            // Lower least health among units with different heatlh values is greater
            foreach ((VirtualUnit me, VirtualUnit you) in _enemies.Zip(other._enemies))
                if (me.Health != you.Health)
                    return (int)((you.Health - me.Health)*10);

            // Higher enemy health difference is greater
            float hp = EnemyHealthDifference - other.EnemyHealthDifference;
            if (diff != 0)
                return (int)(hp*10);

            // Lower ally health difference is greater
            hp = other.AllyHealthDifference - AllyHealthDifference;
            if (hp != 0)
                return (int)(hp*10);

            return 0;
        }

        public bool Equals(GridValue other) => CompareTo(other) == 0;
        public override int GetHashCode() => HashCode.Combine(Source, Grid);
    }

    /// <summary>Acts as a "value" for a move of a unit on a grid which can be compared to other values to evaluate moves against each other.</summary>
    /// <param name="Grid">Grid the unit is moving on</param>
    /// <param name="Unit">Unit that's moving.</param>
    /// <param name="Destination">Potential destination of the move.</param>
    /// <param name="Grid">Grid on which the unit will move.</param>
    private readonly record struct MoveValue(VirtualGrid Grid, VirtualUnit Unit, Vector2I Destination) : IEquatable<MoveValue>, IComparable<MoveValue>
    {
        public static bool operator>(MoveValue a, MoveValue b) => a.CompareTo(b) > 0;
        public static bool operator<(MoveValue a, MoveValue b) => a.CompareTo(b) < 0;

        /// <summary>Starting cell of the move.</summary>
        public readonly Vector2I Source = Unit.Cell;

        /// <summary>Manhattan distance from <see cref="Unit"/>'s cell to <see cref="Destination"/>. Lower is better.</summary>
        public readonly int Distance = Unit.Cell.ManhattanDistanceTo(Destination);

        /// <summary>Path cost from <see cref="Unit"/>'s cell to <see cref="Destination"/>. Lower is better.</summary>
        public readonly int Cost = Path.Empty(Grid, Unit.TraversableCells(Grid)).Add(Unit.Cell).Add(Destination).Cost;

        // Note that, unlike GridValue, a negative number means this is better
        public readonly int CompareTo(MoveValue other)
        {
            int diff = Distance - other.Distance;
            if (diff != 0)
                return diff;
            
            diff = Cost - other.Cost;
            if (diff != 0)
                return diff;

            return 0;
        }

        public bool Equals(MoveValue other) => CompareTo(other) == 0;
        public override int GetHashCode() => HashCode.Combine(Distance, Cost);
    }

    private static Grid DuplicateGrid(Grid grid)
    {
        Grid copy = grid.Duplicate((int)(DuplicateFlags.Scripts | DuplicateFlags.UseInstantiation)) as Grid;
        foreach ((Vector2I cell, GridNode occupant) in grid.Occupants)
        {
            if (occupant is Unit o)
            {
                GridNode clone = o.Duplicate((int)(DuplicateFlags.Scripts | DuplicateFlags.UseInstantiation)) as GridNode;
                clone.Grid = copy;
                if (clone is Unit u)
                    u.Army = o.Army;
                copy.Occupants[cell] = clone;
            }
        }

        return copy;
    }

    private Grid _grid = null;
    private Unit _selected = null;
    private Vector2I _destination = -Vector2I.One;
    private StringName _action = null;
    private Unit _target = null;

    private (VirtualGrid, Vector2I) ChooseBestMove(VirtualUnit enemy, IList<VirtualUnit> allies, VirtualGrid grid)
    {
        IEnumerable<Vector2I> destinations = allies[0].AttackableCells(grid, [enemy.Cell]).Where((c) => allies[0].Behavior.Destinations(grid, allies[0]).Contains(c));
        if (!destinations.Any())
        {
            if (allies.Count > 1)
                return ChooseBestMove(enemy, [.. allies.Skip(1)], grid);
            else
                return (grid, allies[0].Cell);
        }

        VirtualGrid? best = null;
        GridValue bestGridValue = new();
        Vector2I move = -Vector2I.One;
        MoveValue bestMoveValue = new();
        foreach (Vector2I destination in destinations)
        {
            VirtualUnit target = enemy;

            // move allies[0] clone to destination
            MoveValue currentMoveValue = new(grid, allies[0], destination);
            VirtualUnit actor = allies[0] with { Cell = destination };
            VirtualGrid current = grid with { Occupants = grid.Occupants.Remove(allies[0].Cell).SetItem(destination, actor) };

            // attack target clone with allies[0] clone
            static float ExpectedDamage(VirtualUnit a, VirtualUnit b) => Math.Max(0f, a.Original.Stats.Accuracy - b.Original.Stats.Evasion)/100f*(a.Original.Stats.Attack - b.Original.Stats.Defense);
            target = target with { Health = target.Health - ExpectedDamage(actor, target) };
            if (target.Health > 0)
            {
                bool retaliate = target.AttackableCells(current, [target.Cell]).Contains(actor.Cell);
                if (retaliate)
                    actor = actor with { Health = actor.Health - ExpectedDamage(target, actor) };
                if (actor.Health > 0 && actor.Original.Stats.Agility > target.Original.Stats.Agility)
                    target = target with { Health = target.Health - ExpectedDamage(actor, target) };
                else if (target.Health > 0 && retaliate && target.Original.Stats.Agility > actor.Original.Stats.Agility)
                    actor = actor with { Health = actor.Health - ExpectedDamage(target, actor) };
            }
            current = current with { Occupants = current.Occupants.SetItem(actor.Cell, actor).SetItem(target.Cell, target) };

            if (allies.Count > 1)
                (current, _) = ChooseBestMove(target, [.. allies.Skip(1)], current);

            GridValue currentGridValue = new(Army.Faction, current);
            if (best is null)
            {
                best = current;
                bestGridValue = currentGridValue;
                move = destination;
                bestMoveValue = currentMoveValue;
            }
            else
            {
                if (currentGridValue > bestGridValue || (currentGridValue == bestGridValue && currentMoveValue < bestMoveValue))
                {
                    best = current;
                    bestGridValue = currentGridValue;
                    move = destination;
                    bestMoveValue = currentMoveValue;
                }
            }
        }
        return (best.Value, move);
    }

    public override Grid Grid { get => _grid; set => _grid = value; }

    public override void InitializeTurn()
    {
        _selected = null;
        _destination = -Vector2I.One;
        _action = null;
        _target = null;
    }

    private (VirtualUnit selected, Vector2I destination, StringName action, VirtualUnit? target) ComputeAction(IEnumerable<VirtualUnit> available, IEnumerable<VirtualUnit> enemies, VirtualGrid grid)
    {
        VirtualUnit? selected = null;
        Vector2I destination = -Vector2I.One;
        StringName action = null;
        VirtualUnit? target = null;

        VirtualGrid? best = null;
        GridValue bestGridValue = new();
        MoveValue bestMoveValue = new();
        foreach (VirtualUnit enemy in enemies)
        {
            IEnumerable<VirtualUnit> attackers = available.Where((u) => {
                Dictionary<StringName, IEnumerable<Vector2I>> actions = u.Behavior.Actions(grid, u);
                return actions.ContainsKey("Attack") && actions["Attack"].Contains(enemy.Cell);
            });

            foreach (IList<VirtualUnit> permutation in attackers.Permutations())
            {
                (VirtualGrid current, Vector2I move) = ChooseBestMove(enemy, permutation, grid);
                GridValue currentGridValue = new(Army.Faction, current);
                MoveValue currentMoveValue = new(grid, permutation[0], move);

                if (best is null)
                {
                    best = current;
                    selected = permutation[0];
                    destination = move;
                    action = "Attack";
                    target = enemy;

                    bestGridValue = currentGridValue;
                    bestMoveValue = currentMoveValue;
                }
                else
                {
                    if (currentGridValue > bestGridValue || (currentGridValue == bestGridValue && currentMoveValue < bestMoveValue))
                    {
                        best = current;
                        selected = permutation[0];
                        destination = move;
                        action = "Attack";
                        target = enemy;

                        bestGridValue = currentGridValue;
                        bestMoveValue = currentMoveValue;
                    }
                }
            }
        }

        // If no one has been selected yet, just pick the unit closest to an enemy
        if (selected is null)
        {
            selected = enemies.Any() ? available.MinBy((u) => enemies.Select((e) => u.Cell.DistanceTo(e.Cell)).Min()) : available.First();
            action = "End";

            IEnumerable<VirtualUnit> ordered = enemies.OrderBy((u) => u.Cell.DistanceTo(selected.Value.Cell));
            if (ordered.Any())
                destination = selected.Value.Behavior.Destinations(grid, selected.Value).OrderBy((c) => selected.Value.Behavior.GetPath(grid, selected.Value, c).Cost).OrderBy((c) => c.DistanceTo(ordered.First().Cell)).First();
            else
                destination = selected.Value.Cell;
        }

        return (selected.Value, destination, action, target);
    }

    public (Unit selected, Vector2I destination, StringName action, Unit target) ComputeAction(IEnumerable<Unit> available, IEnumerable<Unit> enemies, Grid grid)
    {
        VirtualGrid virtualGrid = new(grid);
        IEnumerable<VirtualUnit> virtualAvailable = available.Select((u) => new VirtualUnit(u));
        IEnumerable<VirtualUnit> virtualEnemies = enemies.Select((u) => new VirtualUnit(u));

        (VirtualUnit selected, Vector2I destination, StringName action, VirtualUnit? target) = ComputeAction(virtualAvailable, virtualEnemies, virtualGrid);

        return (selected.Original, destination, action, target?.Original);
    }

    public override async void SelectUnit()
    {
        // Compute this outside the task because it calls Node.GetChildren(), which has to be called on the same thread as that node.
        // Also, use a collection expression to immediately evaluated it rather than waiting until later, because that will be in the
        // wrong thread.
        VirtualGrid grid = new(Grid);
        IEnumerable<VirtualUnit> available = [.. ((IEnumerable<Unit>)Army).Where(static (u) => u.Active).Select(static (u) => new VirtualUnit(u))];
        IEnumerable<VirtualUnit> enemies = [.. Grid.Occupants.Values.OfType<Unit>().Where((u) => !Army.Faction.AlliedTo(u)).Select(static (u) => new VirtualUnit(u))];

        (VirtualUnit selected, _destination, _action, VirtualUnit? target) = await Task.Run<(VirtualUnit, Vector2I, StringName, VirtualUnit?)>(() => ComputeAction(available, enemies, grid));
        _selected = selected.Original;
        _target = target?.Original;

        EmitSignal(SignalName.UnitSelected, _selected);
    }

    public override void MoveUnit(Unit unit)
    {
        EmitSignal(SignalName.PathConfirmed, unit, new Godot.Collections.Array<Vector2I>(unit.Behavior.GetPath(unit, _destination)));
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