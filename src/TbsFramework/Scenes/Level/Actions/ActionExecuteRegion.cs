using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Events;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Scenes.Level.Actions;

[GlobalClass, Tool]
public partial class ActionExecuteRegion : ActionExecute
{
    [Export(PropertyHint.NodePathValidTypes, nameof(SpecialActionRegion))] public NodePath RegionPath = null;

    public SpecialActionRegion Region = null;

    public override UnitActionResult Perform(UnitData unit, Vector2I target) => new(Region.Data, unit, target, this);

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
        base.Initialize(manager);
        Region = manager.GetNode<SpecialActionRegion>(RegionPath);
    }
}