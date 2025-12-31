using System;
using Godot;
using TbsFramework.Scenes.Level.Map;

namespace TbsFramework.Scenes.Level.Object;

public abstract class GridObjectData(bool occupies)
{
    public delegate void CellChangedEventHandler(Vector2I cell);

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
                _grid = value;
                if (_grid is not null)
                    Cell = _grid.Clamp(_cell);
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
                if (occupies && (_grid?.Occupants.TryGetValue(next, out GridObjectData occupant) ?? false) && occupant != this)
                    throw new ArgumentException($"Cell {next} is already occupied");
                Vector2I old = _cell;
                _cell = next;
                if (occupies && _grid is not null)
                {
                    _grid.Occupants.Remove(old);
                    _grid.Occupants[_cell] = this;
                }
                if (CellChanged is not null)
                    CellChanged(_cell);
            }
        }
    }
}