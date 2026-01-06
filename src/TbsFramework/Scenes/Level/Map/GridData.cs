using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Collections;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Level.Layers;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Map;

public class GridData
{
    public delegate void SizeUpdatedEventHandler(Vector2I size);
    public delegate void TerrainUpdatedEventHandler(Vector2I cell, Terrain terrain);

    /// <returns>
    /// A copy of <paramref name="original"/> with copies of its occupants and regions. The terrain map is copied, but the terrain
    /// references inside are not.
    /// </returns>
    public static GridData Copy(GridData original) => new(original);

    private Vector2I _size = Vector2I.One;
    private readonly ObservableDictionary<Vector2I, Terrain> _terrain = [];
    private readonly ObservableDictionary<Vector2I, GridObjectData> _occupants = [];

    public event SizeUpdatedEventHandler SizeUpdated;
    public event TerrainUpdatedEventHandler TerrainUpdated;

    public Vector2I Size
    {
        get => _size;
        set
        {
            if (_size != value)
            {
                _size = value;
                if (SizeUpdated is not null)
                    SizeUpdated(_size);
                foreach ((_, GridObjectData occupant) in new Dictionary<Vector2I, GridObjectData>(Occupants))
                    occupant.Cell = Clamp(occupant.Cell);
            }
        }
    }

    public IDictionary<Vector2I, Terrain> Terrain => _terrain;

    public Terrain DefaultTerrain = new();

    public IDictionary<Vector2I, GridObjectData> Occupants => _occupants;

    public readonly List<SpecialActionRegionData> SpecialActionRegions = [];

    public GridData()
    {
        _terrain.ItemsAdded += (items) => {
            if (TerrainUpdated is not null)
                foreach ((Vector2I cell, Terrain terrain) in items)
                    TerrainUpdated(cell, terrain);
        };
        _terrain.ItemsRemoved += (items) => {
            if (TerrainUpdated is not null)
                foreach ((Vector2I cell, _) in items)
                    TerrainUpdated(cell, DefaultTerrain);
        };
        _terrain.ItemReplaced += (cell, _, @new) => { if (TerrainUpdated is not null) TerrainUpdated(cell, @new); };
    }

    private GridData(GridData original) : this()
    {
        _size = original._size;
        DefaultTerrain = original.DefaultTerrain;
        foreach ((Vector2I cell, Terrain terrain) in original.Terrain)
            _terrain[cell] = terrain;
        foreach ((Vector2I cell, GridObjectData occupant) in original._occupants)
        {
            _occupants[cell] = occupant.Clone();
            _occupants[cell].Grid = this;
        }
        foreach (SpecialActionRegionData region in original.SpecialActionRegions)
            SpecialActionRegions.Add(region.Clone());
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

    public IEnumerable<Vector2I> GetCellsInRange(Vector2I center, IEnumerable<int> distances) => distances.SelectMany((r) => GetCellsAtDistance(center, r)).ToHashSet();

    public IList<StringName> GetSpecialActions(Vector2I cell) => [.. SpecialActionRegions.Where((r) => r.Cells.Contains(cell)).Select((r) => r.Action)];

    public int PathCost(IEnumerable<Vector2I> path) => path.Sum((c) => Terrain.GetValueOrDefault(c, DefaultTerrain).Cost);

    public GridData Clone() => new(this);
}