using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using level.map;
using level.unit;

namespace level.manager;

/// <summary>
/// Manages the setup of and objects inside a level and provides them information about it.  Is "global" in a way in that
/// all objects in the level may be able to see it and request information from it, but each level has its own manager.
/// </summary>
[Tool]
public partial class LevelManager : Node2D
{
    private LevelMap _map;
    private readonly List<ArmyManager> _affiliations = new();

    private LevelMap Map => _map ??= GetNode<LevelMap>("LevelMap");

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

    /// <summary>When a cell is selected, act based on what is or isn't in the cell.</summary>
    /// <param name="cell">Coordinates of the cell selection.</param>
    public void OnCellCelected(Vector2I cell)
    {
        bool has = false;
        foreach (ArmyManager army in _affiliations)
        {
            if (army.Units.ContainsKey(cell))
            {
                has = true;
                GD.Print($"{cell} contains a unit in {army.Name}");
                break;
            }
        }
        if (!has)
            GD.Print(cell);
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        // Make sure there's a map
        int maps = GetChildren().Where((c) => c is LevelMap).Count();
        if (maps < 1)
            warnings.Add("Level does not contain a map.");
        else if (maps > 1)
            warnings.Add($"Level contains too many maps ({maps}).");

        // Make sure there are units to control and to fight.
        if (GetChildren().Where((c) => c is ArmyManager).Any())
            warnings.Add("There are not any armies to assign units to.");

        return warnings.ToArray();
    }

    public override void _Ready()
    {
        base._Ready();
        _affiliations.AddRange(GetChildren().Where((c) => c is ArmyManager).Select((c) => c as ArmyManager));
    }
}