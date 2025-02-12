using System.Collections.Generic;
using GD_NET_ScOUT;
using Godot;
using TbsTemplate.Scenes.Level.Control;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.UnitTesting.Scenes.Level.Control;

[Test]
public partial class AIControllerNothingInRange : Node
{
    private AIController _controller = null;
    private IEnumerable<Unit> _allies = null;
    private IEnumerable<Unit> _enemies = null;

    [BeforeAll]
    public void SetupTests()
    {
        _controller = GetNode<AIController>("Army1/AIController");
        _allies = (IEnumerable<Unit>)GetNode<Army>("Army1");
        _enemies = (IEnumerable<Unit>)GetNode<Army>("Army2");
    }

    [BeforeEach]
    public void SetupController()
    {
        _controller.InitializeTurn();
    }

    [Test]
    public void TestNoEnemiesInRange()
    {
        Unit correct = GetNode<Unit>("Army1/Unit2");

        (Unit selected, Vector2I destination, StringName action, Unit target) = _controller.ComputeAction(_allies, _enemies);
        Assert.AreSame(selected, correct);
        Assert.AreEqual(destination, correct.Cell);
        Assert.AreEqual<StringName>(action, "End");
        Assert.AreEqual(target, null);
    }

    [AfterEach]
    public void CompleteTests()
    {
        _controller.FinalizeTurn();
    }
}