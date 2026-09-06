using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>Specialized behavior for units that aren't allowed to move.</summary>
[GlobalClass, Tool]
public partial class StandBehavior : Behavior
{
    public override IEnumerable<Vector2I> GetTraversableCells(UnitData unit) => [unit.Cell];

    public override Vector2I ChooseDestination(UnitData unit, IEnumerable<Vector2I> destinations, IEnumerable<Vector2I> traversable)
    {
        if (!destinations.Contains(unit.Cell))
            throw new ArgumentException("unit cell is not a destination option");
        return unit.Cell;
    }

    public override Path GetPath(UnitData unit, Vector2I start, Vector2I destination, IEnumerable<Vector2I> traversable)
    {
        GD.PushWarning("StandBehavior: computing path for immobile unit");
        return base.GetPath(unit, start, destination, traversable);
    }

    public override IEnumerable<PerformableAction> GetActions(UnitData unit, IEnumerable<UnitAction> available, IEnumerable<Vector2I> traversable)
    {
        if (!traversable.Contains(unit.Cell))
            throw new ArgumentException("unit cell is not a destination option");
        IEnumerable<UnitAction> allowed = available.Where((a) => a.CanPerform(unit, unit.Cell));

        IEnumerable<PerformableAction> GetTargetedActions(bool allied)
        {
            foreach (UnitAction action in allowed)
            {
                IEnumerable<Vector2I> targets = action.GetTargetCells(unit, unit.Cell).Where((c) => unit.Grid.Occupants.TryGetValue(c, out UnitData occupant) && unit.Faction.AlliedTo(occupant.Faction) == allied);
                foreach (Vector2I target in targets)
                    yield return new(action, unit, target, [unit.Cell]);
            }
        }

        List<PerformableAction> actions = [.. allowed.Where((a) => !a.RequiresTarget).Select((a) => new PerformableAction(a, unit, GridData.InvalidCell, [unit.Cell]))];
        if (AllowAttack)
            actions.AddRange(GetTargetedActions(false));
        if (AllowSupport)
            actions.AddRange(GetTargetedActions(true));
        return actions;
    }
}