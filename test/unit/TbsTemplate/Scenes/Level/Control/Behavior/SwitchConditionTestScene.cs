using GD_NET_ScOUT;
using Godot;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control.Test;

[Test]
public partial class SwitchConditionTestScene : Node
{
    private static void MoveUnit(Unit unit, Vector2I destination)
    {
        unit.Grid.Occupants.Remove(unit.Cell);
        unit.Cell = destination;
        unit.Grid.Occupants[destination] = unit;
        LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.ActionEnded, unit);
    }

    /**********
     * MANUAL *
     **********/

    /// <summary>
    /// Manual switch condition should toggle <see cref="SwitchCondition.Satisfied"/> when <see cref="ManualSwitchCondition.Trigger"/>
    /// is called.
    /// </summary>
    [Test]
    public void TestManualSwitchCondition()
    {
        ManualSwitchCondition dut = GetNode<ManualSwitchCondition>("ManualSwitchCondition");
        dut.Reset();
        Assert.IsFalse(dut.Satisfied);
        dut.Trigger();
        Assert.IsTrue(dut.Satisfied);
    }

    /********
     * TURN *
     ********/

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

    /// <summary>Turn switch condition should not be satisfied if its target turn hasn't been reached.</summary>
    [Test] public void TestTurnSwitchConditionBeforeTrigger() => TestTurnSwitchCondition(4, 5, GetNode<Army>("AllyArmy"), false);

    /// <summary>Turn switch condition should be satisfied on its target turn.</summary>
    [Test] public void TestTurnSwitchConditionWrongArmy() => TestTurnSwitchCondition(5, 5, GetNode<Army>("EnemyArmy"), false);

    /// <summary>Turn switch condition should not be satisfied on a non-triggering army's turn, even if it's the target turn.</summary>
    [Test] public void TestTurnSwitchConditionRightArmy() => TestTurnSwitchCondition(5, 5, GetNode<Army>("AllyArmy"), true);

    /**********
     * REGION *
     **********/

    /// <summary>Default region switch condition should be satisfied as long as an applicable unit is within the region.</summary>
    [Test]
    public void TestRegionSwitchConditionAnyUnitRightArmy()
    {
        RegionSwitchCondition dut = GetNode<RegionSwitchCondition>("RegionSwitchCondition");
        Unit unit = GetNode<Unit>("AllyArmy/Unit");
        MoveUnit(unit, Vector2I.Zero);

        dut.Inside = true;
        dut.RequiresEveryone = false;
        dut.Reset();
        Assert.IsFalse(dut.Satisfied);

        MoveUnit(unit, new(3, 2));
        Assert.IsTrue(dut.Satisfied);

        MoveUnit(unit, Vector2I.Zero);
        Assert.IsFalse(dut.Satisfied);
    }

    /// <summary>If so configured, region switch condition should only be satisfied as long as all applicable units are within the region.</summary>
    [Test]
    public void TestRegionSwitchConditionAllUnitsRightArmy()
    {
        RegionSwitchCondition dut = GetNode<RegionSwitchCondition>("RegionSwitchCondition");
        Unit unit = GetNode<Unit>("AllyArmy/Unit");
        MoveUnit(unit, Vector2I.Zero);
        Unit other = GetNode<Unit>("AllyArmy/Unit2");
        MoveUnit(other, Vector2I.One);

        dut.Inside = true;
        dut.RequiresEveryone = true;
        dut.Reset();
        Assert.IsFalse(dut.Satisfied);

        MoveUnit(unit, new(3, 2));
        Assert.IsFalse(dut.Satisfied);

        MoveUnit(other, new(2, 2));
        Assert.IsTrue(dut.Satisfied);

        MoveUnit(unit, Vector2I.Zero);
        Assert.IsFalse(dut.Satisfied);
    }

    /// <summary>If so configured, region switch condition should be satisfied as long as any applicable units are outside the region.</summary>
    [Test]
    public void TestRegionSwitchConditionInvertedAnyUnitRightArmy()
    {
        RegionSwitchCondition dut = GetNode<RegionSwitchCondition>("RegionSwitchCondition");
        Unit unit = GetNode<Unit>("AllyArmy/Unit");
        MoveUnit(unit, Vector2I.Zero);
        Unit other = GetNode<Unit>("AllyArmy/Unit2");
        MoveUnit(other, Vector2I.One);

        dut.Inside = false;
        dut.RequiresEveryone = false;
        dut.Reset();
        Assert.IsFalse(dut.Satisfied);

        MoveUnit(unit, new(3, 2));
        Assert.IsTrue(dut.Satisfied);

        MoveUnit(other, new(2, 2));
        Assert.IsFalse(dut.Satisfied);

        MoveUnit(unit, Vector2I.Zero);
        Assert.IsTrue(dut.Satisfied);
    }

    /// <summary>If so configured, region switch condition should be satisfied as long as all applicable units are outside the region.</summary>
    [Test]
    public void TestRegionSwitchConditionInvertedAllUnitsRightArmy()
    {
        RegionSwitchCondition dut = GetNode<RegionSwitchCondition>("RegionSwitchCondition");
        Unit unit = GetNode<Unit>("AllyArmy/Unit");
        MoveUnit(unit, Vector2I.Zero);
        Unit other = GetNode<Unit>("AllyArmy/Unit2");
        MoveUnit(other, Vector2I.One);

        dut.Inside = false;
        dut.RequiresEveryone = true;
        dut.Reset();
        Assert.IsFalse(dut.Satisfied);

        MoveUnit(unit, new(3, 2));
        Assert.IsFalse(dut.Satisfied);

        MoveUnit(other, new(2, 2));
        Assert.IsFalse(dut.Satisfied);

        MoveUnit(unit, Vector2I.Zero);
        Assert.IsFalse(dut.Satisfied);

        MoveUnit(other, Vector2I.One);
        Assert.IsTrue(dut.Satisfied);
    }

    /// <summary>Moving a non-applicable unit into the region should not satisfy the condition.</summary>
    [Test]
    public void TestRegionSwitchConditionWrongArmy()
    {
        RegionSwitchCondition dut = GetNode<RegionSwitchCondition>("RegionSwitchCondition");
        Unit enemy = GetNode<Unit>("EnemyArmy/Unit");
        MoveUnit(enemy, new(6, 4));

        dut.Inside = true;
        dut.RequiresEveryone = false;
        dut.Reset();
        Assert.IsFalse(dut.Satisfied);

        MoveUnit(enemy, new(3, 2));
        Assert.IsFalse(dut.Satisfied);
    }

    /************
     * IN-RANGE *
     ************/

    /// <summary>An enemy moving into an applicable unit's range should satisfy the condition.</summary>
    [Test]
    public void TestInRangeSwitchConditionEnemyMoves()
    {
        InRangeSwitchCondition dut = GetNode<InRangeSwitchCondition>("InRangeSwitchCondition");
        Unit enemy = GetNode<Unit>("EnemyArmy/Unit");
        MoveUnit(enemy, new(6, 4));
        dut.Reset();
        Assert.IsFalse(dut.Satisfied);

        MoveUnit(enemy, new(0, 1));
        Assert.IsTrue(dut.Satisfied);

        MoveUnit(enemy, new(6, 4));
        Assert.IsFalse(dut.Satisfied);
    }

    /// <summary>
    /// An applicable unit moving (and thus changing its attack range) such that an enemy is within its range should
    /// satisfy the condition.
    /// </summary>
    [Test]
    public void TestInRangeSwitchConditionAllyMoves()
    {
        InRangeSwitchCondition dut = GetNode<InRangeSwitchCondition>("InRangeSwitchCondition");
        Unit unit = GetNode<Unit>("AllyArmy/Unit");
        MoveUnit(unit, Vector2I.Zero);
        dut.Reset();
        Assert.IsFalse(dut.Satisfied);

        MoveUnit(unit, new(6, 2));
        Assert.IsTrue(dut.Satisfied);

        MoveUnit(unit, Vector2I.Zero);
        Assert.IsFalse(dut.Satisfied);
    }
}