using System;
using Godot;
using TbsFramework.Data;
using TbsFramework.Scenes.Level.Map;

namespace TbsFramework.Scenes.Level.Object;

/// <summary>Data structure for tracking information about an object on the map.</summary>
/// <param name="occupies">
/// Whether or not the object occupies a cell. Only one object with this <c>true</c> can  have the same <see cref="Cell"/> value for a
/// particular grid.
/// </param>
public abstract class GridObjectData()
{
    private GridData _grid = null;
    private Vector2I _cell = -Vector2I.One;

    /// <summary>Signals that the grid object has moved to a new cell.</summary>
    public event PropertyChangedEventHandler<Vector2I> CellChanged;
    /// <summary>Signls that the object's grid has changed.</summary>
    public event PropertyChangedEventHandler<GridData> GridChanged;

    /// <summary>Grid the object exists on.</summary>
    public GridData Grid
    {
        get => _grid;
        set
        {
            if (_grid != value)
            {
                GridData old = _grid;
                _grid = value;
                if (GridChanged is not null)
                    GridChanged(old, _grid);
            }
        }
    }

    protected GridObjectData(GridObjectData original) : this()
    {
        _cell = original.Cell;
        // Leave _grid null because it already has an occupant in that spot
    }

    /// <summary>
    /// Cell on the grid the object is in. If <c>occupies</c> is <c>true</c>, other objects also with it <c>true</c> and with the
    /// same <see cref="Grid"/> can't have the same cell value. If the new value is outside <see cref="Grid"/>'s bounds, it's
    /// clamped to be within the bounds. If that happens, <see cref="CellChanged"/> is only raised if the value actually changes.
    /// </summary>
    public Vector2I Cell
    {
        get => _cell;
        set
        {
            Vector2I next = _grid?.Clamp(value) ?? value;
            if (_cell != next)
            {
                Vector2I old = _cell;
                _cell = next;
                if (CellChanged is not null)
                    CellChanged(old, _cell);
            }
        }
    }

    /// <summary>When this object next enters a specific cell, such as when it's done moving along a path, perform an action.</summary>
    public void WhenDoneMoving(Vector2I cell, Action action)
    {
        void OnCellChanged(Vector2I from, Vector2I to)
        {
            if (to == cell)
            {
                action();
                CellChanged -= OnCellChanged;
            }
        }
        if (Cell == cell)
            action();
        else
            CellChanged += OnCellChanged;
    }
}