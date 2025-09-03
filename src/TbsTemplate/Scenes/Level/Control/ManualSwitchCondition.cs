using Godot;

namespace TbsTemplate.Scenes.Level.Control;

[Tool]
public partial class ManualSwitchCondition : SwitchCondition
{
    public void Trigger() => Satisfied = !Satisfied;
}