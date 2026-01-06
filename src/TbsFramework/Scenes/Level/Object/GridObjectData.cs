using System;
using Godot;
using TbsFramework.Scenes.Level.Map;

namespace TbsFramework.Scenes.Level.Object;

public abstract class GridObjectData(bool occupies)
{
    public delegate void CellChangedEventHandler(Vector2I cell);

    private readonly bool _occupant = occupies;
    private GridData _grid = null;
    private Vector2I _cell = -Vector2I.One;

    public event CellChangedEventHandler CellChanged;

    public GridData Grid
    {
        get => _grid;
        set
        {
            if (_grid != value)
            {
                _grid?.Occupants.Remove(_cell);
                _grid = value;
                if (_grid is not null)
                {
                    Cell = _grid.Clamp(_cell);
                    _grid.Occupants[Cell] = this;
                }
            }
        }
    }

    public Vector2I Cell
    {
        get => _cell;
        set
        {
            Vector2I next = _grid?.Clamp(value) ?? value;
            if (_cell != next)
            {
                if (_occupant && (_grid?.Occupants.TryGetValue(next, out GridObjectData occupant) ?? false) && occupant != this)
                    throw new ArgumentException($"Cell {next} is already occupied");
                Vector2I old = _cell;
                _cell = next;
                if (_occupant && _grid is not null)
                {
                    _grid.Occupants.Remove(old);
                    _grid.Occupants[_cell] = this;
                }
                if (CellChanged is not null)
                    CellChanged(_cell);
            }
        }
    }

    protected GridObjectData(GridObjectData original) : this(original._occupant)
    {
        _cell = original.Cell;
        // Leave _grid null because it already has an occupant in that spot
    }

    public abstract GridObjectData Clone();
}