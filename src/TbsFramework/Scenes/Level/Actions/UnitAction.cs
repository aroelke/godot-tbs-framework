using Godot;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Scenes.Level.Actions;

public partial class UnitAction : Resource
{
    [Export] public Godot.Collections.Array<ActionPermission> PermissionComponents = [];

    [Export] public Godot.Collections.Array<ActionDomain> DomainComponents = [];

    [Export] public Godot.Collections.Array<ActionRange> RangeComponents = [];

    [Export] public ActionExecute ExecuteComponent = null;

    public void Initialize(LevelManager manager)
    {
        foreach (ActionPermission component in PermissionComponents)
            component.Initialize(manager);
        foreach (ActionDomain component in DomainComponents)
            component.Initialize(manager);
        foreach (ActionRange component in RangeComponents)
            component.Initialize(manager);
        ExecuteComponent.Initialize(manager);
    }
}