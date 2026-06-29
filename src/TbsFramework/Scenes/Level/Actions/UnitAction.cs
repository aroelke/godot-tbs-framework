using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Control;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Scenes.Level.Actions;

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
public partial class UnitAction : Resource
{
    /// <summary>Name of the action for display in a menu.</summary>
    [Export] public StringName Name = "";

    /// <summary>Whether a unit must meet all (<c>true</c>) or any (<c>false</c>) permissions in order to be allowed to perform the action.</summary>
    [Export] public bool IntersectPermission = false;

    /// <summary>Set of permissions used to determine if a unit is allowed to perform the action. If empty, any unit can perform the action.</summary>
    [Export] public Godot.Collections.Array<ActionPermission> PermissionComponents = [];

    /// <summary>Whether a cell must be in all (<c>true</c>) or any (<c>false</c>) domain components in order to be part of this action's domain.</summary>
    [Export] public bool IntersectDomains = false;

    /// <summary>
    /// Components that collectively describe this action's "domain," or the set of cells a unit is able to perform the action in. If empty, a unit can perform
    /// this action from any cell.
    /// </summary>
    [Export] public Godot.Collections.Array<ActionDomain> DomainComponents = [];

    /// <summary>Whether a cell must be in all (<c>true</c>) or any (<c>false<c>) range components in order to be part of this action's range.</summary>
    [Export] public bool IntersectRanges = false;

    /// <summary>
    /// Components that collectively describe this action's "range," or the set of cells a unit is able to perform the action on. If empty, this action is considered
    /// to have no target.
    /// </summary>
    [Export] public Godot.Collections.Array<ActionRange> RangeComponents = [];

    /// <summary>Component describing the results of performing the action.</summary>
    [Export] public ActionExecute ExecuteComponent = null;

    /// <summary>Whether or not this action should always be shown in a unit's action menu regardless of permissions or domain.</summary>
    [Export] public bool AlwaysShow = false;

    /// <summary>Whether or not this action should always be performed using a map animation regardless of game settings.</summary>
    [Export] public bool AnimateOnMap = false;

    /// <summary>
    /// Whether or not this action requires a target.
    /// </summary>
    public bool RequiresTarget => RangeComponents.Count > 0;

    /// <returns>
    /// <c>true</c> if <paramref name="unit"/> is allowed to perform this action and <paramref name="source"/> is part of this action's domain and
    /// <c>false</c> otherwise.
    /// </returns>
    public bool CanPerform(UnitData unit, Vector2I source)
    {
        bool hasPermission = PermissionComponents.Count == 0 || (IntersectPermission ? PermissionComponents.All((c) => c.CanPerform(unit)) : PermissionComponents.Any((c) => c.CanPerform(unit)));
        bool inDomain = DomainComponents.Count == 0 || (IntersectDomains ? DomainComponents.All((c) => c.Contains(source)) : DomainComponents.Any((c) => c.Contains(source)));
        return hasPermission && inDomain;
    }

    /// <returns>
    /// <c>true</c> if <paramref name="unit"/> is allowed to perform this action, <paramref name="source"/> is part of this action's domain, and
    /// <paramref name="target"/> is part of this action's range.
    /// </returns>
    public bool CanPerform(UnitData unit, Vector2I source, Vector2I target)
    {
        bool inRange = RangeComponents.Count == 0 || (IntersectRanges ? RangeComponents.All((c) => c.InRange(unit, source, target)) : RangeComponents.Any((c) => c.InRange(unit, source, target)));
        return CanPerform(unit, source) && inRange;
    }

    /// <returns>The set of cells <paramref name="unit"/> can perform this action on from its cell.</returns>
    public IEnumerable<Vector2I> GetTargetCells(UnitData unit, Vector2I cell)
    {
        if (RangeComponents.Count == 0)
            return [];
        else
        {
            IEnumerable<HashSet<Vector2I>> ranges = RangeComponents.Select((c) => c.GetValidCellsInRange(unit, cell).ToHashSet());
            return ranges.Aggregate(IntersectRanges ? (a, b) => a.Intersect(b).ToHashSet() : (a, b) => a.Union(b).ToHashSet());
        }
    }

