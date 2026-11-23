using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Conditions;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>Represents an action whose actor, destination, and target (if applicable) has been selected.</summary>
/// <param name="Actor">Unit who will perform the action.</param>
/// <param name="Action">Action that will be performed.</param>
/// <param name="Destination">Cell <paramref name="Actor"/> will move to in order to the action.</param>
/// <param name="Target">Cell containing the target of the action.</param>
/// <param name="Traversable">Cells <paramref name="Actor"/> can traverse for pathfinding purposes.</param>
public record struct SpecifiedAction([DisallowNull] UnitData Actor, [DisallowNull] UnitAction Action, Vector2I Destination, Vector2I Target, IEnumerable<Vector2I> Traversable)
{
    /// <summary>Path Actor will follow from its current cell to Destination before performing the action.</summary>
    public readonly Path Path = Actor.Behavior.GetPath(Actor, Destination, Traversable);
}

/// <summary>Represents an algorithm usable by <see cref="AIController"/> to choose its next action to perform from among available ones.</summary>
[GlobalClass, Tool]
public abstract partial class ActionSelector : Resource
{
    /// <summary>
    /// Compute all of the available actions available to a list of units on a grid. Note that, if an action can be performed in multiple cells, only
    /// one of those cells will be chosen unless it is the unit's current cell, in which case a single second option will be chosen as well.
    /// </summary>
    /// <param name="units">Units that could perform actions.</param>
    /// <param name="available">Available actions for units to perform.</param>
    /// <returns>A set of actions that can be performed, including which units can perform them and where they can be performed.</returns>
    public static IEnumerable<SpecifiedAction> GetAvailableActions(IEnumerable<UnitData> units, IEnumerable<UnitAction> available)
    {
        List<SpecifiedAction> actions = [];
        foreach (UnitData unit in units)
        {
            IEnumerable<Vector2I> traversable = unit.Behavior.GetTraversableCells(unit);
            foreach (PerformableAction action in unit.Behavior.GetActions(unit, available, traversable))
            {
                IEnumerable<Vector2I> destinations = action.Sources;

                // If the action allows for retaliation, prioritize cells that the target can't retaliate on
                if (action.Action.RequiresTarget && action.Action.RetaliationAllowed)
                {
                    IEnumerable<Vector2I> safe = destinations.Where((c) => !action.Action.CanPerform(unit.Grid.Occupants[action.Target], action.Target, c));
                    if (safe.Any())
                        destinations = safe;
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

        return actions;
    }

    /// <summary>Mapping of action evaluators to use in <see cref="GetActionValue"/> to the weights to apply to their results.</summary>
    [Export] public Godot.Collections.Dictionary<ActionEvaluator, double> Evaluators = [];

    /// <param name="action">Action to evaluate.</param>
    /// <param name="result">State of the grid that would result from the action, computed by simulating it.</param>
    /// <returns>The value of <paramref name="action"/>.</returns>
    public virtual double GetActionValue(SpecifiedAction action, GridData result) => Evaluators.Select((i) => i.Value*i.Key.Evaluate(new(result, action.Actor.Faction, action.Path, action.Action))).DefaultIfEmpty(0).Sum();

    /// <summary>Choose an action from among available ones along with a unit to perform it, a cell to perform it from, and a target to perform it on (if applicable).</summary>
    /// <param name="units">Units that can perform actions.</param>
    /// <param name="available">Available actions to perform.</param>
    /// <returns>A structure containing all the information about how the chosen action should be performed, or <c>null</c> if no action could be chosen.</returns>
    /// <exception cref="ArgumentException">If <paramref name="units"/> is empty. This should never happen during gameplay as the faction's turn should end before then.</exception>
    public abstract SpecifiedAction? SelectAction(IEnumerable<UnitData> units, IEnumerable<UnitAction> available);

    /// <inheritdoc cref="SelectAction"/>
    /// <param name="grid">Grid containing units that could perform actions.</param>
    /// <param name="units">Filter specifying how to determine which active units on <paramref name="grid"/> to consider for performing an action.</param>
    public SpecifiedAction? SelectAction(GridData grid, UnitFilter units, IEnumerable<UnitAction> available) => SelectAction(units.GetUnits(grid).Where((u) => u.Active), available);

    /// <summary>If <see cref="SelectAction"/> fails to find an action, a unit must still be chosen to perform a default action.</summary>
    /// <param name="units">Collection of available units to act.</param>
    /// <returns>A unit from <paramref name="units"/> to perform a default action.</returns>
    /// <exception cref="ArgumentException">If <paramref name="units"/> is empty. This should never happen during gameplay as the faction's turn should end before then.</exception>
    public virtual UnitData GetDefaultUnit(IEnumerable<UnitData> units)
    {
        if (!units.Any())
            throw new ArgumentException("No units to choose from.");
        return units.MinBy((u) => u.Grid.Occupants.Values.Where((o) => !o.Faction.AlliedTo(u)).Select((e) => u.Cell.ManhattanDistanceTo(e.Cell)).DefaultIfEmpty(0).Min());
    }

    /// <inheritdoc cref="GetDefaultUnit"/>
    /// <param name="grid">Grid containing units to choose from.</param>
    /// <param name="units">Filter specifying which active units are available to act.</param>
    public UnitData GetDefaultUnit(GridData grid, UnitFilter units) => GetDefaultUnit(units.GetUnits(grid).Where((u) => u.Active));

    /// <summary>If <see cref="SelectAction"/> fails to find an action, a cell for the unit chosen by <see cref="GetDefaultUnit"/> must be chosen for it to move to.</summary>
    /// <param name="unit">Unit to move to the destination.</param>
    /// <param name="traversable">Cells <paramref name="unit"/> can move through.</param>
    /// <returns>The destination cell for <paramref name="unit"/> to move to.</returns>
    public virtual Vector2I GetDefaultDestination(UnitData unit, IEnumerable<Vector2I> traversable) => unit.Behavior.ChooseDestination(unit, traversable.Where((c) => c == unit.Cell || !unit.Grid.Occupants.ContainsKey(c)), traversable);

    /// <inheritdoc cref="GetDefaultDestination"/>
    public Vector2I GetDefaultDestination(UnitData unit) => GetDefaultDestination(unit, unit.Behavior.GetTraversableCells(unit));
}