using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.State.Occupants;

namespace TbsTemplate.Scenes.Level.Control.Behavior;

/// <summary>A behavior for a <see cref="Unit"/> that does not move.</summary>
[GlobalClass, Tool]
public partial class StandBehavior : UnitBehavior
{
    /// <summary>Whether or not the unit should attack enemies in range.</summary>
    [Export] public bool AttackInRange = false;

    public override IEnumerable<Vector2I> Destinations(UnitState unit) => [unit.Cell];

    public override Dictionary<StringName, IEnumerable<Vector2I>> Actions(UnitState unit)
    {
        if (AttackInRange)
        {
            Dictionary<StringName, IEnumerable<Vector2I>> actions = [];

            IEnumerable<Vector2I> attackable = unit.AttackableCells();
            IEnumerable<UnitState> targets = unit.Grid.Occupants.Where((p) => attackable.Contains(p.Key) && p.Value is UnitState target && !unit.Faction.AlliedTo(target.Faction)).Select((p) => p.Value).OfType<UnitState>();
            if (targets.Any())
                actions["Attack"] = targets.Select((u) => u.Cell);

            return actions;
        }
        else
            return [];
    }
}