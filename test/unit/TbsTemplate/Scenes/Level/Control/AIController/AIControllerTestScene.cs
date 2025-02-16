using System.Collections.Generic;
using System.Linq;
using GD_NET_ScOUT;
using Godot;
using TbsTemplate.Data;
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

    private Unit CreateUnit(Vector2I cell, int[] attackRange=null, Stats stats=null, (int max, int current)? hp = null, UnitBehavior behavior=null)
    {
        Unit unit = UnitScene.Instantiate<Unit>();

        unit.Grid = _dut.Cursor.Grid;
        unit.Cell = cell;
        unit.Grid.Occupants[cell] = unit;

        unit.Class = new();
        if (attackRange is not null)
            unit.AttackRange = attackRange;
        if (stats is not null)
            unit.Stats = stats;
        if (hp is not null)
        {
            unit.Health.Maximum = hp.Value.max;
            unit.Ready += () => unit.Health.Value = hp.Value.current; // Do in Ready handler because Unit inits its health to max
        }

        unit.Behavior = behavior ?? new StandBehavior();

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

    private void RunTest(AIController.DecisionType decider, IEnumerable<Unit> allies, IEnumerable<Unit> enemies, Dictionary<Unit, Vector2I> expected, string expectedAction, Unit expectedTarget=null)
    {
        _dut.Decision = decider;

        foreach (Unit ally in allies)
            _allies.AddChild(ally);
        foreach (Unit enemy in enemies)
            _enemies.AddChild(enemy);

        (Unit selected, Vector2I destination, StringName action, Unit target) = _dut.ComputeAction(_allies, _enemies);
        Assert.IsTrue(expected.Any(
            (p) => selected == p.Key && destination == p.Value),
            $"Expected to choose one of {string.Join(',', expected.Select((p) => $"{p.Key.Army.Faction.Name} unit at {p.Key.Cell} moves {p.Value}"))}; but chose {selected.Army.Faction.Name} unit at {selected.Cell} to {destination}"
        );
        Assert.AreEqual<StringName>(action, expectedAction, $"Expected action {expectedAction}, but chose {action}");
        if (expectedTarget is null)
            Assert.IsNull(target, $"Unexpected target {target?.Army.Faction.Name} unit at {target?.Cell}");
        else
            Assert.AreSame(target, expectedTarget, $"Expected to target {expectedTarget.Army.Faction.Name} unit at {expectedTarget.Cell}, but chose {target.Army.Faction.Name} unit at {target.Cell}");
    }
    private void RunTest(AIController.DecisionType decider, IEnumerable<Unit> allies, IEnumerable<Unit> enemies, Unit expectedSelected, Vector2I expectedDestination, string expectedAction, Unit expectedTarget=null)
        => RunTest(decider, allies, enemies, new() {{ expectedSelected, expectedDestination }}, expectedAction, expectedTarget);

    /// <summary><see cref="AIController.DecisionType.ClosestEnemy"/>: AI should choose its unit closest to any enemy.</summary>
    [Test]
    public void TestClosestStandingNoEnemiesInRange()
    {
        Unit[] allies = [CreateUnit(new(0, 1)), CreateUnit(new(1, 2)), CreateUnit(new(0, 3))];
        Unit[] enemies = [CreateUnit(new(6, 2))];
        RunTest(AIController.DecisionType.ClosestEnemy, allies, enemies,
            expectedSelected:    allies[1],
            expectedDestination: allies[1].Cell,
            expectedAction:      "End"
        );
    }

    /// <summary><see cref="AIController.DecisionType.TargetLoop"/>: AI should choose its unit closest to any enemy and no enemies are in range to attack.</summary>
    [Test]
    public void TestLoopStandingNoEnemiesInRange()
    {
        Unit[] allies = [CreateUnit(new(0, 1)), CreateUnit(new(1, 2)), CreateUnit(new(0, 3))];
        Unit[] enemies = [CreateUnit(new(6, 2))];
        RunTest(AIController.DecisionType.ClosestEnemy, allies, enemies,
            expectedSelected:    allies[1],
            expectedDestination: allies[1].Cell,
            expectedAction:      "End"
        );
    }

    /// <summary><see cref="AIController.DecisionType.ClosestEnemy"/>: AI should choose to attack the enemy closest to its selected unit.</summary>
    [Test]
    public void TestClosestStandingEnemiesInRange()
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

    /// <summary><see cref="AIController.DecisionType.TargetLoop"/>: AI should choose to attack the enemy with the lower HP when it deals the same damage to all enemies.</summary>
    [Test]
    public void TestLoopStandingSingleAllyMultipleEnemiesSameDamage()
    {
        Unit[] allies = [CreateUnit(new(3, 2), attackRange:[1, 2], behavior:new StandBehavior() { AttackInRange = true })];
        Unit[] enemies = [CreateUnit(new(2, 1), hp:(10, 5)), CreateUnit(new(2, 2), hp:(10, 10))];
        RunTest(AIController.DecisionType.TargetLoop, allies, enemies,
            expectedSelected:    allies[0],
            expectedDestination: allies[0].Cell,
            expectedAction:      "Attack",
            expectedTarget:      enemies[0]
        );
    }

    /// <summary><see cref="AIController.DecisionType.TargetLoop"/>: AI should choose to attack the enemy it can do more damage to when enemies have the same HP.</summary>
    [Test]
    public void TestLoopStandingSingleAllyMultipleEnemiesDifferentDamage()
    {
        Unit[] allies = [CreateUnit(new(3, 2), attackRange:[1, 2], stats:new() { Attack = 5 }, behavior:new StandBehavior() { AttackInRange = true })];
        Unit[] enemies = [CreateUnit(new(2, 1), stats:new() { Defense = 3 }), CreateUnit(new(2, 2), stats:new() { Defense = 0 })];
        RunTest(AIController.DecisionType.TargetLoop, allies, enemies,
            expectedSelected:    allies[0],
            expectedDestination: allies[0].Cell,
            expectedAction:      "Attack",
            expectedTarget:      enemies[1]
        );
    }

    /// <summary><see cref="AIController.DecisionType.TargetLoop"/>: AI should choose to attack the enemy it can bring to the lowest HP regardless of current HP or damage.</summary>
    [Test]
    public void TestLoopStandingSingleAllyMultipleEnemiesDifferentEndHealth()
    {
        Unit[] allies = [CreateUnit(new(3, 2), attackRange:[1, 2], stats:new() { Attack = 5 }, behavior:new StandBehavior() { AttackInRange = true })];
        Unit[] enemies = [CreateUnit(new(2, 1), hp:(10, 5), stats:new() { Health = 10, Defense = 3 }), CreateUnit(new(2, 2), hp:(10, 10), stats:new() { Health = 10, Defense = 0 })];
        RunTest(AIController.DecisionType.TargetLoop, allies, enemies,
            expectedSelected:    allies[0],
            expectedDestination: allies[0].Cell,
            expectedAction:      "Attack",
            expectedTarget:      enemies[0]
        );
    }

    /// <summary><see cref="AIController.DecisionType.TargetLoop"/>: AI should choose the unit that can attack the enemy, even though it's further away.</summary>
    [Test]
    public void TestLoopStandingMultipleAlliesSingleEnemyOnlyOneInRange()
    {
        Unit[] allies = [
            CreateUnit(new(2, 1), attackRange:[1], behavior:new StandBehavior { AttackInRange = true }),
            CreateUnit(new(2, 4), attackRange:[3], behavior:new StandBehavior { AttackInRange = true })
        ];
        Unit[] enemies = [CreateUnit(new(3, 2))];
        RunTest(AIController.DecisionType.TargetLoop, allies, enemies,
            expectedSelected:    allies[1],
            expectedDestination: allies[1].Cell,
            expectedAction:      "Attack",
            expectedTarget:      enemies[0]
        );
    }

    /// <summary><see cref="AIController.DecisionType.TargetLoop"/>: AI should choose the target it can kill with its units, even if one of its units can do more damage to a different one.</summary>
    [Test]
    public void TestLoopStandingMultipleAlliesMultipleEnemiesOneCanBeKilled()
    {
        Unit[] allies = [
            CreateUnit(new(0, 1), attackRange:[1, 2], stats:new() { Attack = 7 }, behavior:new StandBehavior() { AttackInRange = true }),
            CreateUnit(new(0, 2), attackRange:[1],    stats:new() { Attack = 7 }, behavior:new StandBehavior() { AttackInRange = true })
        ];
        Unit[] enemies = [
            CreateUnit(new(1, 1), stats:new() { Defense = 0 }),
            CreateUnit(new(1, 2), stats:new() { Defense = 2 })
        ];
        RunTest(AIController.DecisionType.TargetLoop, allies, enemies,
            expected:            allies.ToDictionary((u) => u, (u) => u.Cell),
            expectedAction:      "Attack",
            expectedTarget:      enemies[1]
        );
    }

    /// <summary>
    /// <see cref="AIController.DecisionType.ClosestEnemy"/>: AI should be able to choose an action when there aren't enemies. It also should keep the chosen unit in place even if that unit
    /// could move.
    /// </summary>
    [Test]
    public void TestClosestMovingNoEnemiesPresent()
    {
        Unit ally = CreateUnit(new(3, 2), behavior:new MoveBehavior());
        RunTest(AIController.DecisionType.ClosestEnemy, [ally], [],
            expectedSelected:    ally,
            expectedDestination: ally.Cell,
            expectedAction:      "End"
        );
    }

    /// <summary>
    /// <see cref="AIController.DecisionType.TargetLoop"/>: AI should be able to choose an action when there aren't enemies.  It also should keep the chosen unit in place even if that unit
    /// could move.
    // </summary>
    [Test]
    public void TestLoopMovingNoEnemiesPresent()
    {
        Unit ally = CreateUnit(new(3, 2), behavior:new MoveBehavior());
        RunTest(AIController.DecisionType.TargetLoop, [ally], [],
            expectedSelected:    ally,
            expectedDestination: ally.Cell,
            expectedAction:      "End"
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