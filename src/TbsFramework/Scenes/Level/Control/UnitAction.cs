using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Represents the computed result of performing an action before actually applying it to the map.</summary>
/// <param name="Result">Object representing the result of the action.</param>
/// <param name="Actor">Unit performing the action.</param>
/// <param name="Target">Cell on which the action will be performed or containing the unit on which it will be performed.</param>
/// <param name="Action">Action to be performed.</param>
public record struct UnitActionResult(object Result, UnitData Actor, Vector2I Target, UnitAction Action)
{
    /// <summary>Convenience method for updating the map based on the results of performing <see cref="Action"/>.</summary>
    /// <param name="grid">Grid to update.</param>
    public readonly void UpdateGrid(GridData grid) => Action.UpdateGrid(grid, this);
}

/// <summary>
/// Represents an action that a unit can perform. Provides information on whether a given unit can perform the action, in which cells,
/// and on which cells and computes the results of performing the action.
/// </summary>
[GlobalClass, Tool]
public abstract partial class UnitAction : Resource
{
    /// <summary>Name of the action for display in a menu.</summary>
    [Export] public StringName Name = "";

    /// <summary>Whether or not this action should always be performed using a map animation regardless of game settings.</summary>
    [Export] public bool AnimateOnMap = false;

    /// <summary>If <c>false</c>, <see cref="AIController"/>-controlled armies will never select this action, even if it's the only one available.</summary>
    [Export, ExportGroup("AI Control")] public bool AIAllowed = true;

    /// <summary>
    /// If a unit performs this action on another unit in an opposing faction, the second unit can retaliate in some way.  Used for helping
    /// <see cref="AIController"/> with positioning.
    /// </summary>
    [Export, ExportGroup("AI Control")] public bool RetaliationAllowed = false;

    /// <summary>Whether or not this action requires a target.</summary>
    public abstract bool RequiresTarget { get; }

    /// <returns>
    /// <c>true</c> if <paramref name="unit"/> is allowed to perform this action and <paramref name="source"/> is part of this action's domain and
    /// <c>false</c> otherwise.
    /// </returns>
    public abstract bool CanPerform(UnitData unit, Vector2I source);

    /// <returns>
    /// <c>true</c> if <paramref name="unit"/> is allowed to perform this action, <paramref name="source"/> is part of this action's domain, and
    /// <paramref name="target"/> is part of this action's range.
    /// </returns>
    public abstract bool CanPerform(UnitData unit, Vector2I source, Vector2I target);

    /// <returns>The set of cells <paramref name="unit"/> can perform this action on from its cell.</returns>
    public abstract IEnumerable<Vector2I> GetTargetCells(UnitData unit, Vector2I cell);

    /// <summary>
    /// Get all cells <paramref name="unit"/> can reach to perform this action on from any cell within <paramref name="traversable"/> regardless
    /// of whether or not those cells contain valid targets.
    /// </summary>
    /// <remarks>It is up to the implementor to determine if cells that are in reach but not valid targets should be included.</remarks>
    public abstract IEnumerable<Vector2I> GetAllTargetCells(UnitData unit, IEnumerable<Vector2I> traversable);

    /// <returns>The set of cells within <paramref name="traversable"/> that <paramref name="unit"/> can perform this action from.</returns>
    public virtual IEnumerable<Vector2I> GetSourceCells(UnitData unit, IEnumerable<Vector2I> traversable) => traversable.Where((c) => !unit.Grid.Occupants.TryGetValue(c, out UnitData occupant) || occupant == unit);

    /// <returns>The set of cells <paramref name="unit"/> can perform this action from within the cells it can traverse.</returns>
    public virtual IEnumerable<Vector2I> GetSourceCells(UnitData unit) => GetSourceCells(unit, unit.GetTraversableCells());

    /// <summary>
    /// Get all cells <paramref name="unit"/> can reach to perform this action on from any cell it can traverse regardless of whether or not those
    /// cells contain valid targets.
    /// </summary>
    public virtual IEnumerable<Vector2I> GetAllTargetCells(UnitData unit) => GetAllTargetCells(unit, GetSourceCells(unit));

    /// <returns>The set of cells that contain valid targets for <paramref name="unit"/> from any cell within <paramref name="traversable"/>.</returns>
    public abstract IEnumerable<Vector2I> GetValidTargetCells(UnitData unit, IEnumerable<Vector2I> traversable);

    /// <returns>The set of cells within reach of <paramref name="unit"/> after moving to any cell it can traverse that contain valid targets for the action.</returns>
    public virtual IEnumerable<Vector2I> GetValidTargetCells(UnitData unit) => GetValidTargetCells(unit, GetSourceCells(unit));

    /// <returns>The set of cells from which <paramref name="unit"/> can perform this action on <paramref name="target"/>.</returns>
    public abstract IEnumerable<Vector2I> GetSourceCells(UnitData unit, Vector2I target);

    /// <summary>Compute the result of performing this action without updating the map.</summary>
    /// <param name="unit">Unit performing the action.</param>
    /// <param name="target">
    /// Target cell or cell containing the target of this action. If <see cref="RequiresTarget"/> is <c>false</c>, use <see cref="GridData.InvalidCell"/>
    /// instead.
    /// </param>
    /// <returns>A data structure representing the result of <paramref name="unit"/> performing this action on cell <paramref name="target"/>.</returns>
    /// <exception cref="ArgumentException">
    /// If <paramref name="unit"/> is not allowed to perform this action, it isn't within this action's domain, or <paramref name="target"/> is not a valid
    /// target cell to perform this action on.
    /// </exception>
    public abstract UnitActionResult Perform(UnitData unit, Vector2I target);

    /// <summary>Update <paramref name="grid"/> with the results of this action as computed by <see cref="Perform(UnitData, Vector2I)"/>.</summary>
    /// <exception cref="ArgumentException">If <paramref name="result"/>.Result contains invalid data for performing this action.</exception>
    public abstract void UpdateGrid(GridData grid, UnitActionResult result);

    /// <summary>
    /// Simulate the results of this action, resolving any nondeterminism in some nonrandom way (such as by averaging possible results). Makes no changes
    /// to the state of any existing grid.
    /// </summary>
    /// <param name="unit">Unit that will perform the action.</param>
    /// <param name="source">Cell from which <paramref name="unit"/> will perform the action.  Does not have to be the cell it currently occupies.</param>
    /// <param name="target">Cell being targeted or containing the target of the action.</param>
    /// <returns>A new grid containing the result of the simulation of performing this action.</returns>
    /// <remarks><b>Note</b>: This is intended for use by <see cref="AIController"/> to evaluate actions.</remarks>
    public abstract GridData Simulate(UnitData unit, Vector2I source, Vector2I target);
}