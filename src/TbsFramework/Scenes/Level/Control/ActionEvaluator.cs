using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Structure containing information about a simulated action that can be used for evaluation.</summary>
/// <param name="Grid">State of the grid that would result from performing the action.</param>
/// <param name="Faction">Faction who performed the action.</param>
/// <param name="Traversed">Path that the unit who performed the action traversed.</param>
/// <param name="Action">Action that was simulated.</param>
public record struct SimulatedAction(GridData Grid, Faction Faction, Path Traversed, UnitAction Action)
{
    /// <returns>The unit that performed the action.</returns>
    public readonly UnitData GetActor() => Grid.Occupants[Traversed[^1]];
}

/// <summary>
/// <see cref="AIController"/> component that can be used to produce a value for the result of simulating a potential action.
/// Can be combined with the results of other instances to create an overall value.
/// </summary>
[GlobalClass, Tool]
public abstract partial class ActionEvaluator : Resource
{
    /// <returns>
    /// The value computed for <paramref name="action"/>. Should be between 0 and 1, inclusive, where 0 is the worst possible
    /// value and 1 is the best.
    /// </returns>
    public abstract double Evaluate(SimulatedAction action);
}