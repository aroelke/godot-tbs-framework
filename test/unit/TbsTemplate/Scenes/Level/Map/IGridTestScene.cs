using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using GD_NET_ScOUT;
using Godot;
using TbsTemplate.Data;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Map.Test;

[Test]
public partial class IGridTestScene : Node
{
    private readonly record struct TestGrid(Vector2I Size, IDictionary<Vector2I, Terrain> Terrain) : IGrid
    {
        // Functions to test
        public bool Contains(Vector2I cell) => IGrid.Contains(this, Size);
        public IEnumerable<Vector2I> GetCellsAtDistance(Vector2I cell, int distance) => IGrid.GetCellsAtDistance(this, cell, distance);
        public int PathCost(IEnumerable<Vector2I> path) => IGrid.PathCost(this, path);

        // Supporting functions
        public Terrain GetTerrain(Vector2I cell) => Terrain.TryGetValue(cell, out Terrain terrain) ? terrain : null;

        // Not used for testing
        public IImmutableDictionary<Vector2I, IUnit> GetOccupantUnits() => throw new NotImplementedException();
        public bool IsTraversable(Vector2I cell, Faction faction) => throw new NotImplementedException();
    }
}