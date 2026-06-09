using System.Collections.Generic;
using Godot;
using TbsFramework.Scenes.Level.Control;

namespace TbsFramework.Scenes.Data;

public interface IUnitAction<T>
{
    /// <returns><c>true</c> if <paramref name="unit"/> is allowed to perform this action, and <c>false</c> otherwise</returns>
    public bool CanPerform(UnitData unit);

    /// <returns><c>true</c> if <paramref name="unit"/> can perform this action in <paramref name="cell"/>, and <c>false</c> otherwise.</returns>
    public bool CanPerform(UnitData unit, Vector2I cell);

    /// <summary>
    /// Get the cells a unit can perform this action on from a specific cell.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="cell"></param>
    /// <returns></returns>
    public IEnumerable<Vector2I> GetTargetCells(UnitData unit, Vector2I cell);

    /// <summary>
    /// Get the cells a unit can perform this action on from any cell it can move to from its current location.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    public IEnumerable<Vector2I> GetAllTargetCells(UnitData unit);

    /// <summary>
    /// Compute the result of performing this action.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="target"></param>
    /// <returns>A data structure representing the result of <paramref name="unit"/> performing this action on cell <paramref name="target"/>.</returns>
    public T Perform(UnitData unit, Vector2I target);

    /// <summary>
    /// Update a grid with the results of this action as computed by <see cref="Perform(UnitData, Vector2I)"/>.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="results"></param>
    public void UpdateGrid(GridData grid, T results);

    /// <summary>
    /// Simulate the results of this action, resolving any nondeterminism in some nonrandom way (such as by averaging possible results). Makes no changes
    /// to the state of any existing grid.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns>A new grid containing the result of the simulation of performing this action.</returns>
    /// <remarks><b>Note</b>: This is intended for use by <see cref="AIController"/> to evaluate actions.</remarks>
    public GridData Simulate(UnitData unit, Vector2I source, Vector2I target);
}