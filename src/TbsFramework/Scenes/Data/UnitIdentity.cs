using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Level.Conditions;

namespace TbsFramework.Scenes.Data;

/// <summary>
/// Represents the identity of a specific unit. For convenience, this class can also be used in place of a <see cref="UnitFilter"/>
/// to get all units on a grid with this identity (which should normally be either zero or one of them).
/// </summary>
[GlobalClass, Tool]
public partial class UnitIdentity : UnitFilter
{
    public override IEnumerable<UnitData> GetUnits(GridData grid) => grid.Occupants.Values.Where((u) => u.Identity == this);
    public override bool IsIncluded(UnitData unit) => unit.Identity == this;
}