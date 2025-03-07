using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control.Behavior;

/// <summary>A behavior for a <see cref="Unit"/> that does not move.</summary>
[GlobalClass, Tool]
public partial class StandBehavior : UnitBehavior
{
    /// <summary>Whether or not the unit should attack enemies in range.</summary>
    [Export] public bool AttackInRange = false;

    public override IEnumerable<Vector2I> Destinations(Unit unit) => [unit.Cell];

    public override Dictionary<StringName, IEnumerable<Vector2I>> Actions(Unit unit)
    {
        if (AttackInRange)
        {
            Dictionary<StringName, IEnumerable<Vector2I>> actions = [];

            IEnumerable<Vector2I> attackable = unit.AttackableCells();
            IEnumerable<Unit> targets = unit.Grid.Occupants.Where((p) => attackable.Contains(p.Key) && p.Value is Unit target && !unit.Army.Faction.AlliedTo(target)).Select((p) => p.Value).OfType<Unit>();
            if (targets.Any())
                actions["Attack"] = targets.Select((u) => u.Cell);

            return actions;
        }
        else
            return [];
    }
}