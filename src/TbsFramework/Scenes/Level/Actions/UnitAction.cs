using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Control;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Scenes.Level.Actions;

public record struct UnitActionResult(object Result, UnitData Actor, Vector2I Target, UnitAction Action)
{
    public readonly void UpdateGrid(GridData grid) => Action.UpdateGrid(grid, this);
}

[GlobalClass, Tool]
public partial class UnitAction : Resource
{
    [Export] public StringName Name = "";

    [Export] public bool IntersectPermission = false;

    [Export] public Godot.Collections.Array<ActionPermission> PermissionComponents = [];

    [Export] public bool IntersectDomains = false;

    [Export] public Godot.Collections.Array<ActionDomain> DomainComponents = [];

    [Export] public bool IntersectRanges = false;

    [Export] public Godot.Collections.Array<ActionRange> RangeComponents = [];

    [Export] public ActionExecute ExecuteComponent = null;

    [Export] public bool AlwaysShow = false;

    [Export] public bool AnimateOnMap = false;

    public bool RequiresTarget => RangeComponents.Count > 0;

    public bool CanPerform(UnitData unit, Vector2I source)
    {
        bool hasPermission = PermissionComponents.Count == 0 || (IntersectPermission ? PermissionComponents.All((c) => c.CanPerform(unit)) : PermissionComponents.Any((c) => c.CanPerform(unit)));
        bool inDomain = DomainComponents.Count == 0 || (IntersectDomains ? DomainComponents.All((c) => c.Contains(source)) : DomainComponents.Any((c) => c.Contains(source)));
        return hasPermission && inDomain;
    }

    public bool CanPerform(UnitData unit, Vector2I source, Vector2I target)
    {
        bool inRange = RangeComponents.Count == 0 || (IntersectRanges ? RangeComponents.All((c) => c.InRange(unit, source, target)) : RangeComponents.Any((c) => c.InRange(unit, source, target)));
        return CanPerform(unit, source) && inRange;
    }

    /// <summary>
    /// Get the cells a unit can perform this action on from a specific cell.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="cell"></param>
    /// <returns></returns>
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

    /// <summary>Get all cells a unit can reach to perform this action on from any cell it can move to from its current location for display on the map.</summary>
    /// <param name="unit"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Get the cells a unit can perform this action on from any cell it can move to from its current location.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Compute the result of performing this action.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="target"></param>
    /// <returns>A data structure representing the result of <paramref name="unit"/> performing this action on cell <paramref name="target"/>.</returns>
    /// <exception cref="ArgumentException">If <paramref name="target"/> is not a valid target cell to perform this action on.</exception>
    public UnitActionResult Perform(UnitData unit, Vector2I target) => new(ExecuteComponent.Perform(unit, target), unit, target, this);

    /// <summary>
    /// Update a grid with the results of this action as computed by <see cref="Perform(UnitData, Vector2I)"/>.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="result"></param>
    /// <exception cref="ArgumentException">If <paramref name="result"/>.Result contains invalid data for performing this action.</exception>
    public void UpdateGrid(GridData grid, UnitActionResult result) => ExecuteComponent.UpdateGrid(grid, result.Actor, result.Target, result.Result);

    /// <summary>
    /// Simulate the results of this action, resolving any nondeterminism in some nonrandom way (such as by averaging possible results). Makes no changes
    /// to the state of any existing grid.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns>A new grid containing the result of the simulation of performing this action.</returns>
    /// <remarks><b>Note</b>: This is intended for use by <see cref="AIController"/> to evaluate actions.</remarks>
    public GridData Simulate(UnitData unit, Vector2I source, Vector2I target) => ExecuteComponent.Simulate(unit, source, target);

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