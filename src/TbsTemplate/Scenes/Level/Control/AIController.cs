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
    public enum DecisionType
    {
        ClosestEnemy, // Activate units in order of proximity to their enemies
        TargetLoopHeuristic,
        TargetLoopDuplication
    }

    private readonly record struct MoveEvaluation(Unit Unit, double Dealt, double Taken, Vector2I Closest)
    {
        public static bool operator >(MoveEvaluation a, MoveEvaluation b)
        {
            if (a.Dealt > b.Dealt)
                return true;
            else if (a.Dealt == b.Dealt && a.PathCost() < b.PathCost())
                return true;
            else
                return false;
        }
        public static bool operator <(MoveEvaluation a, MoveEvaluation b) => a != b && !(a > b);

        public readonly void Deconstruct(out double dealt, out double taken, out Vector2I closest)
        {
            dealt = Dealt;
            taken = Taken;
            closest = Closest;
        }

        public readonly int PathCost() => Unit.Behavior.GetPath(Unit, Closest).Cost;
    }

    private readonly record struct GridValue(Faction Source, Grid Grid) : IComparable<GridValue>
    {
        public static bool operator>(GridValue a, GridValue b) => a.CompareTo(b) > 0;
        public static bool operator<(GridValue a, GridValue b) => a.CompareTo(b) < 0;

        private readonly IEnumerable<Unit> _enemies = Grid.Occupants.Values.OfType<Unit>().Where((u) => !@Source.AlliedTo(u.Army.Faction));

        /// <summary>Difference between enemy units' current and maximum health, summed over all enemy units.  Higher is better.</summary>
        public int HealthDifference => _enemies.Select(static (u) => u.Health.Maximum - u.Health.Value).Sum();

        public int CompareTo(GridValue other)
        {
            // Lower least health among units with different heatlh values is greater
            IList<Unit> enemiesOrdered = [.. _enemies.OrderBy(static (u) => u.Health.Value)];
            IList<Unit> otherOrdered = [.. other._enemies.OrderBy(static (u) => u.Health.Value)];
            foreach ((Unit me, Unit you) in enemiesOrdered.Zip(otherOrdered))
                if (me.Health.Value != you.Health.Value)
                    return you.Health.Value - me.Health.Value;

            // Higher health difference is greater
            int diff = HealthDifference - other.HealthDifference;
            if (diff != 0)
                return diff;

            return 0;
        }
    }

    private static Grid DuplicateGrid(Grid grid)
    {
        Grid copy = grid.Duplicate((int)(DuplicateFlags.Scripts | DuplicateFlags.UseInstantiation)) as Grid;
        foreach ((Vector2I cell, GridNode occupant) in grid.Occupants)
        {
            if (occupant is Unit o)
            {
                GridNode clone = o.Duplicate((int)(DuplicateFlags.Scripts | DuplicateFlags.UseInstantiation)) as GridNode;
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
                return ChooseBestMove(enemy, [.. allies.Skip(1)], DuplicateGrid(grid));
            else
                return (DuplicateGrid(grid), allies[0].Cell);
        }

        Grid best = null;
        Vector2I move = -Vector2I.One;
        foreach (Vector2I destination in destinations)
        {
            Grid current = DuplicateGrid(grid);
            Unit actor = current.Occupants[allies[0].Cell] as Unit;
            Unit target = current.Occupants[enemy.Cell] as Unit;

            // move allies[0] clone to destination
            actor.Cell = destination;
            current.Occupants.Remove(allies[0].Cell);
            current.Occupants[destination] = actor;

            // attack target clone with allies[0] clone
            List<CombatAction> results = CombatCalculations.AttackResults(actor, target);
            for (int i = 0; i < results.Count && actor.Health.Value > 0 && target.Health.Value > 0; i++)
                results[i].Target.Health.Value -= (int)Mathf.Round(results[i].Damage*CombatCalculations.HitChance(results[i].Actor, results[i].Target)/100f);

            if (allies.Count > 1)
                (current, move) = ChooseBestMove(target, [.. allies.Skip(1)], current);
            if (best == null)
            {
                best = current;
                move = destination;
            }
            else
            {
                if (new GridValue(Army.Faction, current) > new GridValue(Army.Faction, best))
                {
                    CleanUpGrid(best);
                    best = current;
                }
                else
                    CleanUpGrid(current);
            }
        }
        return (best, move);
    }

    [Export] public DecisionType Decision = DecisionType.ClosestEnemy;

    public override Grid Grid { get => _grid; set => _grid = value; }

    public override void InitializeTurn()
    {
        _selected = null;
        _destination = -Vector2I.One;
        _action = null;
        _target = null;
    }

    public (Unit selected, Vector2I destination, StringName action, Unit target) ComputeAction(IEnumerable<Unit> available, IEnumerable<Unit> enemies)
    {
        Unit selected = null;
        Vector2I destination = -Vector2I.One;
        StringName action = null;
        Unit target = null;

        switch (Decision)
        {
        case DecisionType.ClosestEnemy:
            selected = enemies.Any() ? available.MinBy((u) => enemies.Select((e) => u.Cell.DistanceTo(e.Cell)).Min()) : available.First();

            IEnumerable<Vector2I> destinations = selected.Behavior.Destinations(selected);
            Dictionary<StringName, IEnumerable<Vector2I>> actions = selected.Behavior.Actions(selected);

            IEnumerable<Unit> attackable = actions.ContainsKey("Attack") ? actions["Attack"].Select((c) => selected.Grid.Occupants[c]).OfType<Unit>().OrderBy((u) => u.Cell.DistanceTo(selected.Cell)) : [];
            if (attackable.Any())
            {
                action = "Attack";
                target = attackable.First();
                destination = selected.AttackableCells(target.Cell).Where(destinations.Contains).OrderBy((c) => selected.Behavior.GetPath(selected, c).Cost).First();
            }
            else
            {
                IEnumerable<Unit> ordered = enemies.OrderBy((u) => u.Cell.DistanceTo(selected.Cell));
                if (ordered.Any())
                    destination = destinations.OrderBy((c) => selected.Behavior.GetPath(selected, c).Cost).OrderBy((c) => c.DistanceTo(ordered.First().Cell)).First();
                else
                    destination = selected.Cell;
                action = "End";
            }
            break;
        case DecisionType.TargetLoopHeuristic:
            if (enemies.Any())
            {
                // optimize for enemy unit with lowest remaining health
                double hurt = double.PositiveInfinity;
                foreach (Unit enemy in enemies.OrderBy((u) => u.Health.Value))
                {
                    IEnumerable<Unit> attackers = available.Where((u) => {
                        Dictionary<StringName, IEnumerable<Vector2I>> actions = u.Behavior.Actions(u);
                        return actions.ContainsKey("Attack") && actions["Attack"].Contains(enemy.Cell);
                    });

                    foreach (IList<Unit> permutation in attackers.Permutations())
                    {
                        MoveEvaluation EvaluateAction(IList<Unit> units, IImmutableSet<Vector2I> blocked=null)
                        {
                            if (units.Count == 0)
                                return new(null, 0, 0, Vector2I.Zero);

                            blocked ??= [];
                            MoveEvaluation best = new(units[0], 0, 0, units[0].Cell);

                            foreach (Vector2I destination in units[0].AttackableCells(enemy.Cell).Where((c) => units[0].Behavior.Destinations(units[0]).Contains(c) && !blocked.Contains(c)))
                            {
                                MoveEvaluation next = EvaluateAction([.. units.Skip(1)], blocked.Add(destination));
                                MoveEvaluation evaluation = new(best.Unit, next.Dealt + CombatCalculations.TotalExpectedDamage(enemy, CombatCalculations.AttackResults(best.Unit, enemy)), 0, destination);
                                if (evaluation > best)
                                    best = evaluation;
                            }

                            return best;
                        }

                        (double damage, _, Vector2I source) = EvaluateAction(permutation);
                        double remaining = enemy.Health.Value - damage;

                        if (selected is null || remaining < hurt)
                        {
                            selected = permutation[0];
                            action = "Attack";
                            destination = source;
                            target = enemy;
                            hurt = remaining;
                        }
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
            break;
        case DecisionType.TargetLoopDuplication:
            Grid best = null;
            foreach (Unit enemy in enemies)
            {
                IEnumerable<Unit> attackers = available.Where((u) => {
                    Dictionary<StringName, IEnumerable<Vector2I>> actions = u.Behavior.Actions(u);
                    return actions.ContainsKey("Attack") && actions["Attack"].Contains(enemy.Cell);
                });

                foreach (IList<Unit> permutation in attackers.Permutations())
                {
                    (Grid current, Vector2I move) = ChooseBestMove(enemy, permutation, Grid);
                    if (best is null || new GridValue(Army.Faction, current) > new GridValue(Army.Faction, best))
                    {
                        if (best is not null)
                            CleanUpGrid(best);
                        best = current;
                        selected = permutation[0];
                        destination = move;
                        action = "Attack";
                        target = enemy;
                    }
                    else
                        CleanUpGrid(current);
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
            break;
        default:
            break;
        }

        return (selected, destination, action, target);
    }

    public override async void SelectUnit()
    {
        // Compute this outside the task because it calls Node.GetChildren(), which has to be called on the same thread as that node.
        // Also, use a collection expression to immediately evaluated it rather than waiting until later, because that will be in the
        // wrong thread.
        IEnumerable<Unit> available = [.. ((IEnumerable<Unit>)Army).Where(static (u) => u.Active)];
        IEnumerable<Unit> enemies = Grid.Occupants.Values.OfType<Unit>().Where((u) => !Army.Faction.AlliedTo(u));

        (_selected, _destination, _action, _target) = await Task.Run<(Unit, Vector2I, StringName, Unit)>(() => ComputeAction(available, enemies));

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