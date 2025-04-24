using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control.Behavior;

/// <summary>A behavior for a <see cref="Unit"/> that does not move.</summary>
[GlobalClass, Tool]
public partial class StandBehavior : UnitBehavior
{
    /// <summary>Whether or not the unit should attack enemies in range.</summary>
    [Export] public bool AttackInRange = false;

    /// <summary>Whether or not the unit should support allies in range.</summary>
    [Export] public bool SupportInRange = true;

    public override IEnumerable<Vector2I> Destinations(IUnit unit, IGrid grid) => [unit.Cell];

    public override Dictionary<StringName, IEnumerable<Vector2I>> Actions(IUnit unit, IGrid grid)
    {
        Dictionary<StringName, IEnumerable<Vector2I>> actions = [];

        if (AttackInRange)
        {
            IEnumerable<Vector2I> attackable = unit.AttackableCells(grid, [unit.Cell]);
            IEnumerable<IUnit> targets = grid.GetOccupantUnits().Where((e) => attackable.Contains(e.Key) && !unit.Faction.AlliedTo(e.Value.Faction)).Select((p) => p.Value);
            if (targets.Any())
                actions["Attack"] = targets.Select((u) => u.Cell);
        }
        if (SupportInRange)
        {
            IEnumerable<Vector2I> supportable = unit.SupportableCells(grid, [unit.Cell]);
            IEnumerable<IUnit> targets = grid.GetOccupantUnits().Where((e) => supportable.Contains(e.Key) && unit.Faction.AlliedTo(e.Value.Faction)).Select((p) => p.Value);
            if (targets.Any())
                actions["Support"] = targets.Select((u) => u.Cell);
        }

        return actions;
    }
}