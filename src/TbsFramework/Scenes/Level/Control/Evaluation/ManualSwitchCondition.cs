using Godot;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>Switch condition that only switches when a function is called.</summary>
[Tool]
public partial class ManualSwitchCondition : SwitchConditionNode
{
    /// <summary>Switch whether the condition is satisfied.</summary>
    public void Trigger() => Satisfied = !Satisfied;
}