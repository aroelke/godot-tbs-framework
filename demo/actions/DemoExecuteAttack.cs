using System;
using System.Collections.Generic;
using Godot;
using TbsFramework.Scenes.Combat;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Actions;

namespace TbsFramework.Demo;

[GlobalClass, Tool]
public partial class DemoExecuteAttack : ActionExecute
{
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
        return CombatCalculations.AttackResults(unit, occupant, false);
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
        if (!unit.Grid.Occupants.TryGetValue(target, out UnitData occupant))
            throw new ArgumentException($"Cell {target} does not contain a unit to attack");
        List<CombatAction> actions = CombatCalculations.AttackResults(unit, occupant, true);
        GridData copy = unit.Grid.Clone();
        ApplyResults(copy, actions);
        return copy;
    }
}