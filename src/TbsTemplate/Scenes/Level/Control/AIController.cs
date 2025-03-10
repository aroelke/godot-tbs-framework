using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Combat.Data;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="UnitBehavior"/>s and the state of the level.</summary>
public partial class AIController : ArmyController
{
    public enum DecisionType
    {
        ClosestEnemy, // Activate units in order of proximity to their enemies
        TargetLoop
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

    private static Grid DuplicateGrid(Grid grid)
    {
        Grid copy = grid.Duplicate((int)(DuplicateFlags.Scripts | DuplicateFlags.UseInstantiation)) as Grid;
        foreach ((Vector2I cell, GridNode occupant) in grid.Occupants)
        {
            GridNode clone = occupant.Duplicate((int)(DuplicateFlags.Scripts | DuplicateFlags.UseInstantiation)) as Unit;
            if (clone is Unit u)
                u.Army = u.Army;
            copy.Occupants[cell] = clone;
        }

        return copy;
    }

    private static Grid CompareGrids(Grid a, Grid b)
    {
        return a;
    }

    private Grid _grid = null;
    private Unit _selected = null;
    private Vector2I _destination = -Vector2I.One;
    private StringName _action = null;
    private Unit _target = null;

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
        case DecisionType.TargetLoop:
            if (enemies.Any())
            {
                // optimize for enemy unit with lowest remaining health
                double best = double.PositiveInfinity;
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

                        if (selected is null || remaining < best)
                        {
                            selected = permutation[0];
                            action = "Attack";
                            destination = source;
                            target = enemy;
                            best = remaining;
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