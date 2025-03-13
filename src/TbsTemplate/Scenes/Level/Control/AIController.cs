using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsTemplate.Data;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Combat.Data;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="UnitBehavior"/>s and the state of the level.</summary>
public partial class AIController : ArmyController
{
    /// <summary>Acts as a "value" for a grid which can be compared to other values and evaluate grids against each other.</summary>
    /// <param name="Source">Faction evaluating the grid.</param>
    /// <param name="Grid">Grid to be evaluated.</param>
    private readonly record struct GridValue(Faction Source, Grid Grid) : IEquatable<GridValue>, IComparable<GridValue>
    {
        public static bool operator>(GridValue a, GridValue b) => a.CompareTo(b) > 0;
        public static bool operator<(GridValue a, GridValue b) => a.CompareTo(b) < 0;

        /// <summary>Enemy units of <see cref="Source"/>, sorted in increasing order of current health.</summary>
        private readonly IEnumerable<Unit> _enemies = Grid.Occupants.Values.OfType<Unit>().Where((u) => !Source.AlliedTo(u.Army.Faction)).OrderBy(static (u) => u.Health.Value);
        private readonly IEnumerable<Unit> _allies = Grid.Occupants.Values.OfType<Unit>().Where((u) => Source.AlliedTo(u.Army.Faction));

        /// <summary>Difference between enemy units' current and maximum health, summed over all enemy units. Higher is better.</summary>
        public int EnemyHealthDifference => _enemies.Select(static (u) => u.Health.Maximum - u.Health.Value).Sum();

        /// <summary>Difference between ally units' current and maximum health, summed over all allied units. Lower is better.</summary>
        public int AllyHealthDifference => _allies.Select(static (u) => u.Health.Maximum - u.Health.Value).Sum();

        public readonly int CompareTo(GridValue other)
        {
            // Lower least health among units with different heatlh values is greater
            foreach ((Unit me, Unit you) in _enemies.Zip(other._enemies))
                if (me.Health.Value != you.Health.Value)
                    return you.Health.Value - me.Health.Value;

            // Higher enemy health difference is greater
            int diff = EnemyHealthDifference - other.EnemyHealthDifference;
            if (diff != 0)
                return diff;

            // Lower ally health difference is greater
            diff = other.AllyHealthDifference - AllyHealthDifference;
            if (diff != 0)
                return diff;

            return 0;
        }

        public bool Equals(GridValue other) => CompareTo(other) == 0;
        public override int GetHashCode() => Grid.GetHashCode();
    }

