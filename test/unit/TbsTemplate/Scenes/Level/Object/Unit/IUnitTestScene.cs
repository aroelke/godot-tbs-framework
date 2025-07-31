using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using GD_NET_ScOUT;
using Godot;
using TbsTemplate.Data;
using TbsTemplate.Scenes.Level.Layers;
using TbsTemplate.Scenes.Level.Map;

namespace TbsTemplate.Scenes.Level.Object.Test;

[Test]
public partial class IUnitTestScene : Node
{
    private readonly record struct TestGrid(Vector2I Size, Dictionary<Vector2I, Terrain> Terrain, Dictionary<Vector2I, TestUnit> Occupants) : IGrid
    {
        public bool Contains(Vector2I cell) => IGrid.Contains(this, cell);
        public IEnumerable<Vector2I> GetCellsAtDistance(Vector2I cell, int distance) => IGrid.GetCellsAtDistance(this, cell, distance);
        public int PathCost(IEnumerable<Vector2I> path) => IGrid.PathCost(this, path);
        public Terrain GetTerrain(Vector2I cell) => Terrain.TryGetValue(cell, out Terrain terrain) ? terrain : new() { Cost = 1 };
        public IImmutableDictionary<Vector2I, IUnit> GetOccupantUnits() => Occupants.ToImmutableDictionary((e) => e.Key, (e) => (IUnit)e.Value);
        public bool IsTraversable(Vector2I cell, Faction faction) => !Occupants.TryGetValue(cell, out TestUnit occupant) || occupant.Faction.AlliedTo(faction);

        public IEnumerable<ISpecialActionRegion> GetSpecialActionRegions() => throw new NotImplementedException();
    }

    private readonly record struct TestUnit(Stats Stats, Faction Faction, Vector2I Cell) : IUnit
    {
        // Functions to test
        public IEnumerable<Vector2I> TraversableCells(IGrid grid) => IUnit.TraversableCells(this, grid);

        // Not used in testing
        public int Health  => 0;
        public IEnumerable<Vector2I> AttackableCells(IGrid grid, IEnumerable<Vector2I> sources) => throw new NotImplementedException();
        public IEnumerable<Vector2I> SupportableCells(IGrid grid, IEnumerable<Vector2I> sources) => throw new NotImplementedException();
    }

    private readonly TestGrid _grid = new(new(7, 7), [], []);

    private static bool CollectionsEqual<T>(IEnumerable<T> a, IEnumerable<T> b) => a.Count() == b.Count() && a.ToHashSet().SetEquals(b);

    [Export] public Faction[] AlliedFactions = [];

    [Export] public Faction EnemyFaction = null;

    private void TestTraversibleCells(IEnumerable<Vector2I> expected, IEnumerable<Vector2I> actual) => Assert.IsTrue(CollectionsEqual(actual, expected), $"[{string.Join(',', actual)}] != [{string.Join(',', expected)}]");

    [Test] public void TestUnitTraversibleCellsCenterNoTerrain()
    {
        TestUnit dut = new(new() { Move = 1 }, AlliedFactions[0], new(3, 3));
        TestGrid grid = _grid with { Occupants = new() {{ dut.Cell, dut }} };
        TestTraversibleCells(
            [new(3, 3), new(3, 2), new(4, 3), new(3, 4), new(2, 3)],
            dut.TraversableCells(grid)
        );
    }

    [Test] public void TestUnitTraversibleCellsCornerNoTerrain()
    {
        TestUnit dut = new(new() { Move = 1 }, AlliedFactions[0], Vector2I.Zero);
        TestGrid grid = _grid with { Occupants = new() {{ dut.Cell, dut }} };
        TestTraversibleCells(
            [Vector2I.Zero, new(1, 0), new(0, 1)],
            dut.TraversableCells(grid)
        );
    }

    [Test] public void TestUnitTraversibleCellsCenterWithTerrain()
    {
        TestUnit dut = new(new() { Move = 2 }, AlliedFactions[0], new(3, 3));
        TestGrid grid = _grid with { Terrain = new() {{ new(3, 2), new() { Cost = 2 } }}, Occupants = new() {{ dut.Cell, dut }} };
        TestTraversibleCells(
            [new(3, 3), new(3, 2), new(4, 3), new(3, 4), new(2, 3), new(4, 2), new(5, 3), new(4, 4), new(3, 5), new(2, 4), new(2, 2), new(1, 3)],
            dut.TraversableCells(grid)
        );
    }

    [Test] public void TestUnitTraversibleCellsCenterEnemyObstacle()
    {
        TestUnit dut = new(new() { Move = 2 }, AlliedFactions[0], new(3, 3));
        TestUnit enemy = new(new(), EnemyFaction, new(2, 3));
        TestGrid grid = _grid with { Occupants = new() {{ dut.Cell, dut }, { enemy.Cell, enemy }} };
        TestTraversibleCells(
            [new(3, 3), new(3, 2), new(4, 3), new(3, 4), new(3, 1), new(4, 2), new(5, 3), new(4, 4), new(3, 5), new(2, 4), new(2, 2)],
            dut.TraversableCells(grid)
        );
    }

    [Test] public void TestUnitTraversibleCellsCenterAllyObstacle()
    {
        TestUnit dut = new(new() { Move = 2 }, AlliedFactions[0], new(3, 3));
        TestUnit ally = new(new(), AlliedFactions[1], new(2, 3));
        TestGrid grid = _grid with { Occupants = new() {{ dut.Cell, dut }, { ally.Cell, ally }} };
        TestTraversibleCells(
            [new(3, 3), new(3, 2), new(4, 3), new(3, 4), new(2, 3), new(3, 1), new(4, 2), new(5, 3), new(4, 4), new(3, 5), new(2, 4), new(2, 2), new(1, 3)],
            dut.TraversableCells(grid)
        );
    }
}