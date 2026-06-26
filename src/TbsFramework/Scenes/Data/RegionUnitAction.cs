using System.Collections.Generic;
using Godot;
using TbsFramework.Scenes.Level.Events;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Scenes.Data;

[GlobalClass, Tool]
public partial class RegionUnitAction : FlatUnitAction
{
    [Export(PropertyHint.NodePathValidTypes, "TileMapLayer")] public NodePath RegionPath = null;

    public SpecialActionRegion Region = null;

    public override IEnumerable<Vector2I> GetTargetCells(UnitData unit, Vector2I cell) => Region.Data.CanPerformIn(cell, unit) ? [cell] : [];

    public override IEnumerable<Vector2I> ShowAllTargetCells(UnitData unit) => Region.Data.CanPerform(unit) ? Region.Data.Cells.Intersect(unit.GetTraversableCells()) : [];

    public override IEnumerable<Vector2I> GetAllTargetCells(UnitData unit) => ShowAllTargetCells(unit);

    public override bool CanPerform(UnitData unit, Vector2I source, Vector2I target) => Region.Data.Cells.Contains(source) && Region.Data.CanPerformIn(source, unit);

    public override UnitActionResult Perform(UnitData unit, Vector2I target) => new(Region.Data, unit, target, this);

    public override void UpdateGrid(GridData grid, UnitActionResult result)
    {
        if (result.Result is not null)
            GD.PushWarning($"Updating grid with result for action {Name} that isn't null. Should this have been used for a different action?");

        Region.Data.Perform(result.Actor, result.Target);
    }

    public override GridData Simulate(UnitData unit, Vector2I source, Vector2I target)
    {
        throw new System.NotImplementedException();
    }

    public override void Initialize(LevelManager manager) => Region = manager.GetNodeOrNull<SpecialActionRegion>(RegionPath);
}