using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Combat;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Control;

namespace TbsFramework.Demo;

[GlobalClass, Tool]
public partial class DemoSupportAction : UnitAction
{
    public static CombatAction CreateSupportAction(UnitData supporter, UnitData recipient) => new(
        supporter, recipient,
        CombatActionType.Support,
        -Math.Min((supporter.Stats as DemoStats).Healing, (recipient.Stats as DemoStats).Health - recipient.Health),
        true
    );

    private static void ApplyResult(GridData grid, CombatAction action)
    {
        // Get the version of the action's actor on the input grid to avoid updating the wrong grid
        UnitData target = grid.Occupants[action.Target.Cell];
        // Remember that the amount of healing from a healing action is stored as negative damage
        target.Health -= action.Damage;
    }

    public override bool RequiresTarget => true;

    public override bool CanPerform(UnitData unit, Vector2I source) => (unit.Stats as DemoStats).Healing > 0;
    public override bool CanPerform(UnitData unit, Vector2I source, Vector2I target) => CanPerform(unit, source) && (unit.Stats as DemoStats).SupportRange.Contains(source.ManhattanDistanceTo(target));
    public override IEnumerable<Vector2I> GetTargetCells(UnitData unit, Vector2I cell) =>
        unit.Grid.GetCellsInRange(cell, (unit.Stats as DemoStats).SupportRange).Where((c) => unit.Grid.Occupants.TryGetValue(c, out UnitData occupant) && unit.Faction.AlliedTo(occupant.Faction));

    public override IEnumerable<Vector2I> GetAllTargetCells(UnitData unit, IEnumerable<Vector2I> traversable)
    {
        DemoStats stats = unit.Stats as DemoStats;
        return traversable.SelectMany((c) => unit.Grid.GetCellsInRange(c, stats.SupportRange)).ToHashSet();
    }

    public override IEnumerable<Vector2I> GetValidTargetCells(UnitData unit, IEnumerable<Vector2I> traversable) =>
        GetAllTargetCells(unit, traversable).Where((c) => unit.Grid.Occupants.TryGetValue(c, out UnitData occupant) && occupant != unit && occupant.Faction.AlliedTo(unit.Faction));
    public override IEnumerable<Vector2I> GetSourceCells(UnitData unit, Vector2I target) => unit.Grid.GetCellsInRange(target, (unit.Stats as DemoStats).SupportRange);

    public override UnitActionResult Perform(UnitData unit, Vector2I target)
    {
        if (!unit.Grid.Occupants.TryGetValue(target, out UnitData occupant))
            throw new ArgumentException($"Cell {target} does not contain a unit to attack");
        return new(CreateSupportAction(unit, occupant), unit, target, this);
    }

    public override void UpdateGrid(GridData grid, UnitActionResult result)
    {
        if (result.Result is not CombatAction action)
            throw new ArgumentException("Support action result is not a combat action");
        ApplyResult(grid, action);
    }

    public override GridData Simulate(UnitData unit, Vector2I source, Vector2I target)
    {
        if (!unit.Grid.Occupants.TryGetValue(target, out UnitData occupant))
            throw new ArgumentException($"Cell {target} does not contain a unit to attack");
        GridData copy = unit.Grid.Clone();
        copy.Occupants[unit.Cell].Cell = source;
        CombatAction action = CreateSupportAction(copy.Occupants[source], copy.Occupants[target]);
        ApplyResult(copy, action);
        return copy;
    }
}