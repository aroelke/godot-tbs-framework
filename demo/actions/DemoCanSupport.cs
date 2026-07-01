using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Actions;

namespace TbsFramework.Demo;

[GlobalClass, Tool]
public partial class DemoCanSupport : ActionPermission
{
    public override bool CanPerform(UnitData unit) => unit.Stats.Healing > 0;
}