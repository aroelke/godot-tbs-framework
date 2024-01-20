using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Level.Map;
using Level.Object;
using Level.Object.Group;
using UI;
using UI.Controls.Action;
using UI.HUD;
using Util;

namespace Level;

/// <summary>
/// Manages the setup of and objects inside a level and provides them information about it.  Is "global" in a way in that
/// all objects in the level may be able to see it and request information from it, but each level has its own manager.
/// </summary>
[Tool]
public partial class Level : Node2D
{
    private enum State
    {
        Idle,
        SelectUnit,
        UnitMoving,
        PostMove
    }

    private Grid _map = null;
    private Overlay _overlay = null;
    private Cursor _cursor = null;
    private State _state = State.Idle;
    private Unit _selected = null;
    private PathFinder _pathfinder = null;
    private ControlHint _cancelHint = null;

    private Grid Grid => _map ??= GetNode<Grid>("Grid");
    private Overlay Overlay => _overlay ??= GetNode<Overlay>("Overlay");
    private Cursor Cursor => _cursor ??= GetNode<Cursor>("Cursor");
    private ControlHint CancelHint => _cancelHint ??= GetNode<ControlHint>("UserInterface/HUD/Hints/CancelHint");

    private void DeselectUnit()
    {
        if (_selected is not null)
        {
            _selected.IsSelected = false;
            _selected = null;
        }
        _pathfinder = null;
        Overlay.Clear();

        CancelHint.Visible = false;
    }

    /// <summary>Map cancel selection action reference (distinct from menu back/cancel).</summary>
    [Export] public InputActionReference CancelAction = new();

    /// <summary>When a cell is selected, act based on what is or isn't in the cell.</summary>
    /// <param name="cell">Coordinates of the cell selection.</param>
    public async void OnCellSelected(Vector2I cell)
    {
        switch (_state)
        {
        case State.Idle:
            if (Grid.Occupants.ContainsKey(cell))
            {
                _selected = Grid.Occupants[cell] as Unit;
                _selected.IsSelected = true;
                _pathfinder = new(Grid, _selected);
                Overlay.TraversableCells = _pathfinder.TraversableCells;
                Overlay.AttackableCells = _pathfinder.AttackableCells.Where((c) => !_pathfinder.TraversableCells.Contains(c));
                Overlay.SupportableCells = _pathfinder.SupportableCells.Where((c) => !_pathfinder.TraversableCells.Contains(c) && !_pathfinder.AttackableCells.Contains(c));
                CancelHint.Visible = true;
                _state = State.SelectUnit;
            }
            break;
        case State.SelectUnit:
            if (cell != _selected.Cell && _pathfinder.TraversableCells.Contains(cell))
            {
                // Move the unit and wait for it to finish moving, and then delete the pathfinder as we don't need it anymore
                Grid.Occupants.Remove(_selected.Cell);
                _selected.MoveAlong(_pathfinder.Path);
                Grid.Occupants[_selected.Cell] = _selected;
                Overlay.Clear();
                _pathfinder = null;
                _state = State.UnitMoving;
                await ToSignal(_selected, Unit.SignalName.DoneMoving);

                // Show the unit's attack/support ranges
                IEnumerable<Vector2I> attackable = PathFinder.GetCellsInRange(Grid, _selected.AttackRange, _selected.Cell);
                IEnumerable<Vector2I> supportable = PathFinder.GetCellsInRange(Grid, _selected.SupportRange, _selected.Cell);
                Overlay.AttackableCells = attackable;
                Overlay.SupportableCells = supportable.Where((c) => !attackable.Contains(c));
                _state = State.PostMove;
            }
            else
            {
                DeselectUnit();
                _state = State.Idle;
            }
            break;
        case State.PostMove:
            DeselectUnit();
            _state = State.Idle;
            break;
        }
    }

    /// <summary>When the cursor moves while a unit is selected, update the path that's being drawn.</summary>
    /// <param name="position"></param>
    public void OnCursorMoved(Vector2I cell)
    {
        if (_state == State.SelectUnit && _pathfinder.TraversableCells.Contains(cell))
        {
            _pathfinder.AddToPath(cell);
            Overlay.Path = _pathfinder.Path;
        }
    }

    /// <summary>When a grid node is added to a group, update its grid.</summary>
    /// <param name="child"></param>
    public void OnChildEnteredGroup(Node child)
    {
        if (child is GridNode gd)
            gd.Grid = Grid;
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        // Make sure there's a map
        int maps = GetChildren().Where((c) => c is Grid).Count();
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
            _state = State.Idle;

            Cursor.Grid = Grid;
            GetNode<Pointer>("Pointer").Bounds = new(Vector2I.Zero, (Vector2I)(Grid.Size*Grid.CellSize));

            foreach (Node child in GetChildren())
            {
                if (child is IEnumerable<Unit> army)
                {
                    foreach (Unit unit in army)
                    {
                        unit.Grid = Grid;
                        unit.Cell = Grid.CellOf(unit.Position);
                        unit.Position = Grid.PositionOf(unit.Cell);
                        Grid.Occupants[unit.Cell] = unit;
                    }
                }
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        switch (_state)
        {
        case State.SelectUnit or State.PostMove:
            if (@event.IsActionReleased(CancelAction))
            {
                DeselectUnit();
                _state = State.Idle;
            }
            break;
        }
    }
}