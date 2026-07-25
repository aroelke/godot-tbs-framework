using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Scenes.Level.Actions;

[GlobalClass, Tool]
public partial class RegionUnitAction : UnitAction
{
    /// <summary>Path in the current scene to the <see cref="SpecialActionRegion"/> relative to the parameter of <see cref="Initialize"/>.</summary>
    [Export(PropertyHint.NodePathValidTypes, nameof(SpecialActionRegion))] public NodePath RegionPath = null;

    /// <summary>List of node paths in the current scene to the units to allow relative to the parameter of <see cref="Initialize"/></summary>
    [Export(PropertyHint.TypeString, $"22/26:{nameof(Unit)}" /* Variant.Type.NodePath=22/PropertyHint.NodePathValidTypes=26 */)] public NodePath[] AllowedUnitPaths = [];

    [Export] public Faction[] AllowedFactions = [];

    /// <summary>Identity of the region resolved from <see cref="RegionPath"/> after <see cref="Initialize"/> completes.</summary>
    public SpecialActionRegionReferenceType RegionIdentity = null;

    /// <summary>
    /// Identities of units resolved from <see cref="AllowedUnitPaths"/> after <see cref="Initialize"/> completes. If empty, any unit can perform
    /// the action.
    /// </summary>
    public IEnumerable<UnitReferenceType> AllowedUnitIdentities = null;

    public override bool RequiresTarget => false;

    public override bool CanPerform(UnitData unit, Vector2I source)
    {
        bool hasPermission = (AllowedFactions.Length > 0 && AllowedFactions.Any(unit.Faction.AlliedTo)) || AllowedUnitIdentities.Contains(unit.Identity);
        return hasPermission && unit.Grid.SpecialActionRegions[RegionIdentity].CanPerformIn(source, unit);
    }

    public override bool CanPerform(UnitData unit, Vector2I source, Vector2I target) => CanPerform(unit, source);
    public override IEnumerable<Vector2I> GetTargetCells(UnitData unit, Vector2I cell) => [];
    public override IEnumerable<Vector2I> GetAllTargetCells(UnitData unit, IEnumerable<Vector2I> traversable) => [];
    public override IEnumerable<Vector2I> GetValidTargetCells(UnitData unit, IEnumerable<Vector2I> traversable) => [];
    public override IEnumerable<Vector2I> GetSourceCells(UnitData unit, Vector2I target) => [];
    public override UnitActionResult Perform(UnitData unit, Vector2I target) => new(null, unit, target, this);

    public override void UpdateGrid(GridData grid, UnitActionResult result)
    {
        if (result.Result is not null)
            GD.PushWarning($"Updating grid with result for ActionExecuteRegion that isn't null. Should this have been used for a different action?");

        grid.SpecialActionRegions[RegionIdentity].Perform(result.Actor, result.Actor.Cell);
    }

    public override GridData Simulate(UnitData unit, Vector2I source, Vector2I target)
    {
        GridData grid = unit.Grid.Clone();
        grid.Occupants[unit.Cell].Cell = source;
        grid.SpecialActionRegions[RegionIdentity].Perform(grid.Occupants[source], source);
        return grid;
    }

    public override void Initialize(Node owner)
    {
        RegionIdentity = owner.GetNode<SpecialActionRegion>(RegionPath).Data.Identity;
        AllowedUnitIdentities = AllowedUnitPaths.Select((p) => owner.GetNode<Unit>(p).UnitData.Identity);
    }
}