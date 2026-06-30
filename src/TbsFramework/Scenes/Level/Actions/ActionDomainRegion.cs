using Godot;
using TbsFramework.Scenes.Level.Events;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Scenes.Level.Actions;

/// <summary>Defines the cells a unit can perform an action from using the used cells of a <see cref="SpecialActionRegion"/>.</summary>
[GlobalClass, Tool]
public partial class ActionDomainRegion : ActionDomain
{
    /// <summary>Scene path to the <see cref="SpecialActionRegion"/> relative to the parameter of <see cref="Initialize"/>.</summary>
    [Export(PropertyHint.NodePathValidTypes, nameof(SpecialActionRegion))] public NodePath RegionPath = null;

    /// <summary>Node resolved from <see cref="RegionPath"/> after <see cref="Initialize"/> completes.</summary>
    public SpecialActionRegion Region = null;

    public override bool Contains(Vector2I cell) => Region.Data.Cells.Contains(cell);

    public override void Initialize(LevelManager manager)
    {
        base.Initialize(manager);
        Region = manager.GetNode<SpecialActionRegion>(RegionPath);
    }
}