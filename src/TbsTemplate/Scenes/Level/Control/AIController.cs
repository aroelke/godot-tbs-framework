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
    private readonly record struct VirtualGrid(Vector2I Size, Terrain[][] Terrain, IImmutableDictionary<Vector2I, VirtualUnit> Occupants) : IGrid
    {
        public VirtualGrid(Vector2I size, Terrain terrain, IImmutableDictionary<Vector2I, VirtualUnit> occupants) : this(size, [.. Enumerable.Repeat(Enumerable.Repeat(terrain, size.X).ToArray(), size.Y)], occupants) {}

        public VirtualGrid(Grid grid) : this(
            grid.Size,
            [.. Enumerable.Range(1, grid.Size.Y).Select((r) => Enumerable.Range(1, grid.Size.X).Select((c) => grid.GetTerrain(new(c, r))).ToArray())],
            grid.Occupants.Where((e) => e.Value is Unit).ToImmutableDictionary((e) => e.Key, (e) => new VirtualUnit(e.Value as Unit))
        ) {}

        public bool Contains(Vector2I cell) => IGrid.Contains(this, cell);
        public bool IsTraversable(Vector2I cell, Faction faction) => !Occupants.TryGetValue(cell, out VirtualUnit unit) || unit.Faction.AlliedTo(faction);
        public IEnumerable<Vector2I> GetCellsAtDistance(Vector2I cell, int distance) => IGrid.GetCellsAtDistance(this, cell, distance);
        public Terrain GetTerrain(Vector2I cell) => Terrain[cell.Y][cell.X];
        public int PathCost(IEnumerable<Vector2I> path) => IGrid.PathCost(this, path);
        public IImmutableDictionary<Vector2I, IUnit> GetOccupantUnits() => Occupants.ToImmutableDictionary((e) => e.Key, (e) => e.Value as IUnit);
    }

    private readonly record struct VirtualUnit(Unit Original, Vector2I Cell, float ExpectedHealth) : IUnit
    {
        public VirtualUnit(Unit original) : this(original, original.Cell, original.Health.Value) {}

        public Stats Stats => Original.Stats;
        public Faction Faction => Original.Faction;
        public int Health => (int)Math.Round(ExpectedHealth);

        public IEnumerable<Vector2I> TraversableCells(IGrid grid) => IUnit.TraversableCells(this, grid);
        public IEnumerable<Vector2I> AttackableCells(IGrid grid, IEnumerable<Vector2I> sources) => IUnit.GetCellsInRange(grid, sources, Original.AttackRange);
        public IEnumerable<Vector2I> SupportableCells(IGrid grid, IEnumerable<Vector2I> sources) => IUnit.GetCellsInRange(grid, sources, Original.SupportRange);
    }

    /// <summary>Acts as a "value" for a grid which can be compared to other values and evaluate grids against each other.</summary>
    /// <param name="Source">Faction evaluating the grid.</param>
    /// <param name="Grid">Grid to be evaluated.</param>
    private readonly record struct GridValue(Faction Source, VirtualGrid Grid) : IEquatable<GridValue>, IComparable<GridValue>
    {
        public static bool operator>(GridValue a, GridValue b) => a.CompareTo(b) > 0;
        public static bool operator<(GridValue a, GridValue b) => a.CompareTo(b) < 0;

        /// <summary>Enemy units of <see cref="Source"/>, sorted in increasing order of current health.</summary>
        private readonly IOrderedEnumerable<VirtualUnit> _enemies = Grid.Occupants.Values.Where((u) => !Source.AlliedTo(u.Original.Army.Faction)).OrderBy(static (u) => u.ExpectedHealth);
        private readonly IEnumerable<VirtualUnit> _allies = Grid.Occupants.Values.Where((u) => Source.AlliedTo(u.Original.Army.Faction));

        public int DeadEnemies => _enemies.Where(static (u) => u.ExpectedHealth <= 0).Count();

        public int DeadAllies => _allies.Where(static (u) => u.ExpectedHealth <= 0).Count();

        /// <summary>Difference between enemy units' current and maximum health, summed over all enemy units. Higher is better.</summary>
        public float EnemyHealthDifference => _enemies.Select(static (u) => u.Original.Stats.Health - u.ExpectedHealth).Sum();

        /// <summary>Difference between ally units' current and maximum health, summed over all allied units. Lower is better.</summary>
        public float AllyHealthDifference => _allies.Select(static (u) => u.Original.Stats.Health - u.ExpectedHealth).Sum();

        public readonly int CompareTo(GridValue other)
        {
            // More dead enemies is greater
            int diff = DeadEnemies - other.DeadEnemies;
            if (diff != 0)
                return diff;

            // Less dead allies is greater
            diff = other.DeadAllies - DeadAllies;
            if (diff != 0)
                return diff;

            // Lower ally health difference is greater
            float hp = other.AllyHealthDifference - AllyHealthDifference;
            if (hp != 0)
                return (int)(hp*10);

            // Lower least health among units with different heatlh values is greater
            foreach ((VirtualUnit me, VirtualUnit you) in _enemies.Zip(other._enemies))
                if (me.ExpectedHealth != you.ExpectedHealth)
                    return (int)((you.ExpectedHealth - me.ExpectedHealth)*10);

            // Higher enemy health difference is greater
            hp = EnemyHealthDifference - other.EnemyHealthDifference;
            if (diff != 0)
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

    private (VirtualGrid, Vector2I, StringName, VirtualUnit?) ChooseBestMove(IList<VirtualUnit> remaining, VirtualGrid grid, IEnumerable<Vector2I> restriction=null)
    {
        restriction ??= [];

        if (remaining[0].Health <= 0)
            return (grid, remaining[0].Cell, "End", null);

        IEnumerable<Vector2I> destinations = remaining[0].Original.Behavior.Destinations(remaining[0], grid);
        if (!destinations.Any())
            return (grid, remaining[0].Cell, "End", null);

        Dictionary<StringName, IEnumerable<Vector2I>> actions = remaining[0].Original.Behavior.Actions(remaining[0], grid);
        if (!actions.Any((e) => !restriction.Any() || restriction.Any((c) => e.Value.Contains(c))))
            return (grid, remaining[0].Cell, "End", null);

        Dictionary<Vector2I, VirtualUnit> occupants = grid.GetOccupantUnits().ToDictionary((e) => e.Key, (e) => (VirtualUnit)e.Value);

        VirtualGrid? bestGrid = null;
        GridValue bestGridValue = new();
        Vector2I bestMove = -Vector2I.One;
        MoveValue bestMoveValue = new();
        StringName bestAction = null;
        VirtualUnit? bestTarget = null;
        foreach ((StringName action, IEnumerable<Vector2I> targetCells) in actions)
        {
            foreach (VirtualUnit target in targetCells.Where((c) => !restriction.Any() || restriction.Contains(c)).Select((c) => occupants[c]))
            {
                IEnumerable<Vector2I> sources;
                if (action == "Attack")
                    sources = remaining[0].AttackableCells(grid, [target.Cell]).Where(destinations.Contains);
                else if (action == "Support")
                    sources = remaining[0].SupportableCells(grid, [target.Cell]).Where(destinations.Contains);
                else
                    sources = [];
                if (!sources.Any())
                    throw new InvalidOperationException($"No sources to perform {action} from");

                foreach (Vector2I destination in sources)
                {
                    VirtualUnit actor = remaining[0] with { Cell = destination };
                    VirtualUnit temp  = target;
                    VirtualGrid currentGrid = grid with { Occupants = grid.Occupants.Remove(remaining[0].Cell).SetItem(destination, actor) };
                    MoveValue currentMoveValue = new(grid, remaining[0], destination);

                    if (actor.Faction.AlliedTo(target.Faction))
                    {
                        // heal target clone with remaining[0] clone
                        temp = temp with { ExpectedHealth = Math.Min(temp.ExpectedHealth + actor.Stats.Healing, temp.Stats.Health) };
                    }
                    else
                    {
                        // attack target clone with remaining[0] clone
                        static float ExpectedDamage(VirtualUnit a, VirtualUnit b) => Math.Max(0f, a.Original.Stats.Accuracy - b.Original.Stats.Evasion)/100f*(a.Original.Stats.Attack - b.Original.Stats.Defense);
                        temp = temp with { ExpectedHealth = temp.ExpectedHealth - ExpectedDamage(actor, temp) };
                        if (temp.ExpectedHealth > 0)
                        {
                            bool retaliate = temp.AttackableCells(currentGrid, [temp.Cell]).Contains(actor.Cell);
                            if (retaliate)
                                actor = actor with { ExpectedHealth = actor.ExpectedHealth - ExpectedDamage(temp, actor) };
                            if (actor.ExpectedHealth > 0 && actor.Original.Stats.Agility > temp.Original.Stats.Agility)
                                temp = temp with { ExpectedHealth = temp.ExpectedHealth - ExpectedDamage(actor, temp) };
                            else if (temp.ExpectedHealth > 0 && retaliate && temp.Original.Stats.Agility > actor.Original.Stats.Agility)
                                actor = actor with { ExpectedHealth = actor.ExpectedHealth - ExpectedDamage(temp, actor) };
                        }
                    }
                    currentGrid = currentGrid with { Occupants = currentGrid.Occupants.SetItem(actor.Cell, actor).SetItem(temp.Cell, temp) };

                    IList<VirtualUnit> valid = [.. action != "Attack" ? remaining.Skip(1) : remaining.Skip(1).Where((u) => {
                        bool canAttackSameTarget = u.Original.Behavior.Actions(u, currentGrid).TryGetValue("Attack", out IEnumerable<Vector2I> attackable) && attackable.Contains(target.Cell);
                        bool canSupportMe = u.Original.Behavior.Actions(u, currentGrid).TryGetValue("Support", out IEnumerable<Vector2I> supportable) && supportable.Contains(actor.Cell);
                        return canAttackSameTarget || canSupportMe;
                    })];
                    if (valid.Any())
                        (currentGrid, _, _, _) = ChooseBestMove(valid, currentGrid, action != "Attack" ? [] : [target.Cell, actor.Cell]);
                    
                    GridValue currentGridValue = new(Army.Faction, currentGrid);
                    if (bestGrid is null)
                    {
                        bestGrid = currentGrid;
                        bestGridValue = currentGridValue;
                        bestMove = destination;
                        bestMoveValue = currentMoveValue;
                        bestAction = action;
                        bestTarget = target;
                    }
                    else
                    {
                        if (currentGridValue > bestGridValue || (currentGridValue == bestGridValue && currentMoveValue < bestMoveValue))
                        {
                            bestGrid = currentGrid;
                            bestGridValue = currentGridValue;
                            bestMove = destination;
                            bestMoveValue = currentMoveValue;
                            bestAction = action;
                            bestTarget = target;
                        }
                    }
                }
            }
        }
        return (bestGrid.Value, bestMove, bestAction, bestTarget);
    }

    public override Grid Grid { get => _grid; set => _grid = value; }

    public override void InitializeTurn()
    {
        _selected = null;
        _destination = -Vector2I.One;
        _action = null;
        _target = null;
    }

    private (VirtualUnit selected, Vector2I destination, StringName action, VirtualUnit? target) ComputeAction(IEnumerable<VirtualUnit> available, VirtualGrid grid)
    {
        VirtualUnit? selected = null;
        Vector2I destination = -Vector2I.One;
        StringName action = null;
        VirtualUnit? target = null;

        VirtualGrid? bestGrid = null;
        GridValue bestGridValue = new();
        MoveValue bestMoveValue = new();

        foreach (IList<VirtualUnit> permutation in available.Where((u) => u.Original.Behavior.Actions(u, grid).Count != 0).Permutations())
        {
            (VirtualGrid currentGrid, Vector2I move, StringName currentAction, VirtualUnit? currentTarget) = ChooseBestMove(permutation, grid);
            GridValue currentGridValue = new(Army.Faction, currentGrid);
            MoveValue currentMoveValue = new(grid, permutation[0], move);

            if (currentAction != "End")
            {
                if (bestGrid is null)
                {
                    bestGrid = currentGrid;
                    selected = permutation[0];
                    destination = move;
                    action = currentAction;
                    target = currentTarget;

                    bestGridValue = currentGridValue;
                    bestMoveValue = currentMoveValue;
                }
                else
                {
                    if (currentGridValue > bestGridValue || (currentGridValue == bestGridValue && currentMoveValue < bestMoveValue))
                    {
                        bestGrid = currentGrid;
                        selected = permutation[0];
                        destination = move;
                        action = currentAction;
                        target = currentTarget;

                        bestGridValue = currentGridValue;
                        bestMoveValue = currentMoveValue;
                    }
                }
            }
        }

        // If no one has been selected yet, just pick the unit closest to an enemy
        if (selected is null)
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