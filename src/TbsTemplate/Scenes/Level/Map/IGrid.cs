using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TbsTemplate.Scenes.Level.Map;

/// <summary>Defines necessary functionality for maintaining the game state and performing computations based on it.</summary>
public interface IGrid
{
    protected static bool Contains(IGrid grid, Vector2I cell) => new Rect2I(Vector2I.Zero, grid.Size).HasPoint(cell);
    protected static int PathCost(IGrid grid, IEnumerable<Vector2I> path) => path.Select((c) => grid.GetTerrain(c).Cost).Sum();

    /// <summary>Grid dimensions. Both elements should be positive.</summary>
    public Vector2I Size { get; }

    /// <summary>Check if a cell offset is in the grid.</summary>
    /// <param name="cell">offset to check.</param>
    /// <returns><c>true</c> if the <paramref name="cell"/> is within the grid bounds, and <c>false</c> otherwise.</returns>
    public bool Contains(Vector2I cell);

    /// <returns>The terrain information for a cell, or <see cref="DefaultTerrain"/> if the terrain hasn't been set.</returns>
    public Terrain GetTerrain(Vector2I cell);

    /// <summary>
    /// Compute the total cost of a collection of cells. If the cells are a contiguous path, represents the total cost of moving along that
    /// path.
    /// </summary>
    /// <param name="path">List of cells to sum up.</param>
    /// <returns>The sum of the cost of each cell in the <paramref name="path"/>.</returns>
    public int PathCost(IEnumerable<Vector2I> path);
}