using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

[GlobalClass, Tool]
public partial class MoveBehavior : UnitBehavior
{
    public override Vector2I DesiredMoveTarget(Unit unit)
    {
        (IEnumerable<Vector2I> traversable, IEnumerable<Vector2I> attackable, _) = unit.ActionRanges();
        
        IEnumerable<Unit> enemies = unit.Grid.Occupants.Select((p) => p.Value).OfType<Unit>().OrderBy((u) => u.Cell.DistanceTo(unit.Cell));
        foreach (Unit target in enemies.Where((u) => attackable.Contains(u.Cell)))
        {
            if (attackable.Contains(target.Cell))
            {
                IEnumerable<Vector2I> sources = unit.AttackableCells(target.Cell);
                return sources.Where(traversable.Contains).OrderBy((c) => Path.Empty(unit.Grid, traversable).Add(unit.Cell).Add(c).Cost).First();
            }
        }

        return enemies.Any() ? traversable.OrderBy((c) => c.DistanceTo(enemies.First().Cell)).First() : unit.Cell;
    }

    public override (StringName, GridNode) DesiredAction(Unit unit) => ("End", unit);
}