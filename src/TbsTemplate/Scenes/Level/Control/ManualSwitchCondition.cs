using Godot;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Switch condition that only switches when a function is called.</summary>
[Tool]
public partial class ManualSwitchCondition : SwitchCondition
{
    /// <summary>Switch whether the condition is satisfied.</summary>
    public void Trigger() => Satisfied = !Satisfied;
}