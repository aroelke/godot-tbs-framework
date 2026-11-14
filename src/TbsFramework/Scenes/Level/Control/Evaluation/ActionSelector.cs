using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>Represents an action whose actor, destination, and target (if applicable) has been selected.</summary>
/// <param name="Actor">Unit who will perform the action.</param>
/// <param name="Action">Action that will be performed.</param>
/// <param name="Destination">Cell <paramref name="Actor"/> will move to in order to the action.</param>
/// <param name="Target">Cell containing the target of the action.</param>
/// <param name="Traversable">Cells <paramref name="Actor"/> can traverse for pathfinding purposes.</param>
public record struct ResolvedAction(UnitData Actor, UnitAction Action, Vector2I Destination, Vector2I Target, IEnumerable<Vector2I> Traversable)
{
    /// <summary>Path Actor will follow from its current cell to Destination before performing the action.</summary>
    public readonly Path Path = Path.Empty(Traversable, Actor.CellCost).Add(Actor.Cell).Add(Destination);

    /// <summary>Resulting grid after performing the action.</summary>
    public GridData Result;
}

/// <summary>Represents an algorithm usable by <see cref="AIController"/> to choose its next action to perform from among available ones.</summary>
public abstract partial class ActionSelector : Resource
{
    /// <summary>
    /// Compute all of the available actions available to units of a specific faction on a grid. Note that, if an action can be performed in multiple cells, only
    /// one of those cells will be chosen unless it is the unit's current cell, in which case a single second option will be chosen as well.
    /// </summary>
    /// <param name="grid">Grid actions could be performed on.</param>
    /// <param name="faction">Faction containing units to determine actions for.</param>
    /// <param name="available">Available actions for units to perform.</param>
    /// <returns>A set of actions that can be performed, including which units can perform them and where they can be performed.</returns>
    public static IEnumerable<ResolvedAction> GetAvailableActions(GridData grid, Faction faction, IEnumerable<UnitAction> available)
    {
        List<ResolvedAction> actions = [];
        foreach ((_, UnitData unit) in grid.Occupants)
        {
            if (unit.Faction == faction && unit.Active)
            {
                IEnumerable<Vector2I> traversable = unit.Behavior.GetTraversableCells(unit);
                foreach (PerformableAction action in unit.Behavior.GetActions(unit, available, traversable))
                {
                    IEnumerable<Vector2I> destinations;

                    destinations = action.Sources;
                    if (action.Action.RequiresTarget)
                    {
                        // If the action allows for retaliation, prioritize cells that the target can't retaliate on
                        if (action.Action.RequiresTarget && action.Action.RetaliationAllowed)
                        {
                            IEnumerable<Vector2I> safe = destinations.Where((c) => !action.Action.CanPerform(unit.Grid.Occupants[action.Target], action.Target, c));
                            if (safe.Any())
                                destinations = safe;
                        }
                    }

                    // Prioritize the destination closest to the actor's current cell, but if that cell is the actor's current cell then
                    // also try the next-best one in case moving another unit to that cell afterward is overall better. Always use
                    // Manhattan distance here rather than the behavior's destination preference for performance reasons and also because
                    // there are paths to all choices anyway, so it doesn't matter how one is chosen
                    Vector2I best = destinations.MinBy((c) => c.ManhattanDistanceTo(unit.Cell));
                    actions.Add(new(unit, action.Action, best, action.Target, traversable));
                    if (best == unit.Cell && destinations.Count() > 1)
                        actions.Add(new(unit, action.Action, destinations.Where((c) => c != best).MinBy((c) => c.ManhattanDistanceTo(unit.Cell)), action.Target, traversable));
                }
            }
        }

        return actions;
    }

    public abstract ResolvedAction SelectAction(GridData grid, Faction faction, IEnumerable<UnitAction> available);
}