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

    public Vector2I Clamp(Vector2I cell) => cell.Clamp(Vector2I.Zero, Size - Vector2I.One);
}