using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Events;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Scenes.Level.Actions;

/// <summary>Defines the execution of an action to be to update a <see cref="SpecialActionRegion"/> with the unit performing the action.</summary>
[GlobalClass, Tool]
public partial class ActionExecuteRegion : ActionExecute
{
    /// <summary>Scene path to the <see cref="SpecialActionRegion"/> relative to the parameter of <see cref="Initialize"/>.</summary>
    [Export(PropertyHint.NodePathValidTypes, nameof(SpecialActionRegion))] public NodePath RegionPath = null;

    /// <summary>Node resolved from <see cref="RegionPath"/> after <see cref="Initialize"/> completes.</summary>
    public SpecialActionRegion Region = null;

    public override object Perform(UnitData unit, Vector2I target) => null;

    public override void UpdateGrid(GridData grid, UnitData actor, Vector2I target, object result)
    {
        if (result is not null)
            GD.PushWarning($"Updating grid with result for ActionExecuteRegion that isn't null. Should this have been used for a different action?");

        Region.Data.Perform(actor, target);
    }

    public override GridData Simulate(UnitData unit, Vector2I source, Vector2I target)
    {
        throw new System.NotImplementedException();
    }

    public override void Initialize(LevelManager manager)
    {
        base.Initialize(manager);
        Region = manager.GetNode<SpecialActionRegion>(RegionPath);
    }
}