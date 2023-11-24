using Godot;
using System;
using System.Collections.Generic;
using System.Transactions;

namespace battle;

/// <summary>Represents the battle map, containing its terrain and managing units and obstacles on it.</summary>
[Tool]
public partial class BattleMap : TileMap
{
    /// <summary>Grid dimensions. Both elements should be positive.</summary>
    [Export] public Vector2I Size { get; private set; } = Vector2I.Zero;

    /// <summary>Color to draw the grid bounds in the editor.</summary>
    [Export] public Color GridColor { get; private set; } = Colors.Black;

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

    public override void _Draw()
    {
        base._Draw();
        if (Engine.IsEditorHint())
            DrawRect(new Rect2I(Vector2I.Zero, Size*(TileSet?.TileSize ?? Vector2I.Zero)), GridColor, filled:false);
    }
}
