using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>Represents a collection of units on the map.</summary>
[GlobalClass, Tool]
public partial class UnitFilter : Resource
{
    /// <summary>Specific units that should be part of the collection.</summary>
    [Export] public UnitIdentity[] Units = [];

    /// <summary>Factions containing units that should be part of the collection.</summary>
    [Export] public Faction[] Factions = [];

    /// <returns>The units on the <paramref name="grid"/> that are part of the collection.</returns>
    public IEnumerable<UnitData> GetUnits(GridData grid) => grid.Occupants.Values.Where((u) => Units.Contains(u.Identity) || Factions.Contains(u.Faction));

    /// <returns><c>true</c> if <paramref name="unit"/> is included and <c>false</c> otherwise.</returns>
    public bool IsIncluded(UnitData unit) => Units.Contains(unit.Identity) || Factions.Contains(unit.Faction);
}