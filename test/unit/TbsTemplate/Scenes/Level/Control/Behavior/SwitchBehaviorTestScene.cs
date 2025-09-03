using GD_NET_ScOUT;
using Godot;

namespace TbsTemplate.Scenes.Level.Control.Test;

[Test]
public partial class SwitchBehaviorTestScene : Node
{
    private static (Behavior initial, Behavior final) GetBehaviorOptions(SwitchBehavior dut) => (dut.GetNode<Behavior>("BehaviorA"), dut.GetNode<Behavior>("BehaviorB"));

    [Test]
    public void TestDefaultSwitchBehaviorLatches()
    {
        SwitchBehavior dut = GetNode<SwitchBehavior>("DefaultSwitchBehavior");
        ManualSwitchCondition condition = dut.GetNode<ManualSwitchCondition>("ManualSwitchCondition");
        (Behavior initial, Behavior final) = GetBehaviorOptions(dut);

        dut.Reset();
        condition.Reset();
        Assert.AreEqual(dut.TargetBehavior(), initial);

        condition.Trigger();
        Assert.AreEqual(dut.TargetBehavior(), final);
        condition.Trigger();
        Assert.AreEqual(dut.TargetBehavior(), final);
    }

    [Test]
    public void TestRevertSwitchBehaviorReverts()
    {
        SwitchBehavior dut = GetNode<SwitchBehavior>("RevertSwitchBehavior");
        ManualSwitchCondition condition = dut.GetNode<ManualSwitchCondition>("ManualSwitchCondition");
        (Behavior initial, Behavior final) = GetBehaviorOptions(dut);

        dut.Reset();
        condition.Reset();
        Assert.AreEqual(dut.TargetBehavior(), initial);

        condition.Trigger();
        Assert.AreEqual(dut.TargetBehavior(), final);
        condition.Trigger();
        Assert.AreEqual(dut.TargetBehavior(), initial);
    }
}