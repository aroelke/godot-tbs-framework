using System.Collections.Generic;
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

    /// <summary>Node resolved from <see cref="RegionPath"/> after <see cref="Initialize"/> completes.</summary>
    public SpecialActionRegion Region = null;

    public override bool RequiresTarget => false;

    public override bool CanPerform(UnitData unit, Vector2I source) => Region.Data.CanPerformIn(source, unit);
    public override bool CanPerform(UnitData unit, Vector2I source, Vector2I target) => target == source && CanPerform(unit, source);
    public override IEnumerable<Vector2I> GetTargetCells(UnitData unit, Vector2I cell) => [];
    public override IEnumerable<Vector2I> GetAllTargetCells(UnitData unit) => [];
    public override IEnumerable<Vector2I> GetValidTargetCells(UnitData unit) => [];
    public override IEnumerable<Vector2I> GetSourceCells(UnitData unit, Vector2I target) => [];
    public override UnitActionResult Perform(UnitData unit, Vector2I target) => new(null, unit, target, this);

    public override void UpdateGrid(GridData grid, UnitActionResult result)
    {
        if (result.Result is not null)
            GD.PushWarning($"Updating grid with result for ActionExecuteRegion that isn't null. Should this have been used for a different action?");

        Region.Data.Perform(result.Actor, result.Target);
    }

    public override GridData Simulate(UnitData unit, Vector2I source, Vector2I target)
    {
        throw new System.NotImplementedException();
    }

    public override void Initialize(LevelManager manager)
    {
        Region = manager.GetNode<SpecialActionRegion>(RegionPath);
    }
}