using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Combat;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Actions;

namespace TbsFramework.Demo;

[GlobalClass, Tool]
public partial class DemoExecuteAttack : ActionExecute
{
    private static List<CombatAction> AttackResults(UnitData a, UnitData b, bool estimate)
    {
        Dictionary<UnitData, double> damage = new() {{ a, 0 }, { b, 0 }};
        // Compute complete combat action list
        List<CombatAction> actions = [CombatCalculations.CreateAttackAction(a, b, estimate)];
        if (actions[^1].Hit)
            damage[b] += actions[^1].Damage;
        if (damage[b] < b.Health && b.Stats.AttackRange.Contains(b.Cell.ManhattanDistanceTo(a.Cell)))
        {
            actions.Add(CombatCalculations.CreateAttackAction(b, a, estimate));
            if (actions[^1].Hit)
                damage[a] += actions[^1].Damage;
        }
        if (CombatCalculations.FollowUp(a, b) is (UnitData doubler, UnitData doublee) && damage[doubler] < doubler.Health && doubler.Stats.AttackRange.Contains(doubler.Cell.ManhattanDistanceTo(doublee.Cell)))
        {
            actions.Add(CombatCalculations.CreateAttackAction(doubler, doublee, estimate));
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

    public override object Perform(UnitData unit, Vector2I target)
    {
        if (!unit.Grid.Occupants.TryGetValue(target, out UnitData occupant))
            throw new ArgumentException($"Cell {target} does not contain a unit to attack");
        return AttackResults(unit, occupant, false);
    }

    public override void UpdateGrid(GridData grid, UnitData actor, Vector2I target, object result)
    {
        if (result is not List<CombatAction> actions)
            throw new ArgumentException("Attack action result is not a list of combat actions");

        ApplyResults(grid, actions);
        if (actor.Health <= 0)
            actor.Renderer.Die();
        if (grid.Occupants[target].Health <= 0)
            grid.Occupants[target].Renderer.Die();
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