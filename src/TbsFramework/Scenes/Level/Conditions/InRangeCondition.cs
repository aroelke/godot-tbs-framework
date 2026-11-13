using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Control;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>Grid condition that indicates that a set of source units can perform an action on the set of trigger units.</summary>
[GlobalClass, Tool]
public partial class InRangeCondition : AreaCondition
{
    /// <summary>Units that could perform the action.</summary>
    [Export] public UnitFilter SourceUnits = null;

    /// <summary>Action to use for range checking.</summary>
    [Export] public UnitAction Action = null;

    /// <summary>
    /// Whether or not the trigger units should be in range of any of the source units (<c>false</c>) or all of them (<c>true</c>).
    /// </summary>
    [Export] public bool UseIntersection = false;

    public override IEnumerable<Vector2I> GetRegion(GridData grid)
    {
        if (SourceUnits is null || Action is null)
            return [];

        static IEnumerable<Vector2I> Union(IEnumerable<Vector2I> a, IEnumerable<Vector2I> b) => a.Union(b);
        static IEnumerable<Vector2I> Intersection(IEnumerable<Vector2I> a, IEnumerable<Vector2I> b) => a.Intersect(b);
        return SourceUnits.GetUnits(grid).Select((u) => Action.GetAllTargetCells(u, u.GetTraversableCells())).Aggregate(UseIntersection ? Intersection : Union);
    }
}