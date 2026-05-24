using System.Collections.Generic;
using Godot;
using TbsFramework.Scenes.Combat;

namespace TbsFramework.Scenes.Data;

/// <summary>Base class for stats that describe how units interact with each other.</summary>
public abstract partial class AbstractStats : Resource
{
    /// <summary>Describes the maximum health of the unit.</summary>
    public abstract double MaxHealth { get; }

    /// <summary>Describes the furthest distance a unit can move from its starting cell.</summary>
    public abstract int MoveDistance { get; }

    public abstract int[] GetActionRange(StringName action);

    public abstract int GetTerrainCostModifier(Terrain terrain);
}