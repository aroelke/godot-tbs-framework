using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Level.Map;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Control;

/// <summary><see cref="Unit"/> behavior that prevents a unit from moving and can optionally prevent actions as well.</summary>
[Tool]
public partial class StandBehavior : Behavior
{
    /// <summary>Whether or not the unit should attack enemies in range.</summary>
    [Export] public bool AttackInRange = false;

    /// <summary>Whether or not the unit should support allies in range.</summary>
    [Export] public bool SupportInRange = true;

    public override IEnumerable<Vector2I> Destinations(UnitData unit) => [unit.Cell];

    public override IEnumerable<UnitAction> Actions(UnitData unit)
    {
        List<UnitAction> actions = [];

        actions.AddRange(unit.Grid.SpecialActionRegions.Where((r) => r.CanPerform(unit)).Select((r) => new UnitAction(r.Action, [unit.Cell], unit.Cell, [unit.Cell])));
        if (AttackInRange)
        {
            IEnumerable<Vector2I> attackable = unit.GetAttackableCells();
            IEnumerable<UnitData> targets = unit.Grid.Occupants.Where((e) => attackable.Contains(e.Key) && e.Value is UnitData u && !unit.Faction.AlliedTo(u.Faction)).Select(static (p) => p.Value).OfType<UnitData>();
            actions.AddRange(targets.Select((t) => new UnitAction(UnitAction.AttackAction, [unit.Cell], t.Cell, [unit.Cell])));
        }
        if (SupportInRange)
        {
            IEnumerable<Vector2I> supportable = unit.GetSupportableCells();
            IEnumerable<UnitData> targets = unit.Grid.Occupants.Where((e) => supportable.Contains(e.Key) && e.Value is UnitData u && unit.Faction.AlliedTo(u.Faction)).Select(static (p) => p.Value).OfType<UnitData>();
            if (targets.Any())
            {
                double lowest = targets.Min(static (u) => u.Health);
                actions.AddRange(targets.Where((t) => t.Health == lowest).Select((t) => new UnitAction(UnitAction.SupportAction, [unit.Cell], t.Cell, [unit.Cell])));
            }
        }

        return actions;
    }
}