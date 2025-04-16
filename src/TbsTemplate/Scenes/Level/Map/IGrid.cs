using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsTemplate.Data;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Map;

/// <summary>Defines necessary functionality for maintaining the game state and performing computations based on it.</summary>
public interface IGrid
{
    protected static bool Contains(IGrid grid, Vector2I cell) => new Rect2I(Vector2I.Zero, grid.Size).HasPoint(cell);
    protected static int PathCost(IGrid grid, IEnumerable<Vector2I> path) => path.Select((c) => grid.GetTerrain(c).Cost).Sum();

    protected static IEnumerable<Vector2I> GetCellsAtDistance(IGrid grid, Vector2I cell, int distance)
    {
        HashSet<Vector2I> cells = [];
        for (int i = 0; i < distance; i++)
        {
            Vector2I target;
            if (grid.Contains(target = cell + new Vector2I(-distance + i, -i)))
                cells.Add(target);
            if (grid.Contains(target = cell + new Vector2I(i, -distance + i)))
                cells.Add(target);
            if (grid.Contains(target = cell + new Vector2I(distance - i, i)))
                cells.Add(target);
            if (grid.Contains(target = cell + new Vector2I(-i, distance - i)))
                cells.Add(target);
        }
        return cells;
    }

    // List of the four cardinal directions
    public static readonly Vector2I[] Directions = [Vector2I.Up, Vector2I.Right, Vector2I.Down, Vector2I.Left];

    /// <summary>Grid dimensions. Both elements should be positive.</summary>
    public Vector2I Size { get; }

    /// <summary>Check if a cell offset is in the grid.</summary>
    /// <param name="cell">offset to check.</param>
    /// <returns><c>true</c> if the <paramref name="cell"/> is within the grid bounds, and <c>false</c> otherwise.</returns>
    public bool Contains(Vector2I cell);

    public bool IsTraversable(Vector2I cell, Faction faction);

    /// <returns>The terrain information for a cell, or <see cref="DefaultTerrain"/> if the terrain hasn't been set.</returns>
    public Terrain GetTerrain(Vector2I cell);

    public IImmutableDictionary<Vector2I, IUnit> GetOccupantUnits();

    /// <summary>
    /// Compute the total cost of a collection of cells. If the cells are a contiguous path, represents the total cost of moving along that
    /// path.
    /// </summary>
    /// <param name="path">List of cells to sum up.</param>
    /// <returns>The sum of the cost of each cell in the <paramref name="path"/>.</returns>
    public int PathCost(IEnumerable<Vector2I> path);

    /// <summary>Find all the cells that are exactly a specified Manhattan distance away from a center cell.</summary>
    /// <param name="cell">Cell at the center of the range.</param>
    /// <param name="distance">Distance away from the center cell to search.</param>
    /// <returns>A collection of cells that are on the grid and exactly the specified <paramref name="distance"/> away from the center <paramref name="cell"/>.</returns>
    public IEnumerable<Vector2I> GetCellsAtDistance(Vector2I cell, int distance);
}