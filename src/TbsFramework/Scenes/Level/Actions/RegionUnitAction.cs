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

    /// <summary>Identities of units allowed to perform the action.</summary>
    [Export] public UnitIdentity[] AllowedUnits = [];

    /// <summary>Factions whose units are allowed to perform the action in the region.</summary>
    [Export] public Faction[] AllowedFactions = [];

    /// <summary>Each unit can only perform this action once.</summary>
    [Export] public bool OncePerUnit = false;

    /// <summary>When this action is performed in a cell, that cell is removed from the region.</summary>
    [Export] public bool OncePerCell = false;

    /// <summary>Identity of the region resolved from <see cref="RegionPath"/> after <see cref="Initialize"/> completes.</summary>
    public SpecialActionRegionReferenceType RegionIdentity = null;

    public override bool RequiresTarget => false;

    /// <returns>
    /// The identities as defined by <see cref="IHasIdentity{T, U}"/> of all of the units in <paramref name="grid"/> that are allowed to perform
    /// the action.
    /// </returns>
    public HashSet<UnitIdentity> AllAllowedUnits(GridData grid) => [..AllowedUnits, ..AllowedFactions.SelectMany((f) => f.GetUnits(grid)).Select((u) => u.Identity)];

    public override bool CanPerform(UnitData unit, Vector2I source)
    {
        bool allowed = (AllowedFactions.Length > 0 && AllowedFactions.Any(unit.Faction.AlliedTo)) || AllowedUnits.Contains(unit.Identity);
        bool performed = unit.Grid.SpecialActionRegions[RegionIdentity].Performed.ContainsKey(unit.Identity);
        bool within = unit.Grid.SpecialActionRegions[RegionIdentity].Cells.Contains(source);
        return allowed && (!OncePerUnit || !performed) && within;
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

        if (OncePerUnit)
        {
            if (!grid.SpecialActionRegions[RegionIdentity].Performed.ContainsKey(result.Actor.Identity))
                grid.SpecialActionRegions[RegionIdentity].Performed[result.Actor.Identity] = 1;
        }
        else
            grid.SpecialActionRegions[RegionIdentity].Performed[result.Actor.Identity] = grid.SpecialActionRegions[RegionIdentity].Performed.GetValueOrDefault(result.Actor.Identity, 0) + 1;
        if (OncePerCell)
            grid.SpecialActionRegions[RegionIdentity].Cells = grid.SpecialActionRegions[RegionIdentity].Cells.Remove(result.Actor.Cell);
    }

    public override GridData Simulate(UnitData unit, Vector2I source, Vector2I target)
    {
        GridData grid = unit.Grid.Clone();
        grid.Occupants[unit.Cell].Cell = source;
        UpdateGrid(grid, new(null, unit, GridData.InvalidCell, this));
        return grid;
    }

    public override void Initialize(Node owner)
    {
        RegionIdentity = owner.GetNode<SpecialActionRegion>(RegionPath).Data.Identity;
    }
}