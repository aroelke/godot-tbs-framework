using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Actions;

namespace TbsFramework.Scenes.Level.Control;

public enum DestinationMethod
{
    ClosestToCurrent,
    ClosestToEnemy,
    FurthestFromEnemy,
    ClosestToAlly
}

/// <summary><see cref="Unit"/> behavior that allows the unit to move around the grid to perform actions.</summary>
[Tool]
public partial class MoveBehavior : Behavior
{
    [Export] public DestinationMethod DestinationMethod = DestinationMethod.ClosestToCurrent;

    /// <summary>
    /// Performance option that decides whether or not distances to possible destinations should account for walls and terrain. If <c>true</c>,
    /// destination methods will choose options based on path cost. Otherwise, it will decide based on Manhattan distance (which could cause a
    /// unit to get stuck next to a wall).
    /// </summary>
    [Export] public bool AccountForWalls = true;

    public override IEnumerable<Vector2I> Destinations(UnitData unit) => unit.GetTraversableCells().Where((c) => !unit.Grid.Occupants.ContainsKey(c) || c == unit.Cell);

    public override IEnumerable<ActionInfo> Actions(UnitData unit, IEnumerable<UnitAction> available)
    {
        IEnumerable<Vector2I> destinations = Destinations(unit);
        return available.SelectMany((a) => {
            if (a.RequiresTarget)
                return a.GetValidTargetCells(unit, destinations).Select((c) => new ActionInfo(a, a.GetSourceCells(unit, c), c, destinations));
            else
            {
                IEnumerable<Vector2I> allowed = destinations.Where((c) => a.CanPerform(unit, c));
                return allowed.Any() ? [new ActionInfo(a, allowed, GridData.InvalidCell, destinations)] : [];
            }
        });
    }

    public override Vector2I ChooseDestination(UnitData unit, IEnumerable<Vector2I> choices, IEnumerable<Vector2I> traversable)
    {
        if (!choices.Any())
            throw new ArgumentException("No choices for destination");

        int BestPathCost(Vector2I a, Vector2I b) => AccountForWalls ? Path.Empty(unit.Grid, traversable).Add(a).Add(b).Count : a.ManhattanDistanceTo(b);
        Vector2I DefaultChoice() => choices.MinBy((c) => BestPathCost(unit.Cell, c));

        IEnumerable<Vector2I> units;
        switch (DestinationMethod)
        {
        case DestinationMethod.ClosestToCurrent:
            return DefaultChoice();
        case DestinationMethod.ClosestToEnemy:
            units = unit.Grid.Occupants.Values.Where((u) => !u.Faction.AlliedTo(unit.Faction)).Select((u) => u.Cell);
            return units.Any() ? choices.MinBy((c) => BestPathCost(c, units.MinBy((u) => u.ManhattanDistanceTo(c)))) : DefaultChoice();
        case DestinationMethod.FurthestFromEnemy:
            units = unit.Grid.Occupants.Values.Where((u) => !u.Faction.AlliedTo(unit.Faction)).Select((u) => u.Cell);
            return units.Any() ? choices.MaxBy((c) => BestPathCost(c, units.MaxBy((u) => u.ManhattanDistanceTo(c)))) : DefaultChoice();
        case DestinationMethod.ClosestToAlly:
            units = unit.Grid.Occupants.Values.Where((u) => u.Faction.AlliedTo(unit.Faction)).Select((u) => u.Cell);
            return units.Any() ? choices.MinBy((c) => BestPathCost(c, units.MinBy((u) => u.ManhattanDistanceTo(c)))) : DefaultChoice();
        default:
            throw new ArgumentException($"Unknown destination method {DestinationMethod}");
        }
    }
}