using System.Collections.Generic;
using System.Security;
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

    private void RunTest(AIController.DecisionType decider, Vector2I[] allyCells, Vector2I[] enemyCells, int expectedSelected, Vector2I expectedDestination, string expectedAction, int expectedTarget=-1)
    {
        _dut.Decision = decider;
        Unit correctSelection = null;
        Unit correctTarget = null;

        Unit CreateUnit(Vector2I cell)
        {
            Unit unit = UnitScene.Instantiate<Unit>();
            unit.Class = new();
            unit.Behavior = new StandBehavior();
            unit.Grid = _dut.Cursor.Grid;
            unit.Cell = cell;
            return unit;
        }
        for (int i = 0; i < allyCells.Length; i++)
        {
            Unit ally = CreateUnit(allyCells[i]);
            if (i == expectedSelected)
                correctSelection = ally;
            _allies.AddChild(ally);
        }
        for (int i = 0; i < enemyCells.Length; i++)
        {
            Unit enemy = CreateUnit(enemyCells[i]);
            if (i == expectedTarget)
                correctTarget = enemy;
            _enemies.AddChild(enemy);
        }

        (Unit selected, Vector2I destination, StringName action, Unit target) = _dut.ComputeAction(_allies, _enemies);
        Assert.AreSame(selected, correctSelection);
        Assert.AreEqual(destination, expectedDestination);
        Assert.AreEqual<StringName>(action, expectedAction);
        if (correctTarget is null)
            Assert.IsNull(target);
        else
            Assert.AreSame(target, correctTarget);
    }

    [Test] public void TestNoEnemiesInRangeClosest() => RunTest(AIController.DecisionType.ClosestEnemy,
        allyCells:           [new(0, 1), new(1, 2), new(0, 3)],
        enemyCells:          [new(6, 2)],
        expectedSelected:    1,
        expectedDestination: new(1, 2),
        expectedAction:      "End"
    );

    [Test] public void TestNoEnemiesInRangeLoop() => RunTest(AIController.DecisionType.TargetLoop,
        allyCells:           [new(0, 1), new(1, 2), new(0, 3)],
        enemyCells:          [new(6, 2)],
        expectedSelected:    1,
        expectedDestination: new(1, 2),
        expectedAction:      "End"
    );

    [AfterEach]
    public void FinalizeTest()
    {
        _dut.FinalizeAction();
        _dut.FinalizeTurn();

        foreach (Unit unit in (IEnumerable<Unit>)_allies)
            unit.QueueFree();
        foreach (Unit unit in (IEnumerable<Unit>)_enemies)
            unit.QueueFree();
    }
}