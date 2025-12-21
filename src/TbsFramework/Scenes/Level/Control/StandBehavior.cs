using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary><see cref="Unit"/> behavior that prevents a unit from moving and can optionally prevent actions as well.</summary>
[Tool]
public partial class StandBehavior : Behavior
{
    /// <summary>Whether or not the unit should attack enemies in range.</summary>
    [Export] public bool AttackInRange = false;

    /// <summary>Whether or not the unit should support allies in range.</summary>
    [Export] public bool SupportInRange = true;

    public override IEnumerable<Vector2I> Destinations(IUnit unit, IGrid grid) => [unit.Cell];

    public override IEnumerable<UnitAction> Actions(IUnit unit, IGrid grid)
    {
        List<UnitAction> actions = [];

        actions.AddRange(grid.GetSpecialActionRegions().Where((r) => r.CanPerform(unit, unit.Cell)).Select((r) => new UnitAction(r.Action, [unit.Cell], unit.Cell, [unit.Cell])));
        if (AttackInRange)
        {
            IEnumerable<Vector2I> attackable = unit.AttackableCells(grid, [unit.Cell]);
            IEnumerable<IUnit> targets = grid.GetOccupantUnits().Where((e) => attackable.Contains(e.Key) && !unit.Faction.AlliedTo(e.Value.Faction)).Select(static (p) => p.Value);
            actions.AddRange(targets.Select((t) => new UnitAction(UnitAction.AttackAction, [unit.Cell], t.Cell, [unit.Cell])));
        }
        if (SupportInRange)
        {
            IEnumerable<Vector2I> supportable = unit.SupportableCells(grid, [unit.Cell]);
            IEnumerable<IUnit> targets = grid.GetOccupantUnits().Where((e) => supportable.Contains(e.Key) && unit.Faction.AlliedTo(e.Value.Faction)).Select(static (p) => p.Value);
            if (targets.Any())
            {
                int lowest = targets.Select(static (u) => u.Health).Min();
                actions.AddRange(targets.Where((t) => t.Health == lowest).Select((t) => new UnitAction(UnitAction.SupportAction, [unit.Cell], t.Cell, [unit.Cell])));
            }
        }

        return actions;
    }
}