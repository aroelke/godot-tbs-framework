using System.Collections.Generic;
using Godot;
using TbsFramework.Collections;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Map;

public class GridData
{
    public delegate void TerrainUpdatedEventHandler(Vector2I cell, Terrain terrain);

    private readonly ObservableDictionary<Vector2I, Terrain> _terrain = [];
    private readonly ObservableDictionary<Vector2I, GridObjectData> _occupants = [];

    public event TerrainUpdatedEventHandler TerrainUpdated;

    public Vector2I Size = Vector2I.One;

    public IDictionary<Vector2I, Terrain> Terrain => _terrain;

    public Terrain DefaultTerrain = new();

    public IDictionary<Vector2I, GridObjectData> Occupants => _occupants;

    public GridData() : base()
    {
        _terrain.ItemsAdded += (items) => {
            foreach ((Vector2I cell, Terrain terrain) in items)
                TerrainUpdated(cell, terrain);
        };
        _terrain.ItemsRemoved += (items) => {
            foreach ((Vector2I cell, _) in items)
                TerrainUpdated(cell, DefaultTerrain);
        };
        _terrain.ItemReplaced += (cell, _, @new) => TerrainUpdated(cell, @new);
    }

    public bool Contains(Vector2I cell) => cell.X >= 0 && cell.X < Size.X && cell.Y >= 0 && cell.Y < Size.Y;

    public Vector2I Clamp(Vector2I cell) => cell.Clamp(Vector2I.Zero, Size - Vector2I.One);

    public IEnumerable<Vector2I> GetCellsAtDistance(Vector2I center, int distance)
    {
        HashSet<Vector2I> cells = [];
        for (int i = 0; i < distance; i++)
        {
            Vector2I target;
            if (Contains(target = center + new Vector2I(-distance + i, -i)))
                cells.Add(target);
            if (Contains(target = center + new Vector2I(i, -distance + i)))
                cells.Add(target);
            if (Contains(target = center + new Vector2I(distance - i, i)))
                cells.Add(target);
            if (Contains(target = center + new Vector2I(-i, distance - i)))
                cells.Add(target);
        }
        return cells;
    }

    public IEnumerable<Vector2I> GetNeighbors(Vector2I cell) => GetCellsAtDistance(cell, 1);
}