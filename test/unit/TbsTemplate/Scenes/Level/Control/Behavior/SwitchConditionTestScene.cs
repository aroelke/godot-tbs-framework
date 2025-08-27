using GD_NET_ScOUT;
using Godot;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control.Test;

[Test]
public partial class SwitchConditionTestScene : Node
{
    [Test]
    public void TestManualSwitchCondition()
    {
        ManualSwitchCondition dut = GetNode<ManualSwitchCondition>("ManualSwitchCondition");
        Assert.IsFalse(dut.Satisfied);
        dut.Trigger();
        Assert.IsTrue(dut.Satisfied);
    }

    private void TestTurnSwitchCondition(int turn, int target, Army army, bool expected)
    {
        TurnSwitchCondition dut = GetNode<TurnSwitchCondition>("TurnSwitchCondition");
        dut.TriggerTurn = target;
        dut.Reset();
        Assert.IsFalse(dut.Satisfied);

        // Perform test
        LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.TurnBegan, turn, army);
        Assert.AreEqual(dut.Satisfied, expected);
    }

    [Test] public void TestTurnSwitchConditionFirstTurn() => TestTurnSwitchCondition(1, 5, GetNode<Army>("AllyArmy"), false);
    [Test] public void TestTurnSwitchConditionBeforeTrigger() => TestTurnSwitchCondition(4, 5, GetNode<Army>("AllyArmy"), false);
    [Test] public void TestTurnSwitchConditionWrongArmy() => TestTurnSwitchCondition(5, 5, GetNode<Army>("EnemyArmy"), false);
    [Test] public void TestTurnSwitchConditionRightArmy() => TestTurnSwitchCondition(5, 5, GetNode<Army>("AllyArmy"), true);

    [Test]
    public void TestRegionSwitchConditionAnyUnitRightArmy()
    {
        RegionSwitchCondition dut = GetNode<RegionSwitchCondition>("RegionSwitchCondition");
        Grid grid = GetNode<Grid>("Grid");
        Unit unit = GetNode<Unit>("AllyArmy/Unit");

        dut.Reset();
        Assert.IsFalse(dut.Satisfied);

        grid.Occupants.Remove(unit.Cell);
        unit.Cell = new(3, 2);
        grid.Occupants[unit.Cell] = unit;
        LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.ActionEnded, unit);
        Assert.IsTrue(dut.Satisfied);

        grid.Occupants.Remove(unit.Cell);
        unit.Cell = Vector2I.Zero;
        grid.Occupants[unit.Cell] = unit;
        LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.ActionEnded, unit);
        Assert.IsFalse(dut.Satisfied);
    }
}