using System;
using Godot;

namespace TbsFramework.Scenes.Data;

/// <summary>
/// Base class for unit stats that defines basic information unit stats need to be able to provide. This should not contain
/// transient information about a unit's status, like current health, as it is not duplicated or updated during AI action
/// evaluation.
/// </summary>
[GlobalClass, Tool]
public abstract partial class AbstractStats : Resource
{
    /// <summary>Indicates that one or more values of this set of stats have changed.</summary>
    public event Action<AbstractStats> ValuesChanged;

    /// <summary>Provides the max amount of health a unit with this set of stats can have.</summary>
    public abstract double MaxHealth { get; }

    /// <summary>Indicates the maximum total cost of a path a unit with this set of stats can take when moving.</summary>
    public abstract int MoveDistance { get; }

    /// <returns>
    /// An additional modifier on the cost to move onto a cell with <paramref name="terrain"/> for a unit with this set of
    /// stats.
    /// </returns>
    public abstract int GetTerrainCostModifier(Terrain terrain);

    /// <summary>Signal that one or more of the values of this set of stats have changed.</summary>
    public void SignalValuesChanged() { if (ValuesChanged is not null) ValuesChanged(this); }
}