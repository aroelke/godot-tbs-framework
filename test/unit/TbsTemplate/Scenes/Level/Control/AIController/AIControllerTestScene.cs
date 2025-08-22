using System;
using System.Collections.Generic;
using System.Linq;
using GD_NET_ScOUT;
using Godot;
using TbsTemplate.Data;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Level.Layers;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control.Test;

[Test]
public partial class AIControllerTestScene : Node
{
    private AIController _dut = null;
    private Army _allies = null, _enemies = null;
    private SpecialActionRegion _region = null;

    [Export] public PackedScene UnitScene = null;
    [Export] public PackedScene StandBehaviorScene = null;
    [Export] public PackedScene MoveBehaviorScene = null;

    /*********************
     * SETUP AND SUPPORT *
     *********************/

    private readonly record struct AIAction(Unit Selected, Vector2I[] Destinations, StringName Action, Unit Target=null)
    {
        public override string ToString() => $"move {PrintUnit(Selected)} to {string.Join('/', Destinations)} and {Action} {(Target is null ? "" : $"{PrintUnit(Target)}")}";
    }

    private static string PrintUnit(Unit unit) => $"{unit.Faction.Name}@{unit.Cell}";

    private StandBehavior CreateStandBehavior(bool attack=false, bool support=false)
    {
        StandBehavior behavior = StandBehaviorScene.Instantiate<StandBehavior>();
        behavior.AttackInRange = attack;
        behavior.SupportInRange = support;
        return behavior;
    }

    private MoveBehavior CreateMoveBehavior() => MoveBehaviorScene.Instantiate<MoveBehavior>();

    private Unit CreateUnit(Vector2I cell, int[] attack=null, int[] support=null, Stats stats=null, int? hp = null, Behavior behavior=null)
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

        unit.AddChild(behavior ?? CreateStandBehavior());

