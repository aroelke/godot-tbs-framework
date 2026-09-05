using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>How to decide which cells to use as sources for measuring distance.</summary>
public enum ApproachTarget
{
    /// <summary>Determine distance from cells containing enemy units.</summary>
    Enemies,
    /// <summary>Determine distance from cells containing allied units.</summary>
    Allies,
    /// <summary>Determine distance from the unit's current cell.</summary>
    CurrentCell
}

/// <summary>Specialized behavior for units that are allowed to move to new cells when performing actions.</summary>
[GlobalClass, Tool]
public partial class MoveBehavior : Behavior
{
    /// <summary>Strategy to use for measuring distance when determining where to move.</summary>
    [Export] public DistanceStrategy DistanceStrategy = DistanceStrategy.ManhattanDistance;

    /// <summary>Strategy to use for determing which cells to measure distance from when determining where to move.</summary>
    [Export] public ApproachTarget ApproachTarget = ApproachTarget.Enemies;

    /// <summary>Whether to prioritize moving closer to target cells or further away from them.</summary>
    [Export] public bool MoveCloser = true;

    public override Vector2I ChooseDestination(UnitData unit, IEnumerable<Vector2I> destinations, IEnumerable<Vector2I> traversable)
    {
        if (!destinations.Any())
        {
            GD.PushWarning("MoveBehavior: no destinations to choose from");
            return unit.Cell;
        }

        Vector2I closest;
        switch (ApproachTarget)
        {
        case ApproachTarget.Enemies:
            closest = unit.Grid.Occupants.Values.Where((u) => !unit.Faction.AlliedTo(u.Faction)).DefaultIfEmpty(unit).MinBy((u) => DistanceStrategy.GetCost(unit.Cell, u.Cell, unit.Grid.AllCells, unit)).Cell;
            if (closest == unit.Cell)
                GD.PushWarning("MoveBehavior: no enemies to move around");
            break;
        case ApproachTarget.Allies:
            closest = unit.Grid.Occupants.Values.Where((u) => u != unit && unit.Faction.AlliedTo(u.Faction)).DefaultIfEmpty(unit).MinBy((u) => DistanceStrategy.GetCost(unit.Cell, u.Cell, unit.Grid.AllCells, unit)).Cell;
            if (closest == unit.Cell)
                GD.PushWarning("MoveBehavior: no allies to move around");
            break;
        case ApproachTarget.CurrentCell:
            closest = unit.Cell;
            break;
        default:
            GD.PushError("MoveBehavior: unknown distance strategy");
            closest = unit.Cell;
            break;
        }
        if (MoveCloser)
            return destinations.MinBy((c) => DistanceStrategy.GetCost(closest, c, unit.Grid.AllCells, unit));
        else
            return destinations.MaxBy((c) => DistanceStrategy.GetCost(closest, c, unit.Grid.AllCells, unit));
    }

    public override IEnumerable<PerformableAction> GetActions(UnitData unit, IEnumerable<UnitAction> available, IEnumerable<Vector2I> traversable)
    {
        IEnumerable<Vector2I> endable = traversable.Where((c) => c == unit.Cell || !unit.Grid.Occupants.ContainsKey(c));
        return available.SelectMany((a) => {
            if (a.RequiresTarget)
                return a.GetValidTargetCells(unit, endable).Select((c) => new PerformableAction(a, unit, c, a.GetSourceCells(unit, c).Intersect(endable)));
            else
            {
                IEnumerable<Vector2I> allowed = endable.Where((c) => a.CanPerform(unit, c));
                return allowed.Any() ? [new PerformableAction(a, unit, GridData.InvalidCell, allowed)] : [];
            }
        });
    }
}