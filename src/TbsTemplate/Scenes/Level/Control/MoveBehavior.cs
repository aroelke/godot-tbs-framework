using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

[GlobalClass, Tool]
public partial class MoveBehavior : UnitBehavior
{
    public override IEnumerable<Vector2I> Destinations(Unit unit) => unit.TraversableCells().Where((c) => !unit.Grid.Occupants.ContainsKey(c));

    public override Dictionary<StringName, IEnumerable<Vector2I>> Actions(Unit unit)
    {
        IEnumerable<Vector2I> enemies = unit.AttackableCells(unit.TraversableCells()).Where((c) => unit.Grid.Occupants.ContainsKey(c) && !((unit.Grid.Occupants[c] as Unit)?.Army.Faction.AlliedTo(unit) ?? false));
        if (enemies.Any())
            return new() { {"Attack", enemies} };
        else
            return [];
    }
}