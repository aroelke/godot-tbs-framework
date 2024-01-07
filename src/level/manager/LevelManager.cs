using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using level.map;
using level.Object;
using level.Object.Group;
using ui;
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
    private Cursor _cursor = null;
    private PointerProjection _projection = null;
    private readonly Dictionary<Vector2I, Unit> _units = new();
    private Unit _selected = null;
    private PathFinder _pathfinder = null;

    private LevelMap Map => _map ??= GetNode<LevelMap>("LevelMap");
    private Overlay Overlay => _overlay ??= GetNode<Overlay>("Overlay");
    private Cursor Cursor => _cursor ??= GetNode<Cursor>("Cursor");
    private PointerProjection Projection => _projection ??= GetNode<PointerProjection>("PointerProjection");

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

    /// <summary>When a cell is selected, act based on what is or isn't in the cell.</summary>
    /// <param name="cell">Coordinates of the cell selection.</param>
    public async void OnCellSelected(Vector2I cell)
    {
        if (_selected is null)
        {
            if (_units.ContainsKey(cell))
            {
                _selected = _units[cell];
                _selected.IsSelected = true;
                _pathfinder = new(Map, _selected);
                Overlay.TraversableCells = _pathfinder.TraversableCells;
                Overlay.AttackableCells = _pathfinder.AttackableCells.Where((c) => !_pathfinder.TraversableCells.Contains(c));
                Overlay.SupportableCells = _pathfinder.SupportableCells.Where((c) => !_pathfinder.TraversableCells.Contains(c) && !_pathfinder.AttackableCells.Contains(c));
            }
        }
        else if (_pathfinder is not null)
        {
            if (!_selected.IsMoving)
            {
                if (cell != _selected.Cell && _pathfinder.TraversableCells.Contains(cell))
                {
                    // Move the unit and wait for it to finish moving, and then delete the pathfinder as we don't need it anymore
                    _units.Remove(_selected.Cell);
                    _selected.MoveAlong(_pathfinder.Path);
                    _units[_selected.Cell] = _selected;
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
            Vector2I cell = Map.CellOf(position);
            if (_pathfinder.TraversableCells.Contains(cell))
            {
                _pathfinder.AddToPath(cell);
                Overlay.Path = _pathfinder.Path;
            }
        }
    }

    public void OnChildEnteredGroup(Node child)
    {
        if (child is GridNode gd)
            gd.Grid = Map;
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
        if (!GetChildren().Where((c) => c is Army).Any())
            warnings.Add("There are not any armies to assign units to.");

        return warnings.ToArray();
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            if (Cursor is not null)
                Cursor.Grid = Map;
            if (Projection is not null)
                Projection.Grid = Map;

            _units.Clear();
            foreach (Node child in GetChildren())
            {
                if (child is IEnumerable<Unit> army)
                {
                    foreach (Unit unit in army)
                    {
                        unit.Grid = Map;
                        unit.Cell = Map.CellOf(unit.Position);
                        unit.Position = Map.PositionOf(unit.Cell);
                        _units[unit.Cell] = unit;
                    }
                }
            }
        }
    }
}