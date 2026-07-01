using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Control;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Scenes.Level.Actions;

/// <summary>
/// Describes the results of performing an action. Computing those results and updating the grid with them are different to allow for display before
/// the grid is updated.
/// </summary>
[GlobalClass]
public abstract partial class ActionExecute : Resource
{
    /// <summary>Compute the result of performing this action. Does not update the grid.</summary>
    /// <param name="unit">Unit performing the action.</param>
    /// <param name="target">Cell that is or contains the target of the action.</param>
    /// <returns>An object representing the result of <paramref name="unit"/> performing this action on cell <paramref name="target"/>.</returns>
    public abstract object Perform(UnitData unit, Vector2I target);

    /// <summary>Update a grid with the results of this action as computed by <see cref="Perform(UnitData, Vector2I)"/>.</summary>
    /// <param name="grid">Grid to update.</param>
    /// <param name="actor">Unit that performed the action.</param>
    /// <param name="target">Cell that is or contains the target of the action</param>
    /// <param name="result">Object representing the results of the action.</param>
    public abstract void UpdateGrid(GridData grid, UnitData actor, Vector2I target, object result);

    /// <summary>
    /// Simulate the results of this action, resolving any nondeterminism in some nonrandom way (such as by averaging possible results). Makes no changes
    /// to the state of any existing grid.
    /// </summary>
    /// <param name="unit">Unit that could perform the action.</param>
    /// <param name="source">Cell from which <paramref name="unit"/> could perform the action.</param>
    /// <param name="target">Cell that is or contains the potential target of the action.</param>
    /// <returns>A new grid containing the result of the simulation of performing this action.</returns>
    /// <remarks><b>Note</b>: This is intended for use by <see cref="AIController"/> to evaluate actions.</remarks>
    public abstract GridData Simulate(UnitData unit, Vector2I source, Vector2I target);

    /// <summary>Perform any initial setup at the beginning of the level.</summary>
    /// <param name="manager">Node providing access to the scene tree in case any information needs to be extracted from it.</param>
    public virtual void Initialize(LevelManager manager) {}
}