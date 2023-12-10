using System;
using System.Collections.Generic;
using Godot;

namespace level.map;

/// <summary>Represents the environment on which a level is played, defining its grid dimensions, tiles, and terrain.</summary>
[Tool]
public partial class LevelMap : TileMap
{
    /// <summary><c>TileSet</c> layer containing ground tiles.</summary>
    public int GroundLayer { get; private set; } = -1;

    /// <summary><c>TileSet</c> layer containing terrain tiles. This is the layer that will be used to define terrain effects.</summary>
    public int TerrainLayer { get; private set; } = -1;

    /// <summary>Default terrain to use when it isn't placed explicitly on the map.</summary>
    [Export] public Terrain DefaultTerrain = null;

    /// <summary>Grid cell dimensions derived from the tile set.  If there is no tileset, the size is zero.</summary>
    public Vector2 CellSize => TileSet?.TileSize ?? Vector2.Zero;

    /// <summary>Grid dimensions. Both elements should be positive.</summary>
    public Vector2I Size => GetUsedRect().End;

    /// <summary>Check if a cell offset is in the grid.</summary>
    /// <param name="offset">offset to check.</param>
    /// <returns><c>true</c> if the offset is within the grid bounds, and <c>false</c> otherwise.</returns>
    public bool Contains(Vector2I offset) => new Rect2I(Vector2I.Zero, Size).HasPoint(offset);

    /// <summary>Find the cell offset closest to the given one inside the grid.</summary>
    /// <param name="cell">Cell offset to clamp.
    /// <returns>The cell offset clamped to be inside the grid bounds using <c>Vector2I.Clamp</c></returns>
    public Vector2I Clamp(Vector2I offset) => offset.Clamp(Vector2I.Zero, Size - Vector2I.One);

    /// <summary>Find the position in pixels of a cell offset.</summary>
    /// <param name="offset">Cell offset to use for calculation (can be outside grid bounds).</param>
    /// <returns>The position, in pixels of the upper-left corner of the grid cell.</returns>
    public Vector2 PositionOf(Vector2I offset) => offset*CellSize;

    /// <summary>Find the cell offset of a pixel position.</summary>
    /// <param name="position">Position in world pixels.</param>
    /// <returns>The coordinates of the cell containing the pixel point (can be outside grid bounds).</returns>
    public Vector2I CellOf(Vector2 position) => (Vector2I)(position/CellSize);

    /// <summary>Snap a position to a grid cell.</summary>
    /// <param name="position">Position in world pixels.</param>
    /// <returns>The position of the upper-left corner of the cell containing the given position.</returns>
    public Vector2 Snap(Vector2 position) => PositionOf(CellOf(position));

    /// <returns>The terrain information for a cell, or <c>DefaultTerrain</c> if the terrain hasn't been set.</returns>
    /// <exception cref="IndexOutOfRangeException">If the cell is outside the grid.</exception>
    public Terrain GetTerrain(Vector2I cell) => GetCellTileData(TerrainLayer, cell)?.GetCustomData("terrain").As<Terrain>() ?? DefaultTerrain;

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        // Don't use Rect property here because it needs to update with the tilemap as it's edited
        int ground = -1;
        for (int i = 0; i < GetLayersCount(); i++)
            if (GetLayerName(i) == "ground")
                ground = i;
        if (ground == -1)
            warnings.Add("No ground layer");
        else
        {
            for (int i = 0; i < GetUsedRect().End.X; i++)
            {
                for (int j = 0; j < GetUsedRect().End.Y; j++)
                {
                    Vector2I cell = new(i, j);
                    if (!GetUsedCells(ground).Contains(cell))
                        warnings.Add($"Missing ground tile at {cell}");
                }
            }
        }
        return warnings.ToArray();
    }

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
        {
            for (int i = 0; i < GetLayersCount(); i++)
            {
                if (GetLayerName(i) == "ground")
                    GroundLayer = i;
                if (GetLayerName(i) == "terrain")
                    TerrainLayer = i;
            }
        }
    }
}