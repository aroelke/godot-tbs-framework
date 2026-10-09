using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>Grid condition that indicates units are inside or outside an area on the map. How the area is specified is implementation-defined. </summary>
[GlobalClass, Tool]
public abstract partial class AreaCondition : GridCondition
{
    /// <summary>Units to test for presence in the area.</summary>
    [Export] public UnitFilter TriggerUnits = null;

    /// <summary>Whether the units should be inside or outside the area.</summary>
    [Export] public bool Inside = true;

    /// <summary>Whether all applicable units must be inside or outside the area to satisfy the condition or only some of them need to be.</summary>
    [Export] public bool RequiresEveryone = false;

    /// <returns>The set of cells on the <paramref name="grid"/> that define the area.</returns>
    public abstract IEnumerable<Vector2I> GetRegion(GridData grid);

    public override bool IsSatisfied(GridData grid)
    {
        if (TriggerUnits is null)
            return false;

        IEnumerable<UnitData> units = TriggerUnits.GetUnits(grid);
        Func<UnitData, bool> matches = Inside ? (u) => GetRegion(grid).Contains(u.Cell) : (u) => !GetRegion(grid).Contains(u.Cell);
        return RequiresEveryone ? units.All(matches) : units.Any(matches);
    }
}