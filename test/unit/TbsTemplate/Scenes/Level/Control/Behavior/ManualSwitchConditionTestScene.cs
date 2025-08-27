using GD_NET_ScOUT;
using Godot;

namespace TbsTemplate.Scenes.Level.Control.Test;

[Test]
public partial class ManualSwitchConditionTestScene : Node
{
    [Test]
    public void TestSwitchConditionSatisfied()
    {
        ManualSwitchCondition dut = GetNode<ManualSwitchCondition>("ManualSwitchCondition");
        Assert.IsFalse(dut.Satisfied);
        dut.Trigger();
        Assert.IsTrue(dut.Satisfied);
    }
}