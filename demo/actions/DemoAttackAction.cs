using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Combat;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Control;

namespace TbsFramework.Demo;

/// <summary><see cref="UnitAction"/> used in the demo for allowing units to attack other units in opposing factions.</summary>
[GlobalClass, Tool]
public partial class DemoAttackAction : UnitAction
{
    private static readonly Random rnd = new();

    private static List<CombatAction> AttackResults(UnitData a, UnitData b, bool estimate)
    {
        static CombatAction CreateAttackAction(UnitData attacker, UnitData defender, bool estimate) => new(
            attacker, defender,
            CombatActionType.Attack,
            Math.Max((attacker.Stats as DemoStats).Attack - (defender.Stats as DemoStats).Defense, 0)*(estimate ? HitChance(attacker, defender)/100.0 : 1),
            estimate || rnd.Next(100) < HitChance(attacker, defender)
        );

        DemoStats aStats = a.Stats as DemoStats, bStats = b.Stats as DemoStats;
        Dictionary<UnitData, double> damage = new() {{ a, 0 }, { b, 0 }};

        List<CombatAction> actions = [CreateAttackAction(a, b, estimate)];
        if (actions[^1].Hit)
            damage[b] += actions[^1].Damage;
        if (damage[b] < b.Health && bStats.AttackRange.Contains(b.Cell.ManhattanDistanceTo(a.Cell)))
        {
            actions.Add(CreateAttackAction(b, a, estimate));
            if (actions[^1].Hit)
                damage[a] += actions[^1].Damage;
        }

        UnitData doubler, doublee;
        if (aStats.Agility > bStats.Agility)
        {
            doubler = a;
            doublee = b;
        }
        else if (aStats.Agility < bStats.Agility)
        {
            doubler = b;
            doublee = a;
        }
        else
            doubler = doublee = null;
        if (doubler is not null && doublee is not null && damage[doubler] < doubler.Health && (doubler.Stats as DemoStats).AttackRange.Contains(doubler.Cell.ManhattanDistanceTo(doublee.Cell)))
        {
            actions.Add(CreateAttackAction(doubler, doublee, estimate));
            if (actions[^1].Hit)
                damage[doublee] += actions[^1].Damage;
        }

        return actions;
    }

    private static void ApplyResults(GridData grid, List<CombatAction> results)
    {
        foreach (CombatAction action in results)
        {
            // Get the version of the action's actor on the input grid to avoid updating the wrong grid
            UnitData target = grid.Occupants[action.Target.Cell];

            if (action.Hit)
                target.Health -= action.Damage;
        }
    }

    public static int HitChance(UnitData attacker, UnitData defender) => (attacker.Stats as DemoStats).Accuracy - (defender.Stats as DemoStats).Evasion;

    public override bool RequiresTarget => true;

    public override bool CanPerform(UnitData unit, Vector2I source) => (unit.Stats as DemoStats).Attack > 0;
    public override bool CanPerform(UnitData unit, Vector2I source, Vector2I target) => CanPerform(unit, source) && (unit.Stats as DemoStats).AttackRange.Contains(source.ManhattanDistanceTo(target));
    public override IEnumerable<Vector2I> GetTargetCells(UnitData unit, Vector2I cell) =>
        unit.Grid.GetCellsInRange(cell, (unit.Stats as DemoStats).AttackRange).Where((c) => unit.Grid.Occupants.TryGetValue(c, out UnitData occupant) && !unit.Faction.AlliedTo(occupant.Faction));

    public override IEnumerable<Vector2I> GetAllTargetCells(UnitData unit, IEnumerable<Vector2I> traversable)
    {
        DemoStats stats = unit.Stats as DemoStats;
        return traversable.SelectMany((c) => unit.Grid.GetCellsInRange(c, stats.AttackRange)).ToHashSet();
    }

    public override IEnumerable<Vector2I> GetValidTargetCells(UnitData unit, IEnumerable<Vector2I> traversable) =>
        GetAllTargetCells(unit, traversable).Where((c) => unit.Grid.Occupants.TryGetValue(c, out UnitData occupant) && !occupant.Faction.AlliedTo(unit.Faction));
    public override IEnumerable<Vector2I> GetSourceCells(UnitData unit, Vector2I target) => unit.Grid.GetCellsInRange(target, (unit.Stats as DemoStats).AttackRange);

    public override UnitActionResult Perform(UnitData unit, Vector2I target)
    {
        if (!unit.Grid.Occupants.TryGetValue(target, out UnitData occupant))
            throw new ArgumentException($"Cell {target} does not contain a unit to attack");
        return new(AttackResults(unit, occupant, false), unit, target, this);
    }

    public override void UpdateGrid(GridData grid, UnitActionResult result)
    {
        if (result.Result is not List<CombatAction> actions)
            throw new ArgumentException("Attack action result is not a list of combat actions");

        ApplyResults(grid, actions);
        if (result.Actor.Health <= 0)
            result.Actor.Renderer.Die();
        if (grid.Occupants[result.Target].Health <= 0)
            grid.Occupants[result.Target].Renderer.Die();
    }

    public override GridData Simulate(UnitData unit, Vector2I source, Vector2I target)
    {
        if (!unit.Grid.Occupants.ContainsKey(target))
            throw new ArgumentException($"Cell {target} does not contain a unit to attack");

        GridData copy = unit.Grid.Clone();
        copy.Occupants[unit.Cell].Cell = source;
        List<CombatAction> actions = AttackResults(copy.Occupants[source], copy.Occupants[target], true);
        ApplyResults(copy, actions);
        return copy;
    }
}