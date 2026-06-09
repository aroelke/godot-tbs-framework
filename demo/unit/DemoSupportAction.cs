using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Combat;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Demo;

public class DemoSupportAction : IUnitAction<List<CombatAction>>
{
    public bool CanPerform(UnitData unit) => unit.Stats.Healing > 0;

    public bool CanPerform(UnitData unit, Vector2I source, Vector2I target) => CanPerform(unit) && unit.Stats.SupportRange.Contains(source.ManhattanDistanceTo(target));

    public IEnumerable<Vector2I> ShowAllTargetCells(UnitData unit) => unit.GetSupportableCellsInReach();

    public IEnumerable<Vector2I> GetAllTargetCells(UnitData unit) => unit.GetFilteredSupportableCellsInReach();

    public IEnumerable<Vector2I> GetTargetCells(UnitData unit, Vector2I cell) => unit.GetSupportableCells(cell);

    public List<CombatAction> Perform(UnitData unit, Vector2I target)
    {
        if (!unit.Grid.Occupants.TryGetValue(target, out UnitData occupant))
            throw new ArgumentException($"Cell {target} does not contain a unit to attack");
        return [CombatCalculations.CreateSupportAction(unit, occupant)];
    }

    public GridData Simulate(UnitData unit, Vector2I source, Vector2I target)
    {
        if (!unit.Grid.Occupants.TryGetValue(target, out UnitData occupant))
            throw new ArgumentException($"Cell {target} does not contain a unit to attack");
        CombatAction action = CombatCalculations.CreateSupportAction(unit, occupant);
        GridData copy = unit.Grid.Clone();
        UpdateGrid(copy, [action]);
        return copy;
    }

    public void UpdateGrid(GridData grid, List<CombatAction> results)
    {
        // Get the version of the action's actor on the input grid to avoid updating the wrong grid
        UnitData target = grid.Occupants[results[0].Target.Cell];
        // Remember that the amount of healing from a healing action is stored as negative damage
        target.Health -= results[0].Damage;
    }
}