    /// <summary>Acts as a "value" for a move of a unit on a grid which can be compared to other values to evaluate moves against each other.</summary>
    /// <param name="Unit">Unit that's moving.</param>
    /// <param name="Destination">Potential destination of the move.</param>
    /// <param name="Grid">Grid on which the unit will move.</param>
    private readonly record struct MoveValue(Unit Unit, Vector2I Destination) : IEquatable<MoveValue>, IComparable<MoveValue>
    {
        public static bool operator>(MoveValue a, MoveValue b) => a.CompareTo(b) > 0;
        public static bool operator<(MoveValue a, MoveValue b) => a.CompareTo(b) < 0;

        /// <summary>Starting cell of the move.</summary>
        public readonly Vector2I Source = Unit.Cell;

        /// <summary>Manhattan distance from <see cref="Unit"/>'s cell to <see cref="Destination"/>. Lower is better.</summary>
        public readonly int Distance = Unit.Cell.ManhattanDistanceTo(Destination);

        /// <summary>Path cost from <see cref="Unit"/>'s cell to <see cref="Destination"/>. Lower is better.</summary>
        public readonly int Cost = Path.Empty(Unit.Grid, Unit.TraversableCells()).Add(Unit.Cell).Add(Destination).Cost;

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

    static void CleanUpGrid(Grid grid)
    {
        foreach ((_, GridNode node) in grid.Occupants)
            node.Free();
        grid.Free();
    }

    private Grid _grid = null;
    private Unit _selected = null;
    private Vector2I _destination = -Vector2I.One;
    private StringName _action = null;
    private Unit _target = null;

    private (Grid, Vector2I) ChooseBestMove(Unit enemy, IList<Unit> allies, Grid grid)
    {
        IEnumerable<Vector2I> destinations = allies[0].AttackableCells(enemy.Cell).Where((c) => allies[0].Behavior.Destinations(allies[0]).Contains(c));
        if (!destinations.Any())
        {
            if (allies.Count > 1)
            {
                Grid current = DuplicateGrid(grid);
                return ChooseBestMove(enemy, [.. allies.Skip(1).Select((a) => current.Occupants[a.Cell]).OfType<Unit>()], current);
            }
            else
                return (DuplicateGrid(grid), allies[0].Cell);
        }

        Grid best = null;
        GridValue bestGridValue = new();
        Vector2I move = -Vector2I.One;
        MoveValue bestMoveValue = new();
        foreach (Vector2I destination in destinations)
        {
            Grid current = DuplicateGrid(grid);
            Unit actor = current.Occupants[allies[0].Cell] as Unit;
            Unit target = current.Occupants[enemy.Cell] as Unit;

            // move allies[0] clone to destination
            MoveValue currentMoveValue = new(actor, destination);
            current.Occupants.Remove(actor.Cell);
            actor.Cell = destination;
            current.Occupants[destination] = actor;

            // attack target clone with allies[0] clone
            List<CombatAction> results = CombatCalculations.AttackResults(actor, target);
            for (int i = 0; i < results.Count && actor.Health.Value > 0 && target.Health.Value > 0; i++)
                results[i].Target.Health.Value -= (int)Mathf.Round(results[i].Damage*CombatCalculations.HitChance(results[i].Actor, results[i].Target)/100f);

            if (allies.Count > 1)
                (current, _) = ChooseBestMove(target, [.. allies.Skip(1).Select((a) => current.Occupants[a.Cell]).OfType<Unit>()], current);

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
                    CleanUpGrid(best);
                    best = current;
                    bestGridValue = currentGridValue;
                    move = destination;
                    bestMoveValue = currentMoveValue;
                }
                else
                    CleanUpGrid(current);
            }
        }
        return (best, move);
    }

    public override Grid Grid { get => _grid; set => _grid = value; }

    public override void InitializeTurn()
    {
        _selected = null;
        _destination = -Vector2I.One;
        _action = null;
        _target = null;
    }

    public (Unit selected, Vector2I destination, StringName action, Unit target) ComputeAction(IEnumerable<Unit> available, IEnumerable<Unit> enemies, Grid grid)
    {
        Unit selected = null;
        Vector2I destination = -Vector2I.One;
        StringName action = null;
        Unit target = null;

        Grid best = null;
        GridValue bestGridValue = new();
        MoveValue bestMoveValue = new();
        foreach (Unit enemy in enemies)
        {
            IEnumerable<Unit> attackers = available.Where((u) => {
                Dictionary<StringName, IEnumerable<Vector2I>> actions = u.Behavior.Actions(u);
                return actions.ContainsKey("Attack") && actions["Attack"].Contains(enemy.Cell);
            });

            foreach (IList<Unit> permutation in attackers.Permutations())
            {
                (Grid current, Vector2I move) = ChooseBestMove(enemy, permutation, grid);
                GridValue currentGridValue = new(Army.Faction, current);
                MoveValue currentMoveValue = new(permutation[0], move);

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
                        CleanUpGrid(best);
                        best = current;
                        selected = permutation[0];
                        destination = move;
                        action = "Attack";
                        target = enemy;

                        bestGridValue = currentGridValue;
                        bestMoveValue = currentMoveValue;
                    }
                    else
                        CleanUpGrid(current);
                }
            }
        }

        // If no one has been selected yet, just pick the unit closest to an enemy
        if (selected is null)
        {
            selected = enemies.Any() ? available.MinBy((u) => enemies.Select((e) => u.Cell.DistanceTo(e.Cell)).Min()) : available.First();
            action = "End";

            IEnumerable<Unit> ordered = enemies.OrderBy((u) => u.Cell.DistanceTo(selected.Cell));
            if (ordered.Any())
                destination = selected.Behavior.Destinations(selected).OrderBy((c) => selected.Behavior.GetPath(selected, c).Cost).OrderBy((c) => c.DistanceTo(ordered.First().Cell)).First();
            else
                destination = selected.Cell;
        }

        return (selected, destination, action, target);
    }

    public override async void SelectUnit()
    {
        // Compute this outside the task because it calls Node.GetChildren(), which has to be called on the same thread as that node.
        // Also, use a collection expression to immediately evaluated it rather than waiting until later, because that will be in the
        // wrong thread.
        Grid copy = DuplicateGrid(Grid);
        IEnumerable<Unit> available = [.. ((IEnumerable<Unit>)Army).Where(static (u) => u.Active)];
        IEnumerable<Unit> enemies = Grid.Occupants.Values.OfType<Unit>().Where((u) => !Army.Faction.AlliedTo(u));

        (_selected, _destination, _action, _target) = await Task.Run<(Unit, Vector2I, StringName, Unit)>(() => ComputeAction(available, enemies, copy));

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