        return unit;
    }

    [BeforeAll]
    public void SetupTests()
    {
        _dut = GetNode<AIController>("Army1/AIController");
        _allies = GetNode<Army>("Army1");
        _enemies = GetNode<Army>("Army2");
        _region = GetNode<SpecialActionRegion>("Activate");
    }

    [BeforeEach]
    public void InitializeTest()
    {
        _dut.InitializeTurn();
    }

    private void RunTest(IEnumerable<Unit> allies, IEnumerable<Unit> enemies, params AIAction[] expected)
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
                    run += $"& [{string.Join(',', enemyPermutation.Select(PrintUnit))}]";
                (Unit selected, Vector2I destination, StringName action, Unit target) = _dut.ComputeAction(allyPermutation, enemyPermutation, _dut.Grid);
                AIAction result = new(selected, [destination], action, target);

                string error = $"Expected to {string.Join(" or ", expected)} but instead chose to {result}";

                Assert.IsTrue(expected.Any((a) => a.Selected == result.Selected), $"{run}: Wrong unit selected: {error}");
                Assert.IsTrue(expected.Any((a) => a.Destinations.Contains(result.Destinations[0])), $"{run}: Wrong destination selected: {error}");
                Assert.IsTrue(expected.Any((a) => a.Action == result.Action), $"{run}: Wrong action: {error}");
                if (expected.All((a) => a.Target is null) && result.Target is not null)
                    Assert.IsNull(target, $"{run}: Unexpected target: {error}");
                else
                    Assert.IsTrue(expected.Any((a) => a.Target == result.Target), $"{run}: Wrong target: {error}");
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

    /*******
     * END *
     *******/

    /// <summary>AI should choose its unit closest to any enemy and no enemies are in range to attack.</summary>
    [Test]
    public void TestEndStandingNoEnemiesInRange()
    {
        Unit[] allies = [CreateUnit(new(0, 1)), CreateUnit(new(1, 2)), CreateUnit(new(0, 3))];
        Unit[] enemies = [CreateUnit(new(6, 2))];
        RunTest(allies, enemies, [new(allies[1], [allies[1].Cell], UnitActions.EndAction)]);
    }

    /// <summary>When the behavior prevents movement, AI should not choose to attack if an enemy is reachable but not in range to attack.</summary>
    [Test]
    public void TestEndStandingOneReachableEnemyNotInRange()
    {
        Unit ally = CreateUnit(new(0, 2));
        Unit enemy = CreateUnit(new(3, 2));
        RunTest([ally], [enemy], [new(ally, [ally.Cell], UnitActions.EndAction)]);
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
                Unit ally = CreateUnit(new(i, j), behavior:CreateMoveBehavior());
                RunTest([ally], [], [new(ally, [ally.Cell], UnitActions.EndAction)]);
            }
        }
    }

    /// <summary>AI should choose the traversable cell closest to any enemy when it can't attack anything.</summary>
    [Test]
    public void TestEndMovingSingleUnreachableEnemy()
    {
        Unit ally = CreateUnit(new(0, 2), attack:[1], stats:new() { Move = 3 }, behavior:CreateMoveBehavior());
        Unit enemy = CreateUnit(new(5, 2));
        RunTest([ally], [enemy], [new(ally, [new(3, 2)], UnitActions.EndAction)]);
    }

    /**********
     * ATTACK *
     **********/

    /// <summary>AI should choose to attack the enemy with the lower HP when it deals the same damage to all enemies.</summary>
    [Test]
    public void TestAttackStandingSingleAllyMultipleEnemiesSameDamage()
    {
        Unit[] allies = [CreateUnit(new(3, 2), attack:[1, 2], behavior:CreateStandBehavior(attack:true))];
        Unit[] enemies = [CreateUnit(new(2, 1), stats:new() { Health = 10 }, hp:5), CreateUnit(new(2, 2), stats:new() { Health = 10 }, hp:10)];
        RunTest(allies, enemies, [new(allies[0], [allies[0].Cell], UnitActions.AttackAction, enemies[0])]);
    }

    /// <summary>AI should choose to attack the enemy it can do more damage to when enemies have the same HP.</summary>
    [Test]
    public void TestAttackStandingSingleAllyMultipleEnemiesDifferentDamage()
    {
        Unit[] allies = [CreateUnit(new(3, 2), attack:[1, 2], stats:new() { Attack = 5 }, behavior:CreateStandBehavior(attack:true))];
        Unit[] enemies = [CreateUnit(new(2, 1), stats:new() { Defense = 3 }), CreateUnit(new(2, 2), stats:new() { Defense = 0 })];
        RunTest(allies, enemies, [new(allies[0], [allies[0].Cell], UnitActions.AttackAction, enemies[1])]);
    }

    /// <summary>AI should choose to attack the enemy it can bring to the lowest HP regardless of current HP or damage.</summary>
    [Test]
    public void TestAttackStandingSingleAllyMultipleEnemiesDifferentEndHealth()
    {
        Unit[] allies = [CreateUnit(new(3, 2), attack:[1, 2], stats:new() { Attack = 5 }, behavior:CreateStandBehavior(attack:true))];
        Unit[] enemies = [CreateUnit(new(2, 1), stats:new() { Health = 10, Defense = 3 }, hp:5), CreateUnit(new(2, 2), stats:new() { Health = 10, Defense = 0 }, hp:10)];
        RunTest(allies, enemies, [new(allies[0], [allies[0].Cell], UnitActions.AttackAction, enemies[0])]);
    }

    /// <summary>AI should choose the unit that can attack the enemy, even though it's further away.</summary>
    [Test]
    public void TestAttackStandingMultipleAlliesSingleEnemyOnlyOneInRange()
    {
        Unit[] allies = [
            CreateUnit(new(2, 1), attack:[1], behavior:CreateStandBehavior(attack:true)),
            CreateUnit(new(2, 4), attack:[3], behavior:CreateStandBehavior(attack:true))
        ];
        Unit[] enemies = [CreateUnit(new(3, 2))];
        RunTest(allies, enemies, [new(allies[1], [allies[1].Cell], "Attack", enemies[0])]);
    }

    /// <summary>AI should choose the target it can kill with its units, even if one of its units can do more damage to a different one.</summary>
    [Test]
    public void TestAttackStandingMultipleAlliesMultipleEnemiesOneCanBeKilled()
    {
        Unit[] allies = [
            CreateUnit(new(0, 1), attack:[1, 2], stats:new() { Attack = 7 }, behavior:CreateStandBehavior(attack:true)),
            CreateUnit(new(0, 2), attack:[1],    stats:new() { Attack = 7 }, behavior:CreateStandBehavior(attack:true))
        ];
        Unit[] enemies = [
            CreateUnit(new(1, 1), stats:new() { Defense = 0 }),
            CreateUnit(new(1, 2), stats:new() { Defense = 2 })
        ];
        RunTest(allies, enemies, [.. allies.Select((u) => new AIAction(u, [u.Cell], "Attack", enemies[1]))]);
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
                    Unit ally = CreateUnit(new(i, j), attack:[1], stats:new() { Move = 5 }, behavior:CreateMoveBehavior());
                    int closest = destinations.Select((c) => c.ManhattanDistanceTo(ally.Cell)).Min();
                    RunTest([ally], [enemy], [new(ally, [.. destinations.Where((c) => c.ManhattanDistanceTo(ally.Cell) == closest)], "Attack", enemy)]);
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
                    Unit ally = CreateUnit(new(3, 2), attack:[1, 2], stats:new() { Move = 5 }, behavior:CreateMoveBehavior());
                    int closest = destinations.Select((c) => c.ManhattanDistanceTo(ally.Cell)).Min();
                    RunTest([ally], [enemy], [new(ally, [.. destinations.Where((c) => c.ManhattanDistanceTo(ally.Cell) == closest)], "Attack", enemy)]);
                }
            }
        }
    }

    /// <summary>AI should not block other allies from attacking when making ordering decisions to attack the same enemy.</summary>
    [Test]
    public void TestAttackDontBlockAlliesAttackingSameEnemy()
    {
        Unit[] allies = [
            CreateUnit(new(0, 2), attack:[1, 2], stats:new() { Move = 4 }, behavior:CreateMoveBehavior()),
            CreateUnit(new(1, 2), attack:[1, 2], stats:new() { Move = 4 }, behavior:CreateMoveBehavior())
        ];
        Unit enemy = CreateUnit(new(6, 2), stats:new() { Attack = 0 });
        RunTest(allies, [enemy], [
            new(allies[0], [new(4, 2)], "Attack", enemy),
            new(allies[1], [new(5, 2)], "Attack", enemy)
        ]);
    }

    /// <summary>AI should not block other allies from attacking when making ordering decisions to divide attacks among multiple enemies.</summary>
    [Test]
    public void TestAttackDontBlockAlliesAttackingOtherEnemy()
    {
        Unit[] allies = [
            CreateUnit(new(0, 2), attack:[1], stats:new() { Attack = 10, Move = 1 }, behavior:CreateMoveBehavior()),
            CreateUnit(new(1, 2), attack:[2], stats:new() { Attack = 5,  Move = 4 }, behavior:CreateMoveBehavior())
        ];
        Unit[] enemies = [
            CreateUnit(new(2, 2), stats:new() { Health = 10, Attack = 0, Defense = 5 }),
            CreateUnit(new(3, 2), stats:new() { Health = 10, Attack = 0, Defense = 0 })
        ];
        RunTest(allies, enemies, [new(allies[1], [new(2, 1), new(2, 4)], "Attack", enemies[1])]);
    }

    /// <summary>AI should attack from a space that its target can't retaliate from, even if it's not the furthest one.</summary>
    [Test]
    public void TestAttackMinimizeRetaliationDamageViaPositioning()
    {
        Unit ally = CreateUnit(new(1, 2), attack:[1, 2], behavior:CreateMoveBehavior());
        Unit enemy = CreateUnit(new(5, 2), attack:[2]);
        RunTest([ally], [enemy], [new(ally, [new(4, 2)], "Attack", enemy)]);
    }

    /// <summary>AI should attack in the order that reduces retaliation damage to its units.</summary>
    [Test]
    public void TestAttackMinimizeRetaliationDamageViaDeath()
    {
        Unit[] allies = [
            CreateUnit(new(0, 2), attack:[1, 2], stats:new() { Attack = 5, Move = 4 }, behavior:CreateMoveBehavior()),
            CreateUnit(new(1, 2), attack:[1, 2], stats:new() { Attack = 5, Move = 4 }, behavior:CreateMoveBehavior())
        ];
        Unit enemy = CreateUnit(new(6, 2), attack:[1], stats:new() { Health = 10, Attack = 5, Defense = 0 }, hp:10);
        RunTest(allies, [enemy], [new(allies[0], [new(4, 2)], "Attack", enemy)]);
    }

    /// <summary>AI should attack an enemy it can kill, even if it can do more damage to another enemy.</summary>
    [Test]
    public void TestAttackMaximizeEnemyDeaths()
    {
        Unit ally = CreateUnit(new(2, 2), attack:[2], stats:new() { Health = 10, Attack = 5 }, behavior:CreateMoveBehavior());
        Unit[] enemies = [
            CreateUnit(new(3, 1), attack:[1], stats:new() { Health = 5,  Defense = 4 }, hp:1),
            CreateUnit(new(3, 3), attack:[1], stats:new() { Health = 10, Defense = 0 }, hp:6)
        ];
        RunTest([ally], enemies, [new(ally, [ally.Cell], "Attack", enemies[0])]);
    }

    /// <summary>AI should attack in the order that reduces the number of allies that die in retaliation regardless of damage dealt.</summary>
    [Test]
    public void TestAttackMinimizeAllyDeaths()
    {
        Unit[] allies = [
            CreateUnit(new(0, 2), attack:[1, 2], stats:new() { Health = 10, Attack = 5, Defense = 0, Move = 4 }, hp:10, behavior:CreateMoveBehavior()),
            CreateUnit(new(1, 2), attack:[1, 2], stats:new() { Health = 10, Attack = 5, Defense = 3, Move = 4 }, hp:5, behavior:CreateMoveBehavior())
        ];
        Unit enemy = CreateUnit(new(6, 2), attack:[1, 2], stats:new() { Health = 10, Attack = 8, Defense = 0 }, hp:10);
        RunTest(allies, [enemy], [new(allies[0], [new(4, 2)], "Attack", enemy)]);
    }

    /// <summary>AI should divide attacks in such a way as to maximize kills when one of its allies can kill multiple enemies and one can't.</summary>
    [Test]
    public void TestAttackChooseCorrectKill()
    {
        Unit[] allies = [
            CreateUnit(new(2, 1), attack:[1, 2], stats:new() { Attack = 10, Move = 2 }, behavior:CreateMoveBehavior()),
            CreateUnit(new(2, 3), attack:[1, 2], stats:new() { Attack = 5,  Move = 2 }, behavior:CreateMoveBehavior())
        ];
        Unit[] enemies = [
            CreateUnit(new(4, 1), stats:new() { Health = 10, Defense = 0 }),
            CreateUnit(new(4, 3), stats:new() { Health = 5,  Defense = 0 })
        ];
        RunTest(allies, enemies, [
            new(allies[0], [allies[0].Cell], "Attack", enemies[0]),
            new(allies[1], [allies[1].Cell], "Attack", enemies[1])
        ]);
    }

    /***********
     * SUPPORT *
     ***********/

    /// <summary>AI should heal the ally with the lowest HP, even if it can heal a different ally by a greater amount.</summary>
    [Test]
    public void TestSupportStandingPreferLowestHP()
    {
        Unit[] allies = [
            CreateUnit(new(3, 2), support:[1], stats:new() { Healing = 5 }, behavior:CreateStandBehavior(support:true)),
            CreateUnit(new(2, 2), stats:new() { Health = 5 },  hp:1),
            CreateUnit(new(3, 1), stats:new() { Health = 20 }, hp:5)
        ];
        RunTest(allies, [], [new(allies[0], [allies[0].Cell], UnitActions.SupportAction, allies[1])]);
    }

    /// <summary>AI should heal the ally with the lowest HP, even if it can heal a different ally by a greater amount.</summary>
    [Test]
    public void TestSupportMovingPreferLowestHP()
    {
        Unit[] allies = [
            CreateUnit(new(3, 2), support:[1], stats:new() { Healing = 5, Move = 1 }, behavior:CreateMoveBehavior()),
            CreateUnit(new(3, 0), stats:new() { Health = 5 },  hp:1),
            CreateUnit(new(3, 4), stats:new() { Health = 20 }, hp:5)
        ];
        RunTest(allies, [], [new(allies[0], [new(3, 1)], UnitActions.SupportAction, allies[1])]);
    }

    /// <summary>AI should prefer to heal injured allies it can reach over attacking enemies it can reach.</summary>
    [Test]
    public void TestMovingPreferSupportOverAttack()
    {
        Unit[] allies = [
            CreateUnit(new(3, 2), attack:[1], support:[1], stats:new() { Attack = 5, Healing = 5, Move = 1 }, behavior:CreateMoveBehavior()),
            CreateUnit(new(3, 0), stats:new() { Health = 5 },  hp:1),
        ];
        Unit enemy = CreateUnit(new(3, 4), stats:new() { Health = 10, Defense = 0 }, hp:10);
        RunTest(allies, [enemy], [new(allies[0], [new(3, 1)], UnitActions.SupportAction, allies[1])]);
    }

    /// <summary>AI should prefer to heal injured allies it can reach over attacking enemies it can reach.</summary>
    [Test]
    public void TestStandingPreferSupportOverAttack()
    {
        Unit[] allies = [
            CreateUnit(new(3, 2), attack:[2, 4], support:[1, 2], stats:new() { Attack = 5, Healing = 5 }, behavior:CreateStandBehavior(attack:true, support:true)),
            CreateUnit(new(3, 0), stats:new() { Health = 5 },  hp:1),
        ];
        Unit enemy = CreateUnit(new(3, 4), stats:new() { Health = 10, Defense = 0 }, hp:10);
        RunTest(allies, [enemy], [new(allies[0], [allies[0].Cell], UnitActions.SupportAction, allies[1])]);
    }

    /// <summary>AI should prefer to attack enemies it can reach if there allies in reach but all of them are uninjured.</summary>
    [Test]
    public void TestMovingPreferAttackWhenAlliesUninjured()
    {
        Unit[] allies = [
            CreateUnit(new(3, 2), attack:[1], support:[1], stats:new() { Attack = 5, Healing = 5, Move = 1 }, behavior:CreateMoveBehavior()),
            CreateUnit(new(3, 0), stats:new() { Health = 5 },  hp:5),
        ];
        Unit enemy = CreateUnit(new(3, 4), stats:new() { Health = 10, Defense = 0 }, hp:10);
        RunTest(allies, [enemy], [new(allies[0], [new(3, 3)], "Attack", enemy)]);
    }

    /// <summary>AI should prefer to attack if it thinks it can defeat an enemy, even if there is an injured ally in range.</summary>
    [Test]
    public void TestStandingPreferKillOverSupport()
    {
        Unit[] allies = [
            CreateUnit(new(3, 2), attack:[1, 2], support:[1, 2], stats:new() { Attack = 5, Healing = 5 }, behavior:CreateStandBehavior(attack:true, support:true)),
            CreateUnit(new(3, 0), stats:new() { Health = 5 },  hp:1),
        ];
        Unit enemy = CreateUnit(new(3, 4), stats:new() { Health = 10, Defense = 0 }, hp:5);
        RunTest(allies, [enemy], [new(allies[0], [allies[0].Cell], "Attack", enemy)]);
    }

    /// <summary>AI should prefer to attack if it thinks it can defeat an enemy, even if there is an injured ally in range.</summary>
    [Test]
    public void TestMovingPreferKillOverSupport()
    {
        Unit[] allies = [
            CreateUnit(new(3, 2), attack:[1], support:[1], stats:new() { Attack = 5, Healing = 5, Move = 1 }, behavior:CreateMoveBehavior()),
            CreateUnit(new(3, 0), stats:new() { Health = 5 },  hp:1),
        ];
        Unit enemy = CreateUnit(new(3, 4), stats:new() { Health = 10, Defense = 0 }, hp:5);
        RunTest(allies, [enemy], [new(allies[0], [new(3, 3)], "Attack", enemy)]);
    }

    /// <summary>AI should heal after an ally attacks with retaliation to maximize amount healed, even if ally is damaged beforehand.</summary>
    [Test]
    public void TestStandingSupportAfterAttack()
    {
        Unit attacker = CreateUnit(new(3, 2), attack:[3], stats:new() { Health = 10, Attack = 5, Defense = 0 }, hp:8, behavior:CreateStandBehavior(attack:true, support:true));
        Unit healer = CreateUnit(new(5, 2), support:[2], stats:new() { Healing = 5 }, behavior:CreateStandBehavior(attack:true, support:true));
        Unit enemy = CreateUnit(new(0, 2), attack:[3], stats:new() { Attack = 5, Defense = 0 });
        RunTest([attacker, healer], [enemy], [new(attacker, [attacker.Cell], "Attack", enemy)]);
    }

    /// <summary>AI should heal after an ally attacks with retaliation to maximize amount healed, even if ally is damaged beforehand.</summary>
    [Test]
    public void TestMovingSupportAfterAttack()
    {
        Unit attacker = CreateUnit(new(3, 2), attack:[1], stats:new() { Health = 10, Attack = 5, Defense = 0, Move = 3 }, hp:8, behavior:CreateMoveBehavior());
        Unit healer = CreateUnit(new(5, 2), support:[1], stats:new() { Healing = 5, Move = 3 }, behavior:CreateMoveBehavior());
        Unit enemy = CreateUnit(new(0, 2), attack:[1], stats:new() { Attack = 5, Defense = 0 });
        RunTest([attacker, healer], [enemy], [new(attacker, [new(1, 2)], "Attack", enemy)]);
    }

    /*******************
     * SPECIAL ACTIONS *
     *******************/

    private void RunActionRegionTest(Action test)
    {
        RemoveChild(_region);
        GetNode<Grid>("Grid").AddChild(_region);

        try
        {
            test();
        }
        finally
        {
            GetNode<Grid>("Grid").RemoveChild(_region);
            AddChild(_region);
        }
    }

    /// <summary>AI should perform the special action it's standing on even if it can't move.</summary>
    [Test]
    public void TestStandingPerformSpecialAction()
    {
        Unit ally = CreateUnit(new(0, 2), behavior:CreateStandBehavior());
        RunActionRegionTest(() => RunTest([ally], [], new AIAction(ally, [ally.Cell], _region.Action)));
    }

    /// <summary>AI should stay where it is and perform the special action if it's already standing there.</summary>
    [Test]
    public void TestMovingPerformSpecialAction()
    {
        Unit ally = CreateUnit(new(0, 2), behavior:CreateMoveBehavior());
        RunActionRegionTest(() => RunTest([ally], [], new AIAction(ally, [ally.Cell], _region.Action)));
    }

    /// <summary>AI should move to the closest special action space and perform it.</summary>
    [Test]
    public void TestMoveToSpecialAction()
    {
        Unit ally = CreateUnit(new(3, 2), stats:new() { Move = 4 }, behavior:CreateMoveBehavior());
        RunActionRegionTest(() => RunTest([ally], [], new AIAction(ally, [new(0, 2)], _region.Action)));
    }

    /// <summary>AI should prefer the special action over attacking.</summary>
    [Test]
    public void TestStandingPreferSpecialActionOverAttack()
    {
        Unit ally =  CreateUnit(new(0, 2), attack:[1], stats:new() { Attack = 5 }, behavior:CreateStandBehavior(attack:true));
        Unit enemy = CreateUnit(new(1, 2), attack:[], stats:new() { Health = 5, Defense = 0 });
        RunActionRegionTest(() => RunTest([ally], [enemy], new AIAction(ally, [ally.Cell], _region.Action)));
    }

    /// <summary>AI should move around enemies to perform the special action even if it could attack.</summary>
    [Test]
    public void TestMovingPreferSpecialActionOverAttack()
    {
        Unit ally =  CreateUnit(new(2, 2), attack:[1], stats:new() { Attack = 5, Move = 5 }, behavior:CreateMoveBehavior());
        Unit enemy = CreateUnit(new(1, 2), attack:[], stats:new() { Health = 5, Defense = 0 });
        RunActionRegionTest(() => RunTest([ally], [enemy], new AIAction(ally, [new(0, 1), new(0, 3)], _region.Action)));
    }

    /// <summary>AI should prefer the special action over supporting.</summary>
    [Test]
    public void TestStandingPreferSpecialActionOverSupport()
    {
        Unit[] allies = [
            CreateUnit(new(0, 2), support:[1], stats:new() { Healing = 5 }, behavior:CreateStandBehavior(support:true)),
            CreateUnit(new(1, 2), stats:new() { Health = 5 }, hp:1)
        ];

        RunActionRegionTest(() => RunTest(allies, [], new AIAction(allies[0], [allies[0].Cell], _region.Action)));
    }

    /// <summary>AI should move through allies to perform the special action even if it could support.</summary>
    [Test]
    public void TestMovingPreferSpecialActionOverSupport()
    {
        Unit[] allies = [
            CreateUnit(new(2, 2), support:[1], stats:new() { Healing = 5, Move = 5 }, behavior:CreateMoveBehavior()),
            CreateUnit(new(1, 2), stats:new() { Health = 5 }, hp:1)
        ];

        RunActionRegionTest(() => RunTest(allies, [], new AIAction(allies[0], [new(0, 2)], _region.Action)));
    }

    /// <summary>AI should only use special action regions it can use and ignore ones its enemy use.</summary>
    [Test]
    public void TestMovingIgnoreEnemySpecialActions()
    {
        TileMapLayer noActivate = GetNode<TileMapLayer>("NoActivate");
        RemoveChild(noActivate);
        GetNode<Grid>("Grid").AddChild(noActivate);

        try
        {
            Unit ally = CreateUnit(new(3, 2), stats:new() { Move = 4 }, behavior:CreateMoveBehavior());
            RunActionRegionTest(() => RunTest([ally], [], new AIAction(ally, [new(0, 2)], _region.Action)));
        }
        finally
        {
            GetNode<Grid>("Grid").RemoveChild(noActivate);
            AddChild(noActivate);
        }
    }

    /*********
     * OTHER *
     *********/

    /// <summary>AI should not try to act with a defeated unit (mostly only applies mid-simulation of turn).</summary>
    [Test]
    public void TestDontSelectDefeatedAlly()
    {
        Unit[] allies = [
            CreateUnit(new(2, 2), attack:[1], stats:new() { Health = 5, Attack = 5 }, hp:0, behavior:CreateStandBehavior(attack:true)),
            CreateUnit(new(4, 2), attack:[1], stats:new() { Health = 5, Attack = 5 }, hp:5, behavior:CreateStandBehavior(attack:true))
        ];
        Unit enemy = CreateUnit(new(3, 2), attack:[1], stats:new() { Attack = 5 });
        RunTest(allies, [enemy], [new(allies[1], [allies[1].Cell], "Attack", enemy)]);
    }
}