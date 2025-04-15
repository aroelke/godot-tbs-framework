using Godot;

namespace TbsTemplate.Scenes.Level.Map;

/// <summary>Defines necessary functionality for maintaining the game state and performing computations based on it.</summary>
public interface IGrid
{
    protected static bool Contains(IGrid grid, Vector2I cell) => new Rect2I(Vector2I.Zero, grid.Size).HasPoint(cell);

    /// <summary>Grid dimensions. Both elements should be positive.</summary>
    public Vector2I Size { get; }

    /// <summary>Check if a cell offset is in the grid.</summary>
    /// <param name="cell">offset to check.</param>
    /// <returns><c>true</c> if the <paramref name="cell"/> is within the grid bounds, and <c>false</c> otherwise.</returns>
    public bool Contains(Vector2I cell);

    /// <returns>The terrain information for a cell, or <see cref="DefaultTerrain"/> if the terrain hasn't been set.</returns>
    public Terrain GetTerrain(Vector2I cell);
}