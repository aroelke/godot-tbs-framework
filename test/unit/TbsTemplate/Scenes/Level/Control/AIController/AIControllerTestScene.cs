using System.Collections.Generic;
using GD_NET_ScOUT;
using Godot;
using TbsTemplate.Scenes.Level.Control.Behavior;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control.Test;

[Test]
public partial class AIControllerTestScene : Node
{
    private AIController _dut = null;
    private Army _allies = null, _enemies = null;

    [Export] public PackedScene UnitScene = null;

    private Unit CreateUnit(Vector2I cell, int[] attackRange=null, UnitBehavior behavior=null)
    {
        Unit unit = UnitScene.Instantiate<Unit>();
        unit.Class = new();
        unit.Behavior = behavior ?? new StandBehavior();
        unit.Grid = _dut.Cursor.Grid;
        unit.Cell = cell;
        unit.Grid.Occupants[cell] = unit;
        if (attackRange is not null)
            unit.AttackRange = attackRange;
        return unit;
    }

    [BeforeAll]
    public void SetupTests()
    {
        _dut = GetNode<AIController>("Army1/AIController");
        _allies = GetNode<Army>("Army1");
        _enemies = GetNode<Army>("Army2");
    }

    [BeforeEach]
    public void InitializeTest()
    {
        _dut.InitializeTurn();
    }

    private void RunTest(AIController.DecisionType decider, Unit[] allies, Unit[] enemies, Unit expectedSelected, Vector2I expectedDestination, string expectedAction, Unit expectedTarget=null)
    {
        _dut.Decision = decider;

        foreach (Unit ally in allies)
            _allies.AddChild(ally);
        foreach (Unit enemy in enemies)
            _enemies.AddChild(enemy);

        (Unit selected, Vector2I destination, StringName action, Unit target) = _dut.ComputeAction(_allies, _enemies);
        Assert.AreSame(selected, expectedSelected);
        Assert.AreEqual(destination, expectedDestination);
        Assert.AreEqual<StringName>(action, expectedAction);
        if (expectedTarget is null)
            Assert.IsNull(target);
        else
            Assert.AreSame(target, expectedTarget);
    }

    [Test]
    public void TestNoEnemiesInRangeClosest()
    {
        Unit[] allies = [CreateUnit(new(0, 1)), CreateUnit(new(1, 2)), CreateUnit(new(0, 3))];
        Unit[] enemies = [CreateUnit(new(6, 2))];
        RunTest(AIController.DecisionType.ClosestEnemy, allies, enemies,
            expectedSelected:    allies[1],
            expectedDestination: allies[1].Cell,
            expectedAction:      "End"
        );
    }

    [Test]
    public void TestNoEnemiesInRangeLoop()
    {
        Unit[] allies = [CreateUnit(new(0, 1)), CreateUnit(new(1, 2)), CreateUnit(new(0, 3))];
        Unit[] enemies = [CreateUnit(new(6, 2))];
        RunTest(AIController.DecisionType.ClosestEnemy, allies, enemies,
            expectedSelected:    allies[1],
            expectedDestination: allies[1].Cell,
            expectedAction:      "End"
        );
    }

    [Test]
    public void TestEnemiesInRangeClosest()
    {
        Unit[] allies = [CreateUnit(new(3, 2), attackRange:[1, 2], behavior:new StandBehavior() { AttackInRange = true })];
        Unit[] enemies = [CreateUnit(new(2, 1)), CreateUnit(new(2, 2)), CreateUnit(new(1, 3))];
        RunTest(AIController.DecisionType.ClosestEnemy, allies, enemies,
            expectedSelected:    allies[0],
            expectedDestination: allies[0].Cell,
            expectedAction:      "Attack",
            expectedTarget:      enemies[1]
        );
    }

    [AfterEach]
    public void FinalizeTest()
    {
        _dut.FinalizeAction();
        _dut.FinalizeTurn();

        foreach (Unit unit in (IEnumerable<Unit>)_allies)
            unit.Die();
        foreach (Unit unit in (IEnumerable<Unit>)_enemies)
            unit.Die();
    }
}