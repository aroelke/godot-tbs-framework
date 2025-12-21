using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Level.Layers;
using TbsFramework.Scenes.Level.Map;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Control;

/// <summary><see cref="Unit"/> behavior that allows the unit to move around the grid to perform actions.</summary>
[Tool]
public partial class MoveBehavior : Behavior
{
    public override IEnumerable<Vector2I> Destinations(IUnit unit, IGrid grid) => unit.TraversableCells(grid).Where((c) => !grid.GetOccupantUnits().TryGetValue(c, out IUnit occupant) || c == unit.Cell);

    public override IEnumerable<UnitAction> Actions(IUnit unit, IGrid grid)
    {
        IEnumerable<Vector2I> destinations = Destinations(unit, grid);
        List<UnitAction> actions = [];

        foreach (ISpecialActionRegion region in grid.GetSpecialActionRegions())
        {
            IEnumerable<Vector2I> actionable = region.Cells.Intersect(destinations).Where((c) => region.CanPerform(unit, c));
            actions.AddRange(actionable.Select((a) => new UnitAction(region.Action, [a], a, destinations)));
        }

        IEnumerable<Vector2I> enemies = unit.AttackableCells(grid, destinations).Where((c) => grid.GetOccupantUnits().TryGetValue(c, out IUnit occupant) && !occupant.Faction.AlliedTo(unit.Faction));
        actions.AddRange(enemies.Select((e) => new UnitAction(UnitAction.AttackAction, unit.AttackableCells(grid, [e]).Intersect(destinations), e, destinations)));

        IEnumerable<Vector2I> allyCells = unit.SupportableCells(grid, destinations).Where((c) => c != unit.Cell && grid.GetOccupantUnits().TryGetValue(c, out IUnit occupant) && occupant.Faction.AlliedTo(unit.Faction));
        if (allyCells.Any())
        {
            IEnumerable<IUnit> allies = allyCells.Select((c) => grid.GetOccupantUnits()[c]);
            int lowest = allies.Select(static (u) => u.Health).Min();
            actions.AddRange(allies.Where((u) => u.Health == lowest).Select((t) => new UnitAction(UnitAction.SupportAction, unit.SupportableCells(grid, [t.Cell]).Intersect(destinations), t.Cell, destinations)));
        }

        return actions;
    }
}