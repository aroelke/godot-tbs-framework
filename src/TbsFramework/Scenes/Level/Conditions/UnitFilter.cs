using System.Collections.Generic;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>Represents a collection of units on the map.</summary>
[GlobalClass, Tool]
public abstract partial class UnitFilter : Resource
{
    /// <returns>The units on the <paramref name="grid"/> that are part of the collection.</returns>
    public abstract IEnumerable<UnitData> GetUnits(GridData grid);

    /// <returns><c>true</c> if <paramref name="unit"/> is included and <c>false</c> otherwise.</returns>
    public abstract bool IsIncluded(UnitData unit);
}