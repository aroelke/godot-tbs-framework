using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Scenes.Level.Actions;

/// <summary>
/// Describes the cells a unit can perform this action on, meaning the cell could be a valid target for the action or it could contain a
/// unit that is a valid target. Some methods present possible cells within the action's range that could be or contain valid targets regardless
/// of whether or not they are valid and others filter those results to contain only cells that are or contain valid targets.
/// </summary>
[GlobalClass]
public abstract partial class ActionRange : Resource
{
    /// <returns>
    /// The set of cells <paramref name="unit"/> could perform an action on from <paramref name="cell"/> within its movement range, regardless of
    /// whether or not those cells are or contain valid targets.
    /// </returns>
    public abstract IEnumerable<Vector2I> GetAllCellsInRange(UnitData unit, Vector2I cell);

    /// <returns>
    /// The set of cells <paramref name="unit"/> could perform an action on from <paramref name="cell"/> that are or contain only
    /// valid targets.
    /// </returns>
    public abstract IEnumerable<Vector2I> GetValidCellsInRange(UnitData unit, Vector2I cell);

    /// <returns>
    /// <c>true</c> if <paramref name="target"/> is or contains a valid target for <paramref name="unit"/> to perform an action on from
    /// <paramref name="source"/>, and <c>false</c> otherwise.
    /// </returns>
    public virtual bool InRange(UnitData unit, Vector2I source, Vector2I target) => GetValidCellsInRange(unit, source).Contains(target);

    /// <returns>
    /// The set of cells from which <paramref name="unit"/> could perform an action on <paramref name="target"/> regardless of whether or not it
    /// could actually reach those cells.
    /// </returns>
    public abstract IEnumerable<Vector2I> GetSources(UnitData unit, Vector2I target);

    /// <summary>Perform any initial setup at the beginning of the level.</summary>
    /// <param name="manager">Node providing access to the scene tree in case any information needs to be extracted from it.</param>
    public virtual void Initialize(LevelManager manager) {}
}