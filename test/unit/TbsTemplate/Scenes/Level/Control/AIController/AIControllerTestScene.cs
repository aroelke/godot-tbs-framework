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

    private string PrintUnit(Unit unit) => $"{unit.Army.Faction.Name}@{unit.Cell}";

    private Unit CreateUnit(Vector2I cell, int[] attackRange=null, Stats stats=null, (int max, int current)? hp = null, UnitBehavior behavior=null)
    {
        Unit unit = UnitScene.Instantiate<Unit>();

        unit.Grid = _dut.Grid;
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
            foreach (IEnumerable<Unit> allyPermutation in allies.Permutations())
            {
                foreach (IEnumerable<Unit> enemyPermutation in enemies.Permutations())
                {
                    string run = $"[{string.Join(',', allyPermutation.Select(PrintUnit))}] & [{string.Join(',', enemyPermutation.Select(PrintUnit))}]";
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

    /// <summary>AI should choose its unit closest to any enemy and no enemies are in range to attack.</summary>
    [Test]
    public void TestDupStandingNoEnemiesInRange()
    {
        Unit[] allies = [CreateUnit(new(0, 1)), CreateUnit(new(1, 2)), CreateUnit(new(0, 3))];
        Unit[] enemies = [CreateUnit(new(6, 2))];
        RunTest(allies, enemies,
            expectedSelected:    allies[1],
            expectedDestinations: [allies[1].Cell],
            expectedAction:      "End"
        );
    }

    /// <summary>AI should choose to attack the enemy with the lower HP when it deals the same damage to all enemies.</summary>
    [Test]
    public void TestDupStandingSingleAllyMultipleEnemiesSameDamage()
    {
        Unit[] allies = [CreateUnit(new(3, 2), attackRange:[1, 2], behavior:new StandBehavior() { AttackInRange = true })];
        Unit[] enemies = [CreateUnit(new(2, 1), hp:(10, 5)), CreateUnit(new(2, 2), hp:(10, 10))];
        RunTest(allies, enemies,
            expectedSelected:    allies[0],
            expectedDestinations: [allies[0].Cell],
            expectedAction:      "Attack",
            expectedTarget:      enemies[0]
        );
    }

    /// <summary>AI should choose to attack the enemy it can do more damage to when enemies have the same HP.</summary>
    [Test]
    public void TestDupStandingSingleAllyMultipleEnemiesDifferentDamage()
    {
        Unit[] allies = [CreateUnit(new(3, 2), attackRange:[1, 2], stats:new() { Attack = 5 }, behavior:new StandBehavior() { AttackInRange = true })];
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
    public void TestDupStandingSingleAllyMultipleEnemiesDifferentEndHealth()
    {
        Unit[] allies = [CreateUnit(new(3, 2), attackRange:[1, 2], stats:new() { Attack = 5 }, behavior:new StandBehavior() { AttackInRange = true })];
        Unit[] enemies = [CreateUnit(new(2, 1), hp:(10, 5), stats:new() { Health = 10, Defense = 3 }), CreateUnit(new(2, 2), hp:(10, 10), stats:new() { Health = 10, Defense = 0 })];
        RunTest(allies, enemies,
            expectedSelected:    allies[0],
            expectedDestinations: [allies[0].Cell],
            expectedAction:      "Attack",
            expectedTarget:      enemies[0]
        );
    }

    /// <summary>AI should choose the unit that can attack the enemy, even though it's further away.</summary>
    [Test]
    public void TestDupStandingMultipleAlliesSingleEnemyOnlyOneInRange()
    {
        Unit[] allies = [
            CreateUnit(new(2, 1), attackRange:[1], behavior:new StandBehavior { AttackInRange = true }),
            CreateUnit(new(2, 4), attackRange:[3], behavior:new StandBehavior { AttackInRange = true })
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
    public void TestDupStandingMultipleAlliesMultipleEnemiesOneCanBeKilled()
    {
        Unit[] allies = [
            CreateUnit(new(0, 1), attackRange:[1, 2], stats:new() { Attack = 7 }, behavior:new StandBehavior() { AttackInRange = true }),
            CreateUnit(new(0, 2), attackRange:[1],    stats:new() { Attack = 7 }, behavior:new StandBehavior() { AttackInRange = true })
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

    /// <summary>AI should be able to choose an action when there aren't enemies.  It also should keep the chosen unit in place even if that unit could move.</summary>
    [Test]
    public void TestDupMovingNoEnemiesPresent()
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

    /// <summary>AI should choose the closest allowed destination when there are multiple options.</summary>
    [Test]
    public void TestDupMovingSingleReachableEnemy()
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
                    Unit ally = CreateUnit(new(i, j), attackRange:[1], stats:new() { Move = 5 }, behavior:new MoveBehavior());
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
    public void TestDupMovingSingleReachableEnemyRanged()
    {
        Vector2I[] destinations = [new(4, 0), new(5, 1), new(6, 2), new(5, 3), new(4, 4), new(3, 3), new(2, 2), new(3, 1), new(4, 1), new(5, 2), new(4, 3), new(3, 2)];
        Vector2I size = GetNode<Grid>("Grid").Size;
        for (int i = 0; i < size.X; i++)
        {
            for (int j = 0; j < size.Y; j++)
            {
                Unit enemy = CreateUnit(new(4, 2), attackRange:[]);
                if (new Vector2I(i, j) != enemy.Cell)
                {
                    Unit ally = CreateUnit(new(3, 2), attackRange:[1, 2], stats:new() { Move = 5 }, behavior:new MoveBehavior());
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

    /// <summary>AI should choose the traversable cell closest to any enemy when it can't attack anything.</summary>
    [Test]
    public void TestDupMovingSingleUnreachableEnemy()
    {
        Unit ally = CreateUnit(new(0, 2), attackRange:[1], stats:new() { Move = 3 }, behavior:new MoveBehavior());
        Unit enemy = CreateUnit(new(5, 2));
        RunTest([ally], [enemy],
            expectedSelected:    ally,
            expectedDestinations: [new(3, 2)],
            expectedAction:      "End"
        );
    }

    /// <summary>AI should not block other allies from attacking when making ordering decisions.</summary>
    [Test]
    public void TestDupDontBlockAllies()
    {
        Unit[] allies = [
            CreateUnit(new(0, 2), attackRange:[1, 2], stats:new() { Move = 4 }, behavior:new MoveBehavior()),
            CreateUnit(new(1, 2), attackRange:[1, 2], stats:new() { Move = 4 }, behavior:new MoveBehavior())
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
    public void TestDupMinimizeRetaliationDamageViaPositioning()
    {
        Unit ally = CreateUnit(new(1, 2), attackRange:[1, 2], behavior:new MoveBehavior());
        Unit enemy = CreateUnit(new(5, 2), attackRange:[2]);
        RunTest([ally], [enemy],
            expectedSelected: ally,
            expectedDestinations: [new(4, 2)],
            expectedAction: "Attack",
            expectedTarget: enemy
        );
    }

    /// <summary>AI should attack in the order that reduces retaliation damage to its units.</summary>
    [Test]
    public void TestDupMinimizeRetaliationDamageViaDeath()
    {
        Unit[] allies = [
            CreateUnit(new(0, 2), attackRange:[1, 2], stats:new() { Attack = 5, Move = 4 }, behavior:new MoveBehavior()),
            CreateUnit(new(1, 2), attackRange:[1, 2], stats:new() { Attack = 5, Move = 4 }, behavior:new MoveBehavior())
        ];
        Unit enemy = CreateUnit(new(6, 2), attackRange:[1], stats:new() { Health = 10, Attack = 5, Defense = 0 }, hp:(10, 10));
        RunTest(allies, [enemy],
            expectedSelected: allies[0],
            expectedDestinations: [new(4, 2)],
            expectedAction: "Attack",
            expectedTarget: enemy
        );
    }

    /// <summary>AI should attack an enemy it can kill, even if it can do more damage to another enemy.</summary>
    [Test]
    public void TestDupMaximizeEnemyDeaths()
    {
        Unit ally = CreateUnit(new(2, 2), attackRange:[2], stats:new() { Health = 10, Attack = 5 }, behavior:new MoveBehavior());
        Unit[] enemies = [
            CreateUnit(new(3, 1), attackRange:[1], stats:new() { Health = 10, Defense = 4 }, hp:(5, 1)),
            CreateUnit(new(3, 3), attackRange:[1], stats:new() { Health = 10, Defense = 0 }, hp:(10, 6))
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
    public void TestDupMinimizeAllyDeaths()
    {
        Unit[] allies = [
            CreateUnit(new(0, 2), attackRange:[1, 2], stats:new() { Health = 10, Attack = 3, Defense = 0, Move = 4 }, hp:(10, 10), behavior:new MoveBehavior()),
            CreateUnit(new(1, 2), attackRange:[1, 2], stats:new() { Health = 10, Attack = 3, Defense = 3, Move = 4 }, hp:(10, 5), behavior:new MoveBehavior())
        ];
        Unit enemy = CreateUnit(new(6, 2), attackRange:[1, 2], stats:new() { Health = 10, Attack = 8, Defense = 0 }, hp:(10, 10));
        RunTest(allies, [enemy],
            expectedSelected: allies[0],
            expectedDestinations: [new(4, 2)],
            expectedAction: "Attack",
            expectedTarget: enemy
        );
    }
}