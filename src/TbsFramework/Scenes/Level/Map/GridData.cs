using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Collections;
using TbsFramework.Data;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Level.Layers;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Map;

/// <summary>Data structure for tracking information about the map and the objects on it.</summary>
public class GridData
{
    /// <summary>Handler for changes in the grid's size.</summary>
    /// <param name="old">Size of the grid before the change.</param>
    /// <param name="new">Size of the grid after the change.</param>
    public delegate void SizeUpdatedEventHandler(Vector2I old, Vector2I @new);

    /// <summary>Handler for changes in a cell's terrain.</summary>
    /// <param name="cell">Cell where the terrain was changed.</param>
    /// <param name="old">Terrain before the change.</param>
    /// <param name="new">Terrain after the change.</param>
    public delegate void TerrainUpdatedEventHandler(Vector2I cell, Terrain old, Terrain @new);

    private readonly ObservableProperty<Vector2I> _size = new(Vector2I.One);
    private readonly ObservableDictionary<Vector2I, Terrain> _terrain = [];
    private readonly ObservableDictionary<Vector2I, UnitData> _occupants = [];

    private GridData(GridData original) : this()
    {
        _size = original._size;
        DefaultTerrain = original.DefaultTerrain;
        foreach ((Vector2I cell, Terrain terrain) in original.Terrain)
            _terrain[cell] = terrain;
        foreach ((Vector2I cell, UnitData occupant) in original._occupants)
        {
            _occupants[cell] = occupant.Clone();
            _occupants[cell].Grid = this;
        }
        foreach (SpecialActionRegionData region in original.SpecialActionRegions)
            SpecialActionRegions.Add(region.Clone());
    }

    /// <summary>
    /// Signals that the grid's size has been updated. If the grid shrinks, <see cref="GridObjectData"/>s that are now outside the grid
    /// are not moved. The handler should handle that.
    /// </summary>
    public event ObservableProperty<Vector2I>.ValueChangedEventHandler SizeUpdated
    {
        add    => _size.ValueChanged += value;
        remove => _size.ValueChanged -= value;
    }

    /// <summary>Signals that the terrain of a cell has been changed.</summary>
    public event TerrainUpdatedEventHandler TerrainUpdated;

    /// <summary>Size of the grid in cells.</summary>
    public Vector2I Size
    {
        get => _size.Value;
        set => _size.Value = value;
    }

    /// <summary>Terrain of the grid cells. This array is sparse, so only cells whose terrain isn't <see cref="DefaultTerrain"/> are present.</summary>
    public IDictionary<Vector2I, Terrain> Terrain => _terrain;

    /// <summary>Terrain to use for cells not present in <see cref="Terrain"/>.</summary>
    public Terrain DefaultTerrain = new();

    /// <summary>Objects occupying the grid. Only one object tracked by this can be in a grid cell at a time.</summary>
    public IDictionary<Vector2I, UnitData> Occupants => _occupants;

    /// <summary>Regions identifying special actions that units can perform.</summary>
    public readonly List<SpecialActionRegionData> SpecialActionRegions = [];

    public GridData()
    {
        _terrain.ItemsAdded += (items) => {
            if (TerrainUpdated is not null)
                foreach ((Vector2I cell, Terrain terrain) in items)
                    TerrainUpdated(cell, DefaultTerrain, terrain);
        };
        _terrain.ItemsRemoved += (items) => {
            if (TerrainUpdated is not null)
                foreach ((Vector2I cell, Terrain removed) in items)
                    TerrainUpdated(cell, removed, DefaultTerrain);
        };
        _terrain.ItemReplaced += (cell, old, @new) => { if (TerrainUpdated is not null) TerrainUpdated(cell, old, @new); };
    }

    /// <returns><c>true</c> if <paramref name="cell"/> is inside the grid bounds, and <c>false</c> otherwise.</returns>
    public bool Contains(Vector2I cell) => cell.X >= 0 && cell.X < Size.X && cell.Y >= 0 && cell.Y < Size.Y;

    /// <summary>Clamp a cell value to be within the grid bounds.</summary>
    public Vector2I Clamp(Vector2I cell) => cell.Clamp(Vector2I.Zero, Size - Vector2I.One);

    /// <returns>A collection containing the indices of all cells within grid bounds that are exactly the the given distance from a center cell.</returns>
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

    /// <returns>A collection containing the indices of all cells adjacent to a given cell.</returns>
    public IEnumerable<Vector2I> GetNeighbors(Vector2I cell) => GetCellsAtDistance(cell, 1);

    /// <returns>A collection containing the indices of all cells within grid bounds at each distance from a center cell.</returns>
    public IEnumerable<Vector2I> GetCellsInRange(Vector2I center, IEnumerable<int> distances) => distances.SelectMany((r) => GetCellsAtDistance(center, r)).ToHashSet();

    /// <summary>Calculate the total movement cost of a collection of cells on the grid.</summary>
    public int PathCost(IEnumerable<Vector2I> path) => path.Sum((c) => Terrain.GetValueOrDefault(c, DefaultTerrain).Cost);

    /// <returns>A new grid of the same size with copies of all of the objects and other structures on it.</returns>
    public GridData Clone() => new(this);
}