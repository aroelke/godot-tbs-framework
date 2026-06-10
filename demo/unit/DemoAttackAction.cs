using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Combat;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Demo;

public class DemoAttackAction : IUnitAction
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

    public StringName Name => "Attack";

    public bool CanPerform(UnitData unit) => unit.Stats.Attack > 0;

    public bool CanPerform(UnitData unit, Vector2I source, Vector2I target) => CanPerform(unit) && unit.Stats.AttackRange.Contains(source.ManhattanDistanceTo(target));

    public IEnumerable<Vector2I> GetTargetCells(UnitData unit, Vector2I cell) => unit.GetAttackableCells(cell).Where((c) => unit.Grid.Occupants.TryGetValue(c, out UnitData occupant) && !occupant.Faction.AlliedTo(unit.Faction));

    public IEnumerable<Vector2I> ShowAllTargetCells(UnitData unit) => unit.GetAttackableCellsInReach();

    public IEnumerable<Vector2I> GetAllTargetCells(UnitData unit) => unit.GetFilteredAttackableCellsInReach();

    public UnitActionResult Perform(UnitData unit, Vector2I target)
    {
        if (!unit.Grid.Occupants.TryGetValue(target, out UnitData occupant))
            throw new ArgumentException($"Cell {target} does not contain a unit to attack");
        return new(CombatCalculations.AttackResults(unit, occupant, false), unit, target, this);
    }

    public GridData Simulate(UnitData unit, Vector2I source, Vector2I target)
    {
        if (!unit.Grid.Occupants.TryGetValue(target, out UnitData occupant))
            throw new ArgumentException($"Cell {target} does not contain a unit to attack");
        List<CombatAction> actions = CombatCalculations.AttackResults(unit, occupant, true);
        GridData copy = unit.Grid.Clone();
        ApplyResults(copy, actions);
        return copy;
    }

    public void UpdateGrid(GridData grid, UnitActionResult result)
    {
        if (result.Result is not List<CombatAction> actions)
            throw new ArgumentException("Attack action result is not a list of combat actions");
        ApplyResults(grid, actions);
    }
}