using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Level.Layers;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Control;

/// <summary><see cref="Unit"/> behavior that allows the unit to move around the grid to perform actions.</summary>
[Tool]
public partial class MoveBehavior : Behavior
{
    public override IEnumerable<Vector2I> Destinations(UnitData unit) => unit.GetTraversableCells().Where((c) => !unit.Grid.Occupants.ContainsKey(c) || c == unit.Cell);

    public override IEnumerable<UnitAction> Actions(UnitData unit)
    {
        IEnumerable<Vector2I> destinations = Destinations(unit);
        List<UnitAction> actions = [];

        foreach (SpecialActionRegionData region in unit.Grid.SpecialActionRegions)
        {
            IEnumerable<Vector2I> actionable = region.Cells.Intersect(destinations).Where((c) => region.CanPerform(unit));
            actions.AddRange(actionable.Select((a) => new UnitAction(region.Action, [a], a, destinations)));
        }

        IEnumerable<Vector2I> enemies = destinations.SelectMany((c) => unit.GetAttackableCells(c)).ToHashSet().Where((c) => unit.Grid.Occupants.TryGetValue(c, out GridObjectData occupant) && occupant is UnitData u && !u.Faction.AlliedTo(unit.Faction));
        actions.AddRange(enemies.Select((e) => new UnitAction(UnitAction.AttackAction, unit.GetAttackableCells(e).Intersect(destinations), e, destinations)));

        IEnumerable<Vector2I> allyCells = destinations.SelectMany((c) => unit.GetSupportableCells(c)).ToHashSet().Where((c) => c != unit.Cell && unit.Grid.Occupants.TryGetValue(c, out GridObjectData occupant) && occupant is UnitData u && u.Faction.AlliedTo(unit.Faction));
        if (allyCells.Any())
        {
            IEnumerable<UnitData> allies = allyCells.Select((c) => unit.Grid.Occupants[c]).OfType<UnitData>();
            double lowest = allies.Min(static (u) => u.Health);
            actions.AddRange(allies.Where((u) => u.Health == lowest).Select((t) => new UnitAction(UnitAction.SupportAction, unit.GetSupportableCells(t.Cell).Intersect(destinations), t.Cell, destinations)));
        }

        return actions;
    }
}