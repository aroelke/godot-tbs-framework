using Godot;
using System;
using System.Collections.Generic;

namespace battle;

/// <summary>Represents the battle map, containing its terrain and managing units and obstacles on it.</summary>
[Tool]
public partial class BattleMap : TileMap
{
    /// <summary>Grid dimensions. Both elements should be positive.</summary>
    [Export] public Vector2I Size { get; private set; } = Vector2I.Zero;

    public Vector2I CellSize => TileSet?.TileSize ?? Vector2I.Zero;

    /// <summary>Find the cell index closest to the given one inside the grid.</summary>
    /// <returns>The cell index clamped to be inside the grid bounds using <c>Vector2I.Clamp</c></returns>
    public Vector2I Clamp(Vector2I position) => position.Clamp(Vector2I.Zero, Size - Vector2I.One);

    /// <summary>Find the position in pixels of a cell offset.</summary>
    /// <param name="offset">Cell offset to use for calculation (can be outside grid bounds).</param>
    /// <returns>The position, in pixels of the upper-left corner of the grid cell.</returns>
    public Vector2I PositionOf(Vector2I offset) => offset*CellSize;

    /// <summary>Find the cell offset of a pixel position.</summary>
    /// <param name="pixels">Position in pixels.</param>
    /// <returns>The coordinates of the cell containing the pixel point (can be outside grid bounds).</returns>
    public Vector2I CellOf(Vector2 point) => (Vector2I)(point/CellSize);

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        // Size dimensions should be nonnegative
        if (Size.X <= 0 || Size.Y <= 0)
            warnings.Add($"Grid size {Size} has illegal dimensions.");

        // Tiles should be within the grid
        for (int i = 0; i < GetLayersCount(); i++)
            foreach (Vector2I cell in GetUsedCells(i))
                if (cell.X < 0 || cell.X >= Size.X || cell.Y < 0 || cell.Y >= Size.Y)
                    warnings.Add($"There is a tile on layer {GetLayerName(i)} placed outside the grid bounds at {cell}");

        return warnings.ToArray();
    }

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
        {
            Camera2D camera = GetNode<Camera2D>("VirtualMouse/Camera");
            (camera.LimitTop, camera.LimitLeft) = Vector2I.Zero;
            (camera.LimitRight, camera.LimitBottom) = Size*CellSize;
        }
    }
}