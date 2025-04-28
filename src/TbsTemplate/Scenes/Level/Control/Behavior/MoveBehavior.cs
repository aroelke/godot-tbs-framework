using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control.Behavior;

[GlobalClass, Tool]
public partial class MoveBehavior : UnitBehavior
{
    public override IEnumerable<Vector2I> Destinations(IUnit unit, IGrid grid) => unit.TraversableCells(grid).Where((c) => !grid.GetOccupantUnits().TryGetValue(c, out IUnit occupant) || c == unit.Cell);

    public override Dictionary<StringName, IEnumerable<Vector2I>> Actions(IUnit unit, IGrid grid)
    {
        Dictionary<StringName, IEnumerable<Vector2I>> actions = [];

        IEnumerable<Vector2I> enemies = unit.AttackableCells(grid, unit.TraversableCells(grid)).Where((c) => grid.GetOccupantUnits().TryGetValue(c, out IUnit occupant) && !occupant.Faction.AlliedTo(unit.Faction));
        if (enemies.Any())
            actions["Attack"] = enemies;
        
        IEnumerable<Vector2I> allies = unit.SupportableCells(grid, unit.TraversableCells(grid)).Where((c) => c != unit.Cell && grid.GetOccupantUnits().TryGetValue(c, out IUnit occupant) && occupant.Faction.AlliedTo(unit.Faction));
        if (allies.Any())
            actions["Support"] = allies;

        return actions;
    }
}