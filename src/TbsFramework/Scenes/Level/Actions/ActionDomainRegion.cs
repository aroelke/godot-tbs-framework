using Godot;
using TbsFramework.Scenes.Level.Events;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Scenes.Level.Actions;

[GlobalClass, Tool]
public partial class ActionDomainRegion : ActionDomain
{
    [Export(PropertyHint.NodePathValidTypes, nameof(SpecialActionRegion))] public NodePath RegionPath = null;

    public SpecialActionRegion Region = null;

    public override bool Contains(Vector2I cell) => Region.Data.Cells.Contains(cell);

    public override void Initialize(LevelManager manager)
    {
        base.Initialize(manager);
        Region = manager.GetNode<SpecialActionRegion>(RegionPath);
    }
}