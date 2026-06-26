using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Combat;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Demo;

[GlobalClass, Tool]
public partial class DemoSupportAction : FlatUnitAction
{
    private static void ApplyResult(GridData grid, CombatAction action)
    {
        // Get the version of the action's actor on the input grid to avoid updating the wrong grid
        UnitData target = grid.Occupants[action.Target.Cell];
        // Remember that the amount of healing from a healing action is stored as negative damage
        target.Health -= action.Damage;
    }

    public override bool CanPerform(UnitData unit, Vector2I source, Vector2I target) => unit.Stats.Healing > 0 && unit.Stats.SupportRange.Contains(source.ManhattanDistanceTo(target));

    public override IEnumerable<Vector2I> GetTargetCells(UnitData unit, Vector2I cell) => unit.GetSupportableCells(cell).Where((c) => unit.Grid.Occupants.TryGetValue(c, out UnitData occupant) && occupant.Faction.AlliedTo(unit.Faction));

    public override IEnumerable<Vector2I> ShowAllTargetCells(UnitData unit) => unit.GetSupportableCellsInReach();

    public override IEnumerable<Vector2I> GetAllTargetCells(UnitData unit) => unit.GetFilteredSupportableCellsInReach();

    public override UnitActionResult Perform(UnitData unit, Vector2I target)
    {
        if (!unit.Grid.Occupants.TryGetValue(target, out UnitData occupant))
            throw new ArgumentException($"Cell {target} does not contain a unit to attack");
        return new(CombatCalculations.CreateSupportAction(unit, occupant), unit, target, this);
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

    public override void UpdateGrid(GridData grid, UnitActionResult result)
    {
        if (result.Result is not CombatAction action)
            throw new ArgumentException("Support action result is not a combat action");
        ApplyResult(grid, action);
    }
}