using Godot;
using System;
using System.Collections.Generic;
using ui;

namespace battle;

/// <summary>Represents the battle map, containing its terrain and managing units and obstacles on it.</summary>
[Tool]
public partial class BattleMap : TileMap
{
    private Camera2D _camera;
    private readonly Dictionary<Vector2I, Unit> _units = new();

    private Camera2D Camera => _camera ??= GetNode<Camera2D>("Pointer/Camera");

    /// <summary>Grid dimensions. Both elements should be positive.</summary>
    [Export] public Vector2I Size { get; private set; } = Vector2I.Zero;

    /// <summary>Grid cell dimensions derived from the tile set.  If there is no tileset, the size is zero.</summary>
    public Vector2I CellSize => TileSet?.TileSize ?? Vector2I.Zero;

    /// <summary>Find the cell index closest to the given one inside the grid.</summary>
    /// <param name="position">Cell index to clamp.
    /// <returns>The cell index clamped to be inside the grid bounds using <c>Vector2I.Clamp</c></returns>
    public Vector2I Clamp(Vector2I position) => position.Clamp(Vector2I.Zero, Size - Vector2I.One);

    /// <summary>Constrain a position to somewhere within the grid (not necessarily snapped to a cell).</summary>
    /// <param name="position">Position to clamp.</param>
    /// <returns>The world position clamped to be inside the grid using <c>Vector2.Clamp</c></returns>
    public Vector2 Clamp(Vector2 position) => position.Clamp(Vector2.Zero, Size*CellSize - Vector2.One);

    /// <summary>Find the position in pixels of a cell offset.</summary>
    /// <param name="offset">Cell offset to use for calculation (can be outside grid bounds).</param>
    /// <returns>The position, in pixels of the upper-left corner of the grid cell.</returns>
    public Vector2I PositionOf(Vector2I offset) => offset*CellSize;

    /// <summary>Find the cell offset of a pixel position.</summary>
    /// <param name="pixels">Position in pixels.</param>
    /// <returns>The coordinates of the cell containing the pixel point (can be outside grid bounds).</returns>
    public Vector2I CellOf(Vector2 point) => (Vector2I)(point/CellSize);

    /// <summary>Only enable smooth scrolling when the mouse is used for control.</summary>
    /// <param name="mode">Cursor input mode being switched to.</param>
    public void OnInputModeChanged(InputMode mode)
    {
        Camera.PositionSmoothingEnabled = mode == InputMode.Mouse;
    }

    /// <summary>Act on the selected cell.</summary>
    /// <param name="cell">Cell to select.</param>
    public void OnCellCelected(Vector2I cell)
    {
        if (_units.ContainsKey(cell))
            _units[cell].IsSelected = !_units[cell].IsSelected;
    }

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
            (Camera.LimitTop, Camera.LimitLeft) = Vector2I.Zero;
            (Camera.LimitRight, Camera.LimitBottom) = Size*CellSize;

            _units.Clear();
            foreach (Node child in GetChildren())
                if (child is Unit unit)
                    _units[unit.Cell] = unit;
        }
    }
}