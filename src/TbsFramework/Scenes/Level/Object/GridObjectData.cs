using System;
using Godot;
using TbsFramework.Scenes.Level.Map;

namespace TbsFramework.Scenes.Level.Object;

/// <summary>Data structure for tracking information about an object on the map.</summary>
/// <param name="occupies">
/// Whether or not the object occupies a cell. Only one object with this <c>true</c> can  have the same <see cref="Cell"/> value for a
/// particular grid.
/// </param>
public abstract class GridObjectData(bool occupies)
{
    /// <summary>Handler for changes in this object's cell.</summary>
    /// <param name="from">Cell that was moved from.</param>
    /// <param name="to">Cell that was moved to.</param>
    public delegate void CellChangedEventHandler(Vector2I from, Vector2I to);

    private readonly bool _occupant = occupies;
    private GridData _grid = null;
    private Vector2I _cell = -Vector2I.One;

    /// <summary>Signals that the grid object has moved to a new cell.</summary>
    public event CellChangedEventHandler CellChanged;

    /// <summary>Grid the object exists on.</summary>
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

    protected GridObjectData(GridObjectData original) : this(original._occupant)
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
                    CellChanged(old, _cell);
            }
        }
    }

    /// <returns>
    /// A copy of this object with all the same values except for <see cref="Grid"/>, which should be left <c>null</c> if <c>occupies</c>
    /// is <c>true</c>.
    /// </returns>
    public abstract GridObjectData Clone();
}