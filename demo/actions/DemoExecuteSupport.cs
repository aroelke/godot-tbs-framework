using System;
using Godot;
using TbsFramework.Scenes.Combat;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Actions;

namespace TbsFramework.Demo;

[GlobalClass, Tool]
public partial class DemoExecuteSupport : ActionExecute
{
    private static void ApplyResult(GridData grid, CombatAction action)
    {
        // Get the version of the action's actor on the input grid to avoid updating the wrong grid
        UnitData target = grid.Occupants[action.Target.Cell];
        // Remember that the amount of healing from a healing action is stored as negative damage
        target.Health -= action.Damage;
    }

    public override UnitActionExecuteResult Perform(UnitData unit, Vector2I target)
    {
        if (!unit.Grid.Occupants.TryGetValue(target, out UnitData occupant))
            throw new ArgumentException($"Cell {target} does not contain a unit to attack");
        return new(CombatCalculations.CreateSupportAction(unit, occupant), unit, target, this);
    }

    public override void UpdateGrid(GridData grid, UnitActionExecuteResult result)
    {
        if (result.Result is not CombatAction action)
            throw new ArgumentException("Support action result is not a combat action");
        ApplyResult(grid, action);
    }

    public override GridData Simulate(UnitData unit, Vector2I source, Vector2I target)
    {
        if (!unit.Grid.Occupants.TryGetValue(target, out UnitData occupant))
            throw new ArgumentException($"Cell {target} does not contain a unit to attack");
        CombatAction action = CombatCalculations.CreateSupportAction(unit, occupant);
        GridData copy = unit.Grid.Clone();
        ApplyResult(copy, action);
        return copy;
    }
}