using System.Collections.Generic;
using System.Linq;
using GD_NET_ScOUT;
using Godot;
using TbsTemplate.Data;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Level.Control.Behavior;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control.Test;

[Test]
public partial class AIControllerTestScene : Node
{
    private AIController _dut = null;
    private Army _allies = null, _enemies = null;

    [Export] public PackedScene UnitScene = null;

    /*********************
     * SETUP AND SUPPORT *
     *********************/

    private string PrintUnit(Unit unit) => $"{unit.Army.Faction.Name}@{unit.Cell}";

    private Unit CreateUnit(Vector2I cell, int[] attack=null, int[] support=null, Stats stats=null, int? hp = null, UnitBehavior behavior=null)
    {
        Unit unit = UnitScene.Instantiate<Unit>();

        unit.Grid = _dut.Grid;
        unit.Cell = cell;
        unit.Grid.Occupants[cell] = unit;

        unit.Class = new();
        unit.AttackRange = attack ?? [];
        unit.SupportRange = support ?? [];
        if (stats is not null)
            unit.Stats = stats;
        if (hp is not null)
            unit.Ready += () => unit.Health.Value = hp.Value; // Do in Ready handler because Unit inits its health to max

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

    /// <summary>Run a test to make sure the AI performs the right action for a given game state.</summary>
    /// <param name="allies">Units in the AI's army.</param>
    /// <param name="enemies">Units not in the AI's army.</param>
    /// <param name="expected">Mapping of units the AI can choose onto the options for destination cell it should move the one it chooses.</param>
    /// <param name="expectedAction">Action the AI should perform with its chosen unit.</param>
    /// <param name="expectedTarget">Unit the AI should be targeting with its action.</param>
    private void RunTest(IEnumerable<Unit> allies, IEnumerable<Unit> enemies, Dictionary<Unit, HashSet<Vector2I>> expected, string expectedAction, Unit expectedTarget=null)
    {
        foreach (Unit ally in allies)
            _allies.AddChild(ally);
        foreach (Unit enemy in enemies)
            _enemies.AddChild(enemy);

        try
        {
            void TestPermutation(IEnumerable<Unit> allyPermutation, IEnumerable<Unit> enemyPermutation)
            {
                string run = $"[{string.Join(',', allyPermutation.Select(PrintUnit))}]";
                    if (enemyPermutation.Any())
                        run +=  $"& [{string.Join(',', enemyPermutation.Select(PrintUnit))}]";
                (Unit selected, Vector2I destination, StringName action, Unit target) = _dut.ComputeAction(allyPermutation, enemyPermutation, _dut.Grid);

                Assert.IsTrue(
                    expected.Any((p) => selected == p.Key && p.Value.Contains(destination)),
                    $"{run}: Expected to move {string.Join('/', expected.Select((e) => $"{PrintUnit(e.Key)}->[{string.Join('/', e.Value)}]"))}; but moved {PrintUnit(selected)} to {destination}"
                );
                Assert.AreEqual<StringName>(action, expectedAction, $"{run}: Expected action {expectedAction}, but chose {action}");
                if (expectedTarget is null)
                    Assert.IsNull(target, $"{run}: Unexpected target {(target is not null ? PrintUnit(target) : "")}");
                else
                    Assert.AreSame(target, expectedTarget, $"{run}: Expected to target {PrintUnit(expectedTarget)}, but chose {PrintUnit(target)}");
            }

            foreach (IEnumerable<Unit> allyPermutation in allies.Permutations())
            {
                if (enemies.Any())
                {
                    foreach (IEnumerable<Unit> enemyPermutation in enemies.Permutations())
                        TestPermutation(allyPermutation, enemyPermutation);
                }
                else
                    TestPermutation(allyPermutation, []);
            }
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
    /// <param name="allies">Units in the AI's army.</param>
    /// <param name="enemies">Units not in the AI's army.</param>
    /// <param name="expectedSelected">Unit the AI should choose for its action.</param>
    /// <param name="expectedDestinations">Options for cell the AI should move its unit to.</param>
    /// <param name="expectedAction">Action the AI should perform with its chosen unit.</param>
    /// <param name="expectedTarget">Unit the AI should be targeting with its action.</param>
    private void RunTest(IEnumerable<Unit> allies, IEnumerable<Unit> enemies, Unit expectedSelected, HashSet<Vector2I> expectedDestinations, string expectedAction, Unit expectedTarget=null)
        => RunTest(allies, enemies, new() {{ expectedSelected, expectedDestinations }}, expectedAction, expectedTarget);

    /*******
     * END *
     *******/

    /// <summary>AI should choose its unit closest to any enemy and no enemies are in range to attack.</summary>
    [Test]
    public void TestEndStandingNoEnemiesInRange()
    {
        Unit[] allies = [CreateUnit(new(0, 1)), CreateUnit(new(1, 2)), CreateUnit(new(0, 3))];
        Unit[] enemies = [CreateUnit(new(6, 2))];
        RunTest(allies, enemies,
            expectedSelected:    allies[1],
            expectedDestinations: [allies[1].Cell],
            expectedAction:      "End"
        );
    }

    /// <summary>When the behavior prevents movement, AI should not choose to attack if an enemy is reachable but not in range to attack.</summary>
    [Test]
    public void TestEndStandingOneReachableEnemyNotInRange()
    {
        Unit ally = CreateUnit(new(0, 2));
        Unit enemy = CreateUnit(new(3, 2));
        RunTest([ally], [enemy],
            expectedSelected:    ally,
            expectedDestinations: [ally.Cell],
            expectedAction:      "End"
        );
    }

    /// <summary>AI should be able to choose an action when there aren't enemies.  It also should keep the chosen unit in place even if that unit could move.</summary>
    [Test]
    public void TestEndMovingNoEnemiesPresent()
    {
        Vector2I size = GetNode<Grid>("Grid").Size;
        for (int i = 0; i < size.X; i++)
        {
            for (int j = 0; j < size.Y; j++)
            {
                Unit ally = CreateUnit(new(i, j), behavior:new MoveBehavior());
                RunTest([ally], [],
                    expectedSelected:    ally,
                    expectedDestinations: [ally.Cell],
                    expectedAction:      "End"
                );
            }
        }
    }

    /// <summary>AI should choose the traversable cell closest to any enemy when it can't attack anything.</summary>
    [Test]
    public void TestEndMovingSingleUnreachableEnemy()
    {
        Unit ally = CreateUnit(new(0, 2), attack:[1], stats:new() { Move = 3 }, behavior:new MoveBehavior());
        Unit enemy = CreateUnit(new(5, 2));
        RunTest([ally], [enemy],
            expectedSelected:    ally,
            expectedDestinations: [new(3, 2)],
            expectedAction:      "End"
        );
    }

    /**********
     * ATTACK *
     **********/

    /// <summary>AI should choose to attack the enemy with the lower HP when it deals the same damage to all enemies.</summary>
    [Test]
    public void TestAttackStandingSingleAllyMultipleEnemiesSameDamage()
    {
        Unit[] allies = [CreateUnit(new(3, 2), attack:[1, 2], behavior:new StandBehavior() { AttackInRange = true })];
        Unit[] enemies = [CreateUnit(new(2, 1), stats:new() { Health = 10 }, hp:5), CreateUnit(new(2, 2), stats:new() { Health = 10 }, hp:10)];
        RunTest(allies, enemies,
            expectedSelected:    allies[0],
            expectedDestinations: [allies[0].Cell],
            expectedAction:      "Attack",
            expectedTarget:      enemies[0]
        );
    }

    /// <summary>AI should choose to attack the enemy it can do more damage to when enemies have the same HP.</summary>
    [Test]
    public void TestAttackStandingSingleAllyMultipleEnemiesDifferentDamage()
    {
        Unit[] allies = [CreateUnit(new(3, 2), attack:[1, 2], stats:new() { Attack = 5 }, behavior:new StandBehavior() { AttackInRange = true })];
        Unit[] enemies = [CreateUnit(new(2, 1), stats:new() { Defense = 3 }), CreateUnit(new(2, 2), stats:new() { Defense = 0 })];
        RunTest(allies, enemies,
            expectedSelected:    allies[0],
            expectedDestinations: [allies[0].Cell],
            expectedAction:      "Attack",
            expectedTarget:      enemies[1]
        );
    }

    /// <summary>AI should choose to attack the enemy it can bring to the lowest HP regardless of current HP or damage.</summary>
    [Test]
    public void TestAttackStandingSingleAllyMultipleEnemiesDifferentEndHealth()
    {
        Unit[] allies = [CreateUnit(new(3, 2), attack:[1, 2], stats:new() { Attack = 5 }, behavior:new StandBehavior() { AttackInRange = true })];
        Unit[] enemies = [CreateUnit(new(2, 1), stats:new() { Health = 10, Defense = 3 }, hp:5), CreateUnit(new(2, 2), stats:new() { Health = 10, Defense = 0 }, hp:10)];
        RunTest(allies, enemies,
            expectedSelected:    allies[0],
            expectedDestinations: [allies[0].Cell],
            expectedAction:      "Attack",
            expectedTarget:      enemies[0]
        );
    }

    /// <summary>AI should choose the unit that can attack the enemy, even though it's further away.</summary>
    [Test]
    public void TestAttackStandingMultipleAlliesSingleEnemyOnlyOneInRange()
    {
        Unit[] allies = [
            CreateUnit(new(2, 1), attack:[1], behavior:new StandBehavior { AttackInRange = true }),
            CreateUnit(new(2, 4), attack:[3], behavior:new StandBehavior { AttackInRange = true })
        ];
        Unit[] enemies = [CreateUnit(new(3, 2))];
        RunTest(allies, enemies,
            expectedSelected:    allies[1],
            expectedDestinations: [allies[1].Cell],
            expectedAction:      "Attack",
            expectedTarget:      enemies[0]
        );
    }

    /// <summary>AI should choose the target it can kill with its units, even if one of its units can do more damage to a different one.</summary>
    [Test]
    public void TestAttackStandingMultipleAlliesMultipleEnemiesOneCanBeKilled()
    {
        Unit[] allies = [
            CreateUnit(new(0, 1), attack:[1, 2], stats:new() { Attack = 7 }, behavior:new StandBehavior() { AttackInRange = true }),
            CreateUnit(new(0, 2), attack:[1],    stats:new() { Attack = 7 }, behavior:new StandBehavior() { AttackInRange = true })
        ];
        Unit[] enemies = [
            CreateUnit(new(1, 1), stats:new() { Defense = 0 }),
            CreateUnit(new(1, 2), stats:new() { Defense = 2 })
        ];
        RunTest(allies, enemies,
            expected:            allies.ToDictionary((u) => u, (u) => new HashSet<Vector2I>() { u.Cell }),
            expectedAction:      "Attack",
            expectedTarget:      enemies[1]
        );
    }

    /// <summary>AI should choose the closest allowed destination when there are multiple options.</summary>
    [Test]
    public void TestAttackMovingSingleReachableEnemy()
    {
        Vector2I[] destinations = [new(4, 1), new(5, 2), new(4, 3), new(3, 2)];
        Vector2I size = GetNode<Grid>("Grid").Size;
        for (int i = 0; i < size.X; i++)
        {
            for (int j = 0; j < size.Y; j++)
            {
                Unit enemy = CreateUnit(new(4, 2));
                if (new Vector2I(i, j) != enemy.Cell)
                {
                    Unit ally = CreateUnit(new(i, j), attack:[1], stats:new() { Move = 5 }, behavior:new MoveBehavior());
                    int closest = destinations.Select((c) => c.ManhattanDistanceTo(ally.Cell)).Min();
                    RunTest([ally], [enemy],
                        expectedSelected:    ally,
                        expectedDestinations: [.. destinations.Where((c) => c.ManhattanDistanceTo(ally.Cell) == closest)],
                        expectedAction:      "Attack",
                        expectedTarget:      enemy
                    );
                }
                else
                {
                    enemy.Grid.Occupants.Remove(enemy.Cell);
                    enemy.Free();
                }
            }
        }
    }

    /// <summary>AI should choose the closest cell it can attack from, even if it's not the furthest and even if it doesn't have to move, when the enemy can't retaliate.</summary>
    [Test]
    public void TestAttackMovingSingleReachableEnemyRanged()
    {
        Vector2I[] destinations = [new(4, 0), new(5, 1), new(6, 2), new(5, 3), new(4, 4), new(3, 3), new(2, 2), new(3, 1), new(4, 1), new(5, 2), new(4, 3), new(3, 2)];
        Vector2I size = GetNode<Grid>("Grid").Size;
        for (int i = 0; i < size.X; i++)
        {
            for (int j = 0; j < size.Y; j++)
            {
                Unit enemy = CreateUnit(new(4, 2), attack:[]);
                if (new Vector2I(i, j) != enemy.Cell)
                {
                    Unit ally = CreateUnit(new(3, 2), attack:[1, 2], stats:new() { Move = 5 }, behavior:new MoveBehavior());
                    int closest = destinations.Select((c) => c.ManhattanDistanceTo(ally.Cell)).Min();
                    RunTest([ally], [enemy],
                        expectedSelected:    ally,
                        expectedDestinations: [.. destinations.Where((c) => c.ManhattanDistanceTo(ally.Cell) == closest)],
                        expectedAction:      "Attack",
                        expectedTarget:      enemy
                    );
                }
            }
        }
    }

    /// <summary>AI should not block other allies from attacking when making ordering decisions.</summary>
    [Test]
    public void TestAttackDontBlockAllies()
    {
        Unit[] allies = [
            CreateUnit(new(0, 2), attack:[1, 2], stats:new() { Move = 4 }, behavior:new MoveBehavior()),
            CreateUnit(new(1, 2), attack:[1, 2], stats:new() { Move = 4 }, behavior:new MoveBehavior())
        ];
        Unit enemy = CreateUnit(new(6, 2), stats:new() { Attack = 0 });
        RunTest(allies, [enemy],
            expected: new() {{ allies[0], new HashSet<Vector2I>() { new(4, 2) }}, { allies[1], new HashSet<Vector2I>() { new(5, 2) }}},
            expectedAction: "Attack",
            expectedTarget: enemy
        );
    }

    /// <summary>AI should attack from a space that its target can't retaliate from, even if it's not the furthest one.</summary>
    [Test]
    public void TestAttackMinimizeRetaliationDamageViaPositioning()
    {
        Unit ally = CreateUnit(new(1, 2), attack:[1, 2], behavior:new MoveBehavior());
        Unit enemy = CreateUnit(new(5, 2), attack:[2]);
        RunTest([ally], [enemy],
            expectedSelected: ally,
            expectedDestinations: [new(4, 2)],
            expectedAction: "Attack",
            expectedTarget: enemy
        );
    }

    /// <summary>AI should attack in the order that reduces retaliation damage to its units.</summary>
    [Test]
    public void TestAttackMinimizeRetaliationDamageViaDeath()
    {
        Unit[] allies = [
            CreateUnit(new(0, 2), attack:[1, 2], stats:new() { Attack = 5, Move = 4 }, behavior:new MoveBehavior()),
            CreateUnit(new(1, 2), attack:[1, 2], stats:new() { Attack = 5, Move = 4 }, behavior:new MoveBehavior())
        ];
        Unit enemy = CreateUnit(new(6, 2), attack:[1], stats:new() { Health = 10, Attack = 5, Defense = 0 }, hp:10);
        RunTest(allies, [enemy],
            expectedSelected: allies[0],
            expectedDestinations: [new(4, 2)],
            expectedAction: "Attack",
            expectedTarget: enemy
        );
    }

    /// <summary>AI should attack an enemy it can kill, even if it can do more damage to another enemy.</summary>
    [Test]
    public void TestAttackMaximizeEnemyDeaths()
    {
        Unit ally = CreateUnit(new(2, 2), attack:[2], stats:new() { Health = 10, Attack = 5 }, behavior:new MoveBehavior());
        Unit[] enemies = [
            CreateUnit(new(3, 1), attack:[1], stats:new() { Health = 5,  Defense = 4 }, hp:1),
            CreateUnit(new(3, 3), attack:[1], stats:new() { Health = 10, Defense = 0 }, hp:6)
        ];
        RunTest([ally], enemies,
            expectedSelected: ally,
            expectedDestinations: [ally.Cell],
            expectedAction: "Attack",
            expectedTarget: enemies[0]
        );
    }

    /// <summary>AI should attack in the order that reduces the number of allies that die in retaliation regardless of damage dealt.</summary>
    [Test]
    public void TestAttackMinimizeAllyDeaths()
    {
        Unit[] allies = [
            CreateUnit(new(0, 2), attack:[1, 2], stats:new() { Health = 10, Attack = 5, Defense = 0, Move = 4 }, hp:10, behavior:new MoveBehavior()),
            CreateUnit(new(1, 2), attack:[1, 2], stats:new() { Health = 10, Attack = 5, Defense = 3, Move = 4 }, hp:5, behavior:new MoveBehavior())
        ];
        Unit enemy = CreateUnit(new(6, 2), attack:[1, 2], stats:new() { Health = 10, Attack = 8, Defense = 0 }, hp:10);
        RunTest(allies, [enemy],
            expectedSelected: allies[0],
            expectedDestinations: [new(4, 2)],
            expectedAction: "Attack",
            expectedTarget: enemy
        );
    }

    /***********
     * SUPPORT *
     ***********/

    /// <summary>AI should heal the ally with the lowest HP, even if it can heal a different ally by a greater amount.</summary>
    [Test]
    public void TestSupportStandingPreferLowestHP()
    {
        Unit[] allies = [
            CreateUnit(new(3, 2), support:[1], stats:new() { Healing = 5 }, behavior:new StandBehavior() { SupportInRange = true }),
            CreateUnit(new(2, 2), stats:new() { Health = 5 },  hp:1),
            CreateUnit(new(3, 1), stats:new() { Health = 20 }, hp:5)
        ];
        RunTest(allies, [],
            expectedSelected: allies[0],
            expectedDestinations: [allies[0].Cell],
            expectedAction: "Support",
            expectedTarget: allies[1]
        );
    }

    /// <summary>AI should heal the ally with the lowest HP, even if it can heal a different ally by a greater amount.</summary>
    [Test]
    public void TestSupportMovingPreferLowestHP()
    {
        Unit[] allies = [
            CreateUnit(new(3, 2), support:[1], stats:new() { Healing = 5, Move = 1 }, behavior:new MoveBehavior()),
            CreateUnit(new(3, 0), stats:new() { Health = 5 },  hp:1),
            CreateUnit(new(3, 4), stats:new() { Health = 20 }, hp:5)
        ];
        RunTest(allies, [],
            expectedSelected: allies[0],
            expectedDestinations: [new(3, 1)],
            expectedAction: "Support",
            expectedTarget: allies[1]
        );
    }

    /// <summary>AI should prefer to heal injured allies it can reach over attacking enemies it can reach.</summary>
    [Test]
    public void TestPreferSupportOverAttack()
    {
        Unit[] allies = [
            CreateUnit(new(3, 2), attack:[1], support:[1], stats:new() { Attack = 5, Healing = 5, Move = 1 }, behavior:new MoveBehavior()),
            CreateUnit(new(3, 0), stats:new() { Health = 5 },  hp:1),
        ];
        Unit enemy = CreateUnit(new(3, 4), stats:new() { Health = 10, Defense = 0 }, hp:10);
        RunTest(allies, [enemy],
            expectedSelected: allies[0],
            expectedDestinations: [new(3, 1)],
            expectedAction: "Support",
            expectedTarget: allies[1]
        );
    }

    /// <summary>AI should prefer to attack enemies it can reach if there allies in reach but all of them are uninjured.</summary>
    [Test]
    public void TestPreferAttackWhenAlliesUninjured()
    {
        Unit[] allies = [
            CreateUnit(new(3, 2), attack:[1], support:[1], stats:new() { Attack = 5, Healing = 5, Move = 1 }, behavior:new MoveBehavior()),
            CreateUnit(new(3, 0), stats:new() { Health = 5 },  hp:5),
        ];
        Unit enemy = CreateUnit(new(3, 4), stats:new() { Health = 10, Defense = 0 }, hp:10);
        RunTest(allies, [enemy],
            expectedSelected: allies[0],
            expectedDestinations: [new(3, 3)],
            expectedAction: "Attack",
            expectedTarget: enemy
        );
    }

    /// <summary>AI should prefer to attack if it thinks it can defeat an enemy, even if there is an injured ally in range.</summary>
    [Test]
    public void TestPreferKillOverSupport()
    {
        Unit[] allies = [
            CreateUnit(new(3, 2), attack:[1], support:[1], stats:new() { Attack = 5, Healing = 5, Move = 1 }, behavior:new MoveBehavior()),
            CreateUnit(new(3, 0), stats:new() { Health = 5 },  hp:1),
        ];
        Unit enemy = CreateUnit(new(3, 4), stats:new() { Health = 10, Defense = 0 }, hp:5);
        RunTest(allies, [enemy],
            expectedSelected: allies[0],
            expectedDestinations: [new(3, 3)],
            expectedAction: "Attack",
            expectedTarget: enemy
        );
    }
}