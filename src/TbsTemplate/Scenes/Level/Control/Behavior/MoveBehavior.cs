using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Layers;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control.Behavior;

[GlobalClass, Tool]
public partial class MoveBehavior : UnitBehavior
{
    public override IEnumerable<Vector2I> Destinations(IUnit unit, IGrid grid) => unit.TraversableCells(grid).Where((c) => !grid.GetOccupantUnits().TryGetValue(c, out IUnit occupant) || c == unit.Cell);

    public override Dictionary<StringName, IEnumerable<Vector2I>> Actions(IUnit unit, IGrid grid)
    {
        IEnumerable<Vector2I> destinations = Destinations(unit, grid);
        Dictionary<StringName, IEnumerable<Vector2I>> actions = [];

        foreach (ISpecialActionRegion region in grid.GetSpecialActionRegions())
        {
            IEnumerable<Vector2I> actionable = region.Cells.Intersect(destinations);
            if (actionable.Any())
                actions[region.Action] = actionable;
        }

        IEnumerable<Vector2I> enemies = unit.AttackableCells(grid, destinations).Where((c) => grid.GetOccupantUnits().TryGetValue(c, out IUnit occupant) && !occupant.Faction.AlliedTo(unit.Faction));
        if (enemies.Any())
            actions["Attack"] = enemies;

        IEnumerable<Vector2I> allyCells = unit.SupportableCells(grid, destinations).Where((c) => c != unit.Cell && grid.GetOccupantUnits().TryGetValue(c, out IUnit occupant) && occupant.Faction.AlliedTo(unit.Faction));
        if (allyCells.Any())
        {
            IEnumerable<IUnit> allies = allyCells.Select((c) => grid.GetOccupantUnits()[c]);
            int lowest = allies.Select((u) => u.Health).Min();
            actions["Support"] = allies.Where((u) => u.Health == lowest).Select((u) => u.Cell);
        }

        return actions;
    }
}