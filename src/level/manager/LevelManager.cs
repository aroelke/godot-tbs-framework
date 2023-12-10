using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using level.map;

namespace level.manager;

/// <summary>
/// Manages the setup of and objects inside a level and provides them information about it.  Is "global" in a way in that
/// all objects in the level may be able to see it and request information from it, but each level has its own manager.
/// </summary>
[Tool]
public partial class LevelManager : Node2D
{
    private LevelMap _map;
    private Camera2D _camera = null;

    private LevelMap Map => _map ??= GetNode<LevelMap>("LevelMap");
    private Camera2D Camera => _camera ??= GetNode<Camera2D>("PointerProjection/LevelCamera");

    /// <summary>The size of the level's grid.</summary>
    public Vector2I GridSize => Map.Size;

    /// <summary>Size of the grid cells in world pixels.</summary>
    public Vector2 CellSize => Map.CellSize;

    /// <summary>Find the cell offset closest to the given one inside the grid.</summary>
    /// <param name="cell">Cell offset to clamp.
    /// <returns>The cell offset clamped to be inside the grid bounds using <c>Vector2I.Clamp</c></returns>
    public Vector2I Clamp(Vector2I cell) => Map.Clamp(cell);

    /// <summary>Find the cell containing a pixel position.</summary>
    /// <param name="position">Position in world pixels.</param>
    /// <returns>The coordinates of the cell containing the pixel point (can be outside grid bounds).</returns>
    public Vector2I CellOf(Vector2 position) => Map.CellOf(position);

    /// <summary>Find the position in world pixels of a cell.</summary>
    /// <param name="cell">Cell to use for calculation (can be outside grid bounds).</param>
    /// <returns>The position, in pixels of the upper-left corner of the grid cell.</returns>
    public Vector2 PositionOf(Vector2I cell) => Map.PositionOf(cell);

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        int maps = GetChildren().Where((c) => c is LevelMap).Count();
        if (maps < 1)
            warnings.Add("Level does not contain a map.");
        else if (maps > 1)
            warnings.Add($"Level contains too many maps ({maps}).");

        return warnings.ToArray();
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            (Camera.LimitTop, Camera.LimitLeft) = Vector2I.Zero;
            (Camera.LimitRight, Camera.LimitBottom) = (Vector2I)(Map.Size*Map.CellSize);
        }
    }
}