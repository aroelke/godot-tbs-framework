using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Conditions;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Scenes.Level.Control;

[GlobalClass, Tool]
public partial class RegionUnitAction : UnitAction
{
    /// <summary>Identity of the region.</summary>
    [Export] public StringName Region = "";

    [Export] public UnitFilter Allowed = null;

    /// <summary>Each unit can only perform this action once.</summary>
    [Export] public bool OncePerUnit = false;

    /// <summary>When this action is performed in a cell, that cell is removed from the region.</summary>
    [Export] public bool OncePerCell = false;

    public override bool RequiresTarget => false;

    public override bool CanPerform(UnitData unit, Vector2I source)
    {
        if (!unit.Grid.Regions.ContainsKey(Region))
            GD.PushWarning($"There is no region named {Region}. Deleting regions, rather than just removing all their cells, is not recommended.");

        bool allowed = Allowed?.IsIncluded(unit) ?? false;
        bool performed = unit.Grid.UnitPerformedActions.GetValueOrDefault(this, []).GetValueOrDefault(unit.Identity, 0) > 0;
        bool within = unit.Grid.Regions.GetValueOrDefault(Region, []).Contains(source);
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
            GD.PushWarning($"Updating grid with result for RegionUnitAction that isn't null. Should this have been used for a different action?");

        if (OncePerCell)
            grid.Regions[Region] = grid.Regions[Region].Where((c) => c != result.Actor.Cell);
    }

    public override GridData Simulate(UnitData unit, Vector2I source, Vector2I target)
    {
        GridData grid = unit.Grid.Clone();
        grid.Occupants[unit.Cell].Cell = source;
        UpdateGrid(grid, new(null, unit, GridData.InvalidCell, this));
        return grid;
    }

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        base._ValidateProperty(property);
        if (Engine.IsEditorHint() && property["name"].AsStringName() == PropertyName.Region)
            HighlightedRegion.AddRegionsToProperty(property);
    }
}