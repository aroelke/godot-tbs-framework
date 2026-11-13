using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>Represents a collection of units on the map.</summary>
[GlobalClass, Tool]
public abstract partial class UnitFilter : Resource
{
    /// <returns><c>true</c> if <paramref name="unit"/> is included and <c>false</c> otherwise.</returns>
    public abstract bool IsIncluded(UnitData unit);

    /// <summary>Filters a list of units to contain only the ones part of this collection.</summary>
    /// <param name="units">List of units to filter.</param>
    /// <returns>The filtered list of units.</returns>
    public virtual IEnumerable<UnitData> Filter(IEnumerable<UnitData> units) => units.Where(IsIncluded);

    /// <returns>The units on the <paramref name="grid"/> that are part of the collection.</returns>
    public virtual IEnumerable<UnitData> GetUnits(GridData grid) => Filter(grid.Occupants.Values);
}