    /// <summary>
    /// Get all cells <paramref name="unit"/> can reach to perform this action on from any cell it can move to from its current location regardless
    /// of whether or not those cells contain valid targets.
    /// </summary>
    /// <remarks>It is up to the implementor to determine if cells that are in reach but not valid targets should be included.</remarks>
    public IEnumerable<Vector2I> GetAllTargetCells(UnitData unit)
    {
        if (RangeComponents.Count == 0)
            return [];
        else
        {
            IEnumerable<HashSet<Vector2I>> ranges = RangeComponents.Select((r) => unit.GetTraversableCells().SelectMany((c) => r.GetAllCellsInRange(unit, c)).ToHashSet());
            return ranges.Aggregate(IntersectRanges ? (a, b) => a.Intersect(b).ToHashSet() : (a, b) => a.Union(b).ToHashSet());
        }
    }

    /// <returns>The set of cells within reach of <paramref name="unit"/> after moving to any cell it can traverse that contain valid targets for the action.</returns>
    public IEnumerable<Vector2I> GetValidTargetCells(UnitData unit)
    {
        if (RangeComponents.Count == 0)
            return [];
        else
        {
            IEnumerable<HashSet<Vector2I>> ranges = RangeComponents.Select((r) => unit.GetTraversableCells().SelectMany((c) => r.GetValidCellsInRange(unit, c)).ToHashSet());
            return ranges.Aggregate(IntersectRanges ? (a, b) => a.Intersect(b).ToHashSet() : (a, b) => a.Union(b).ToHashSet());
        }
    }

    /// <returns>The set of cells from which <paramref name="unit"/> can perform this action on <paramref name="target"/>.</returns>
    public IEnumerable<Vector2I> GetSourceCells(UnitData unit, Vector2I target)
    {
        if (RangeComponents.Count == 0)
            return [];
        else
        {
            IEnumerable<IEnumerable<Vector2I>> ranges = RangeComponents.Select((c) => c.GetSources(unit, target));
            return ranges.Aggregate(IntersectRanges ? (a, b) => a.Intersect(b) : (a, b) => a.Union(b)).ToHashSet();
        }
    }

    /// <summary>Compute the result of performing this action without updating the map.</summary>
    /// <param name="unit">Unit performing the action.</param>
    /// <param name="target">Target cell or cell containing the target of this action.</param>
    /// <returns>A data structure representing the result of <paramref name="unit"/> performing this action on cell <paramref name="target"/>.</returns>
    /// <exception cref="ArgumentException">
    /// If <paramref name="unit"/> is not allowed to perform this action, it isn't within this action's domain, or <paramref name="target"/> is not a valid
    /// target cell to perform this action on.
    /// </exception>
    public UnitActionResult Perform(UnitData unit, Vector2I target) => new(ExecuteComponent.Perform(unit, target), unit, target, this);

    /// <summary>Update <paramref name="grid"/> with the results of this action as computed by <see cref="Perform(UnitData, Vector2I)"/>.</summary>
    /// <exception cref="ArgumentException">If <paramref name="result"/>.Result contains invalid data for performing this action.</exception>
    public void UpdateGrid(GridData grid, UnitActionResult result) => ExecuteComponent.UpdateGrid(grid, result.Actor, result.Target, result.Result);

    /// <summary>
    /// Simulate the results of this action, resolving any nondeterminism in some nonrandom way (such as by averaging possible results). Makes no changes
    /// to the state of any existing grid.
    /// </summary>
    /// <param name="unit">Unit that will perform the action.</param>
    /// <param name="source">Cell from which <paramref name="unit"/> will perform the action.  Does not have to be the cell it currently occupies.</param>
    /// <param name="target">Cell being targeted or containing the target of the action.</param>
    /// <returns>A new grid containing the result of the simulation of performing this action.</returns>
    /// <remarks><b>Note</b>: This is intended for use by <see cref="AIController"/> to evaluate actions.</remarks>
    public GridData Simulate(UnitData unit, Vector2I source, Vector2I target) => ExecuteComponent.Simulate(unit, source, target);

    /// <summary>Perform any initial setup of the action's components at the beginning of the level.</summary>
    /// <param name="manager">Node providing access to the scene tree in case any information needs to be extracted from it.</param>
    public void Initialize(LevelManager manager)
    {
        foreach (ActionPermission component in PermissionComponents)
            component.Initialize(manager);
        foreach (ActionDomain component in DomainComponents)
            component.Initialize(manager);
        foreach (ActionRange component in RangeComponents)
            component.Initialize(manager);
        ExecuteComponent.Initialize(manager);
    }
}