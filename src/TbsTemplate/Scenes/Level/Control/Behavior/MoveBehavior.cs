using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.State.Occupants;

namespace TbsTemplate.Scenes.Level.Control.Behavior;

[GlobalClass, Tool]
public partial class MoveBehavior : UnitBehavior
{
    public override IEnumerable<Vector2I> Destinations(UnitState unit) => unit.TraversableCells().Where((c) => !unit.Grid.Occupants.ContainsKey(c) || unit.Grid.Occupants[c] == unit);

    public override Dictionary<StringName, IEnumerable<Vector2I>> Actions(UnitState unit)
    {
        IEnumerable<Vector2I> enemies = unit.AttackableCells(unit.TraversableCells()).Where((c) => unit.Grid.Occupants.ContainsKey(c) && !((unit.Grid.Occupants[c] as UnitState)?.Faction.AlliedTo(unit.Faction) ?? false));
        if (enemies.Any())
            return new() { {"Attack", enemies} };
        else
            return [];
    }
}