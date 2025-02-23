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

    private string PrintUnit(Unit unit) => $"{unit.Army.Faction.Name}@{unit.Cell}";

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
        _dut.Cursor.Cell = Vector2I.Zero;
    }

    /// <summary>Run a test to make sure the AI performs the right action for a given game state.</summary>
    /// <param name="decider">Algorithm the AI should use to determine actions.</param>
    /// <param name="allies">Units in the AI's army.</param>
    /// <param name="enemies">Units not in the AI's army.</param>
    /// <param name="expected">Mapping of units the AI can choose onto the destination cells it should move the one it chooses.</param>
    /// <param name="expectedAction">Action the AI should perform with its chosen unit.</param>
    /// <param name="expectedTarget">Unit the AI should be targeting with its action.</param>
    private void RunTest(AIController.DecisionType decider, IEnumerable<Unit> allies, IEnumerable<Unit> enemies, Dictionary<Unit, Vector2I> expected, string expectedAction, Unit expectedTarget=null)
    {
        _dut.Decision = decider;

        foreach (Unit ally in allies)
            _allies.AddChild(ally);
        foreach (Unit enemy in enemies)
            _enemies.AddChild(enemy);

        (Unit selected, Vector2I destination, StringName action, Unit target) = _dut.ComputeAction(_allies, _enemies);

        try
        {
            Assert.IsTrue(expected.Any(
                (p) => selected == p.Key && destination == p.Value),
                $"Expected to move {string.Join('/', expected.Keys.Select(PrintUnit))}; but moved {PrintUnit(selected)} to {destination}"
            );
            Assert.AreEqual<StringName>(action, expectedAction, $"Expected action {expectedAction}, but chose {action}");
            if (expectedTarget is null)
                Assert.IsNull(target, $"Unexpected target {(target is not null ? PrintUnit(target) : "")}");
            else
                Assert.AreSame(target, expectedTarget, $"Expected to target {PrintUnit(expectedTarget)}, but chose {PrintUnit(target)}");
        }
        finally
        {
            _dut.FinalizeAction();
            _dut.FinalizeTurn();

            foreach (Unit unit in (IEnumerable<Unit>)_allies)
            {
                unit.Grid.Occupants.Remove(unit.Cell);
                _allies.RemoveChild(unit);
                unit.Free();
            }
            foreach (Unit unit in (IEnumerable<Unit>)_enemies)
            {
                unit.Grid.Occupants.Remove(unit.Cell);
                _enemies.RemoveChild(unit);
                unit.Free();
            }
        }
    }

    /// <summary>Run a test to make sure the AI performs the right action for a given game state.</summary>
    /// <param name="decider">Algorithm the AI should use to determine actions.</param>
    /// <param name="allies">Units in the AI's army.</param>
    /// <param name="enemies">Units not in the AI's army.</param>
    /// <param name="expectedSelected">Unit the AI should choose for its action.</param>
    /// <param name="expectedDestination">Cell the AI should move its unit to.</param>
    /// <param name="expectedAction">Action the AI should perform with its chosen unit.</param>
    /// <param name="expectedTarget">Unit the AI should be targeting with its action.</param>
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

    /// <summary><see cref="AIController.DecisionType.ClosestEnemy"/>: AI should choose the closest allowed destination when there are multiple options.</summary>
    [Test]
    public void TestClosestMovingSingleReachableEnemy()
    {
        Unit ally = CreateUnit(new(1, 2), attackRange:[1], stats:new() { Move = 5 }, behavior:new MoveBehavior());
        Unit enemy = CreateUnit(new(4, 2));
        RunTest(AIController.DecisionType.ClosestEnemy, [ally], [enemy],
            expectedSelected:    ally,
            expectedDestination: new(3, 2),
            expectedAction:      "Attack",
            expectedTarget:      enemy
        );
    }

    /// <summary><see cref="AIController.DecisionType.ClosestEnemy"/>: AI should choose the square at the furthest range it could attack its target.</summary>
    [Test]
    public void TestClosestMovingSingleReachableEnemyRanged()
    {
        Unit ally = CreateUnit(new(1, 2), attackRange:[1, 2], stats:new() { Move = 5 }, behavior:new MoveBehavior());
        Unit enemy = CreateUnit(new(4, 2));
        RunTest(AIController.DecisionType.ClosestEnemy, [ally], [enemy],
            expectedSelected:    ally,
            expectedDestination: new(2, 2),
            expectedAction:      "Attack",
            expectedTarget:      enemy
        );
    }

    /// <summary><see cref="AIController.DecisionType.TargetLoop"/>: AI should choose the closest allowed destination when there are multiple options.</summary>
    [Test]
    public void TestLoopMovingSingleReachableEnemy()
    {
        Unit ally = CreateUnit(new(1, 2), attackRange:[1], stats:new() { Move = 5 }, behavior:new MoveBehavior());
        Unit enemy = CreateUnit(new(4, 2));
        RunTest(AIController.DecisionType.TargetLoop, [ally], [enemy],
            expectedSelected:    ally,
            expectedDestination: new(3, 2),
            expectedAction:      "Attack",
            expectedTarget:      enemy
        );
    }

    /// <summary><see cref="AIController.DecisionType.TargetLoop"/>: AI should choose the square at the furthest range it could attack its target.</summary>
    [Test]
    public void TestLoopMovingSingleReachableEnemyRanged()
    {
        Unit ally = CreateUnit(new(1, 2), attackRange:[1, 2], stats:new() { Move = 5 }, behavior:new MoveBehavior());
        Unit enemy = CreateUnit(new(4, 2));
        RunTest(AIController.DecisionType.TargetLoop, [ally], [enemy],
            expectedSelected:    ally,
            expectedDestination: new(2, 2),
            expectedAction:      "Attack",
            expectedTarget:      enemy
        );
    }

    /// <summary><see cref="AIController.DecisionType.ClosestEnemy"/>: AI should choose the cell closest to the closest target if there are multiple targets.</summary>
    [Test]
    public void TestClosestMovingMultipleReachableEnemies()
    {
        Unit ally = CreateUnit(new(1, 2), attackRange:[1], stats:new() { Move = 5 }, behavior:new MoveBehavior());
        Unit[] enemies = [CreateUnit(new(4, 1)), CreateUnit(new(4, 2))];
        RunTest(AIController.DecisionType.ClosestEnemy, [ally], enemies,
            expectedSelected:    ally,
            expectedDestination: new(3, 2),
            expectedAction:      "Attack",
            expectedTarget:      enemies[1]
        );
    }

    /// <summary><see cref="AIController.DecisionType.ClosestEnemy"/>: AI should choose the traversable cell closest to any enemy when it can't attack anything.</summary>
    [Test]
    public void TestClosestMovingSingleUnreachableEnemy()
    {
        Unit ally = CreateUnit(new(0, 2), attackRange:[1], stats:new() { Move = 3 }, behavior:new MoveBehavior());
        Unit enemy = CreateUnit(new(5, 2));
        RunTest(AIController.DecisionType.ClosestEnemy, [ally], [enemy],
            expectedSelected:    ally,
            expectedDestination: new(3, 2),
            expectedAction:      "End"
        );
    }

    /// <summary><see cref="AIController.DecisionType.TargetLoop"/>: AI should choose the traversable cell closest to any enemy when it can't attack anything.</summary>
    [Test]
    public void TestLoopMovingSingleUnreachableEnemy()
    {
        Unit ally = CreateUnit(new(0, 2), attackRange:[1], stats:new() { Move = 3 }, behavior:new MoveBehavior());
        Unit enemy = CreateUnit(new(5, 2));
        RunTest(AIController.DecisionType.TargetLoop, [ally], [enemy],
            expectedSelected:    ally,
            expectedDestination: new(3, 2),
            expectedAction:      "End"
        );
    }
}