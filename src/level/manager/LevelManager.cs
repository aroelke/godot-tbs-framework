using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using level.map;
using level.ui;
using level.unit;
using util;

namespace level.manager;

/// <summary>
/// Manages the setup of and objects inside a level and provides them information about it.  Is "global" in a way in that
/// all objects in the level may be able to see it and request information from it, but each level has its own manager.
/// </summary>
[Tool]
public partial class LevelManager : Node2D
{
    private LevelMap _map = null;
    private Overlay _overlay = null;
    private Unit _selected = null;
    private PathFinder _pathfinder = null;
    private readonly List<ArmyManager> _affiliations = new();

    private LevelMap Map => _map ??= GetNode<LevelMap>("LevelMap");
    private Overlay Overlay => _overlay ??= GetNode<Overlay>("Overlay");

    private void DeselectUnit()
    {
        if (_selected is not null)
        {
            _selected.IsSelected = false;
            _selected = null;
        }
        _pathfinder = null;
        Overlay.Clear();
    }

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
    public async void OnCellSelected(Vector2I cell)
    {
        if (_selected is null)
        {
            foreach (ArmyManager army in _affiliations)
            {
                if (army.Units.ContainsKey(cell))
                {
                    _selected = army.Units[cell];
                    _selected.IsSelected = true;
                    _pathfinder = new(Map, _selected);
                    Overlay.TraversableCells = _pathfinder.TraversableCells;
                    Overlay.AttackableCells = _pathfinder.AttackableCells.Where((c) => !_pathfinder.TraversableCells.Contains(c));
                    Overlay.SupportableCells = _pathfinder.SupportableCells.Where((c) => !_pathfinder.TraversableCells.Contains(c) && !_pathfinder.AttackableCells.Contains(c));
                    break;
                }
            }
        }
        else if (_pathfinder is not null)
        {
            if (!_selected.IsMoving)
            {
                if (cell != _selected.Cell && _pathfinder.TraversableCells.Contains(cell))
                {
                    // Move the unit and wait for it to finish moving, and then delete the pathfinder as we don't need it anymore
                    _selected.Affiliation.Units.Remove(_selected.Cell);
                    _selected.MoveAlong(_pathfinder.Path);
                    _selected.Affiliation.Units[_selected.Cell] = _selected;
                    Overlay.Clear();
                    _pathfinder = null;
                    await ToSignal(_selected, Unit.SignalName.DoneMoving);

                    // Show the unit's attack/support ranges
                    IEnumerable<Vector2I> attackable = PathFinder.GetCellsInRange(Map, _selected.AttackRange, _selected.Cell);
                    IEnumerable<Vector2I> supportable = PathFinder.GetCellsInRange(Map, _selected.SupportRange, _selected.Cell);
                    Overlay.AttackableCells = attackable;
                    Overlay.SupportableCells = supportable.Where((c) => !attackable.Contains(c));
                }
                else
                    DeselectUnit();
            }
        }
        else
            DeselectUnit();
    }

    public void OnCursorMoved(Vector2 position)
    {
        if (_selected != null && _pathfinder != null)
        {
            Vector2I cell = CellOf(position);
            if (_pathfinder.TraversableCells.Contains(cell))
            {
                _pathfinder.AddToPath(cell);
                Overlay.Path = _pathfinder.Path;
            }
        }
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