using System;
using System.Collections.Generic;
using Godot;
using TbsFramework.Scenes.Level.Control;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Scenes.Data;

public record struct UnitActionResult(object Result, UnitData Actor, Vector2I Target, FlatUnitAction Action)
{
    public readonly void UpdateGrid(GridData grid) => Action.UpdateGrid(grid, this);
}

[GlobalClass, Tool]
public abstract partial class FlatUnitAction : Resource
{
    [Export] public StringName Name = "";

    [Export] public bool RequiresTarget = true;

    [Export] public bool AlwaysShow = false;

    [Export] public bool AnimateOnMap = false;

    /// <returns>
    /// <c>true</c> if <paramref name="unit"/> can perform this action from cell <paramref name="source"/> on cell <paramref name="target"/>,
    /// and <c>false</c> otherwise. By convention, if the action doesn't require a target then <paramref name="source"/> and
    /// <paramref name="target"/> should be the same.
    /// </returns>
    public abstract bool CanPerform(UnitData unit, Vector2I source, Vector2I target);

    /// <summary>
    /// Get the cells a unit can perform this action on from a specific cell.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="cell"></param>
    /// <returns></returns>
    public abstract IEnumerable<Vector2I> GetTargetCells(UnitData unit, Vector2I cell);

    /// <summary>Get all cells a unit can reach to perform this action on from any cell it can move to from its current location for display on the map.</summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    /// <remarks>It is up to the implementor to determine if cells that are in reach but not valid targets should be included.</remarks>
    public abstract IEnumerable<Vector2I> ShowAllTargetCells(UnitData unit);

    /// <summary>
    /// Get the cells a unit can perform this action on from any cell it can move to from its current location.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    public abstract IEnumerable<Vector2I> GetAllTargetCells(UnitData unit);

    /// <summary>
    /// Compute the result of performing this action.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="target"></param>
    /// <returns>A data structure representing the result of <paramref name="unit"/> performing this action on cell <paramref name="target"/>.</returns>
    /// <exception cref="ArgumentException">If <paramref name="target"/> is not a valid target cell to perform this action on.</exception>
    public abstract UnitActionResult Perform(UnitData unit, Vector2I target);

    /// <summary>
    /// Update a grid with the results of this action as computed by <see cref="Perform(UnitData, Vector2I)"/>.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="result"></param>
    /// <exception cref="ArgumentException">If <paramref name="result"/>.Result contains invalid data for performing this action.</exception>
    public abstract void UpdateGrid(GridData grid, UnitActionResult result);

    /// <summary>
    /// Simulate the results of this action, resolving any nondeterminism in some nonrandom way (such as by averaging possible results). Makes no changes
    /// to the state of any existing grid.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns>A new grid containing the result of the simulation of performing this action.</returns>
    /// <remarks><b>Note</b>: This is intended for use by <see cref="AIController"/> to evaluate actions.</remarks>
    public abstract GridData Simulate(UnitData unit, Vector2I source, Vector2I target);

    public virtual void Initialize(LevelManager manager) {}

    public virtual void Reset(LevelManager manager) => Initialize(manager);
}