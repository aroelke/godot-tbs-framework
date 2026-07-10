using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Events;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Scenes.Level.Actions;

[GlobalClass, Tool]
public partial class RegionUnitAction : UnitAction
{
    /// <summary>Path in the current scene to the <see cref="SpecialActionRegion"/> relative to the parameter of <see cref="Initialize"/>.</summary>
    [Export(PropertyHint.NodePathValidTypes, nameof(SpecialActionRegion))] public NodePath RegionPath = null;

    /// <summary>
    /// Whether a unit must meet all (<c>true</c>) or any (<c>false</c>) additional permissions in order to be allowed to perform the action. It also
    /// has to be in one of <see cref="Region"/>'s cells.
    /// </summary>
    [Export] public bool IntersectPermission = false;

    /// <summary>
    /// List of additional permissions that control whether or not a unit to perform this action beyond the set of
    /// allowed cells.
    /// </summary>
    [Export] public Godot.Collections.Array<ActionPermission> AdditionalPermissions = [];

    /// <summary>Identity of the region resolved from <see cref="RegionPath"/> after <see cref="Initialize"/> completes.</summary>
    public SpecialActionRegionReferenceType RegionIdentity = null;

    public override bool RequiresTarget => false;

    public override bool CanPerform(UnitData unit, Vector2I source)
    {
        bool hasPermission = AdditionalPermissions.Count == 0 || (IntersectPermission ? AdditionalPermissions.All((c) => c.CanPerform(unit)) : AdditionalPermissions.Any((c) => c.CanPerform(unit)));
        return hasPermission && unit.Grid.SpecialActionRegions[RegionIdentity].CanPerformIn(source, unit);
    }

    public override bool CanPerform(UnitData unit, Vector2I source, Vector2I target) => target == source && CanPerform(unit, source);
    public override IEnumerable<Vector2I> GetTargetCells(UnitData unit, Vector2I cell) => [];
    public override IEnumerable<Vector2I> GetAllTargetCells(UnitData unit) => [];
    public override IEnumerable<Vector2I> GetValidTargetCells(UnitData unit, IEnumerable<Vector2I> traversable) => [];
    public override IEnumerable<Vector2I> GetValidTargetCells(UnitData unit) => [];
    public override IEnumerable<Vector2I> GetSourceCells(UnitData unit, Vector2I target) => [];
    public override UnitActionResult Perform(UnitData unit, Vector2I target) => new(null, unit, target, this);

    public override void UpdateGrid(GridData grid, UnitActionResult result)
    {
        if (result.Result is not null)
            GD.PushWarning($"Updating grid with result for ActionExecuteRegion that isn't null. Should this have been used for a different action?");

        grid.SpecialActionRegions[RegionIdentity].Perform(result.Actor, result.Target);
    }

    public override GridData Simulate(UnitData unit, Vector2I source, Vector2I target)
    {
        throw new System.NotImplementedException();
    }

    public override void Initialize(LevelManager manager)
    {
        RegionIdentity = manager.GetNode<SpecialActionRegion>(RegionPath).Data.Identity;
        foreach (ActionPermission permission in AdditionalPermissions)
            permission.Initialize(manager);
    }
}