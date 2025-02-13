using System.Collections.Generic;
using GD_NET_ScOUT;
using Godot;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control.Test;

[Test]
public partial class AIControllerTestScene : Node
{
    private AIController _dut = null;

    [BeforeAll]
    public void SetupTests()
    {
        _dut = GetNode<AIController>("Army1/AIController");
    }

    [BeforeEach]
    public void InitializeTest()
    {
        _dut.InitializeTurn();
    }

    private void TestNoEnemiesInRange(AIController.DecisionType decider)
    {
        _dut.Decision = decider;

        IEnumerable<Unit> allies = GetNode<Army>("Army1");
        IEnumerable<Unit> enemies = GetNode<Army>("Army2");
        Unit correct = GetNode<Unit>("Army1/Unit2");

        (Unit selected, Vector2I destination, StringName action, Unit target) = _dut.ComputeAction(allies, enemies);
        Assert.AreSame(selected, correct);
        Assert.AreEqual(destination, correct.Cell);
        Assert.AreEqual<StringName>(action, "End");
        Assert.AreEqual(target, null);
    }
    [Test] public void TestNoEnemiesInRangeClosest() => TestNoEnemiesInRange(AIController.DecisionType.ClosestEnemy);
    [Test] public void TestNoEnemiesInRangeLoop() => TestNoEnemiesInRange(AIController.DecisionType.TargetLoop);

    [AfterEach]
    public void FinalizeTest()
    {
        _dut.FinalizeAction();
        _dut.FinalizeTurn();
    }
}