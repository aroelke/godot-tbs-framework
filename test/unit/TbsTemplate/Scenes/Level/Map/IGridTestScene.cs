using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using GD_NET_ScOUT;
using Godot;
using TbsTemplate.Data;
using TbsTemplate.Scenes.Level.Layers;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Map.Test;

[Test]
public partial class IGridTestScene : Node
{
    private readonly record struct TestGrid(Vector2I Size, Dictionary<Vector2I, Terrain> Terrain) : IGrid
    {
        // Functions to test
        public bool Contains(Vector2I cell) => IGrid.Contains(this, cell);
        public IEnumerable<Vector2I> GetCellsAtDistance(Vector2I cell, int distance) => IGrid.GetCellsAtDistance(this, cell, distance);
        public int PathCost(IEnumerable<Vector2I> path) => IGrid.PathCost(this, path);

        // Supporting functions
        public Terrain GetTerrain(Vector2I cell) => Terrain.TryGetValue(cell, out Terrain terrain) ? terrain : new() { Cost = 1 };

        // Not used for testing
        public IImmutableDictionary<Vector2I, IUnit> GetOccupantUnits() => throw new NotImplementedException();
        public bool IsTraversable(Vector2I cell, Faction faction) => throw new NotImplementedException();
        public IEnumerable<ISpecialActionRegion> GetSpecialActionRegions() => throw new NotImplementedException();
    }

    // "Contains" tests
    [Test] public void TestGridContainsZero() => Assert.IsTrue(new TestGrid(Vector2I.One, []).Contains(Vector2I.Zero));
    [Test] public void TestGridContainsEnd() => Assert.IsTrue(new TestGrid(new(3, 5), []).Contains(new(2, 4)));
    [Test] public void TestGridDoesntContainMinusOne() => Assert.IsFalse(new TestGrid(Vector2I.One, []).Contains(-Vector2I.One));
    [Test] public void TestGridDoesntContainSize() => Assert.IsFalse(new TestGrid(new(3, 5), []).Contains(new(3, 5)));

    // "GetCellsAtDistance" tests
    private static bool CollectionsEqual<T>(IEnumerable<T> a, IEnumerable<T> b) => a.Count() == b.Count() && a.ToHashSet().SetEquals(b);

    [Test] public void TestGridGetCellsAtDistanceOneFromCenter()
    {
        TestGrid dut = new(new(7, 7), []);
        Vector2I target = new(3, 3);
        Vector2I[] expected = [new(3, 2), new(4, 3), new(3, 4), new(2, 3)];
        Assert.IsTrue(CollectionsEqual(dut.GetCellsAtDistance(target, 1), expected));
    }

    [Test] public void TestGridGetCellsAtDistanceOneFromLeftEdge()
    {
        TestGrid dut = new(new(7, 7), []);
        Vector2I target = new(0, 3);
        Vector2I[] expected = [new(0, 2), new(1, 3), new(0, 4)];
        Assert.IsTrue(CollectionsEqual(dut.GetCellsAtDistance(target, 1), expected));
    }

    [Test] public void TestGridGetCellsAtDistanceOneFromTopEdge()
    {
        TestGrid dut = new(new(7, 7), []);
        Vector2I target = new(3, 0);
        Vector2I[] expected = [new(4, 0), new(3, 1), new(2, 0)];
        Assert.IsTrue(CollectionsEqual(dut.GetCellsAtDistance(target, 1), expected));
    }

    [Test] public void TestGridGetCellsAtDistanceOneFromRightEdge()
    {
        TestGrid dut = new(new(7, 7), []);
        Vector2I target = new(6, 3);
        Vector2I[] expected = [new(6, 2), new(6, 4), new(5, 3)];
        Assert.IsTrue(CollectionsEqual(dut.GetCellsAtDistance(target, 1), expected));
    }

    [Test] public void TestGridGetCellsAtDistanceOneFromBottomEdge()
    {
        TestGrid dut = new(new(7, 7), []);
        Vector2I target = new(3, 6);
        Vector2I[] expected = [new(3, 5), new(4, 6), new(2, 6)];
        Assert.IsTrue(CollectionsEqual(dut.GetCellsAtDistance(target, 1), expected));
    }

    [Test] public void TestGridGetCellsAtDistanceTwoFromCenter()
    {
        TestGrid dut = new(new(7, 7), []);
        Vector2I target = new(3, 3);
        Vector2I[] expected = [new(3, 1), new(4, 2), new(5, 3), new(4, 4), new(3, 5), new(2, 4), new(1, 3), new(2, 2)];
        Assert.IsTrue(CollectionsEqual(dut.GetCellsAtDistance(target, 2), expected));
    }

    [Test] public void TestGridGetCellsAtDistanceThreeFromCenter()
    {
        TestGrid dut = new(new(7, 7), []);
        Vector2I target = new(3, 3);
        Vector2I[] expected = [new(3, 0), new(4, 1), new(5, 2), new(6, 3), new(5, 4), new(4, 5), new(3, 6), new(2, 5), new(1, 4), new(0, 3), new(1, 2), new(2, 1)];
        Assert.IsTrue(CollectionsEqual(dut.GetCellsAtDistance(target, 3), expected));
    }

    // "PathCost" tests
    [Test] public void TestGridPathCostEmpty() => Assert.Equals(new TestGrid(new(5, 5), []).PathCost([]), 0);
    [Test] public void TestGridPathNoTerrain() => Assert.Equals(new TestGrid(new(5, 5), []).PathCost([new(0, 2), new(1, 2), new(1, 3)]), 3);
    [Test] public void TestGridPathWithTerrain() => Assert.Equals(new TestGrid(new(5, 5), new() {
        { new(0, 2), new() { Cost = 3 } },
        { new(1, 2), new() { Cost = 2 } },
        { new(1, 3), new() { Cost = 5 } },
        { new(0, 0), new() { Cost = 9 } }
    }).PathCost([new(0, 2), new(1, 2), new(1, 3)]), 10);
}