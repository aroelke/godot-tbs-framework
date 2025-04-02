using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.State.Occupants;
using TbsTemplate.Scenes.Level.Map;

namespace TbsTemplate.Scenes.Level.State;

[GlobalClass, Tool]
public partial class GridState : Resource
{
    [Signal] public delegate void OccupantAddedEventHandler(GridOccupantState occupant, Vector2I cell);

    [Signal] public delegate void OccupantRemovedEventHandler(GridOccupantState occupant);

    [Signal] public delegate void OccupantMovedEventHandler(GridOccupantState occupant, Vector2I from, Vector2I to);

    private Vector2I _size = Vector2I.One;
    private Terrain _default = null;
    private readonly Dictionary<Vector2I, GridOccupantState> _occupants = [];

    /// <summary>Grid dimensions. Both elements should be positive.</summary>
    [Export] public Vector2I Size
    {
        get => _size;
        set
        {
            if (_size != value)
            {
                _size = value;

                Terrain[][] old = Terrain;
                Terrain = [.. Enumerable.Repeat(Enumerable.Repeat(new Terrain() { Cost = 1 }, value.X).ToArray(), value.Y)];
                for (int r = 0; r < value.Y; r++)
                {
                    for (int c = 0; c < value.X; c++)
                    {
                        if (r < old.Length && c < old[r].Length)
                            Terrain[r][c] = old[r][c];
                        else
                            Terrain[r][c] = DefaultTerrain;
                    }
                }
            }
        }
    }

    [Export] public Terrain DefaultTerrain
    {
        get => _default;
        set
        {
            if (value != _default)
            {
                for (int r = 0; r < Size.Y; r++)
                    for (int c = 0; c < Size.X; c++)
                        if (Terrain[r][c] is null || Terrain[r][c] == _default)
                            Terrain[r][c] = value;
                _default = value;
            }
        }
    }

    public Terrain[][] Terrain { get; private set; } = [[null]];

    public IDictionary<Vector2I, GridOccupantState> Occupants => _occupants.ToImmutableDictionary();

    /// <summary>Check if a cell offset is in the grid.</summary>
    /// <param name="offset">offset to check.</param>
    /// <returns><c>true</c> if the <paramref name="offset"/> is within the grid bounds, and <c>false</c> otherwise.</returns>
    public bool Contains(Vector2I offset) => offset.X >= 0 && offset.X < Size.X && offset.Y >= 0 && offset.Y < Size.Y;

    /// <summary>Find the cell offset closest to the given one inside the grid.</summary>
    /// <param name="cell">Cell offset to clamp.
    /// <returns>The cell <paramref name="offset"/> clamped to be inside the grid bounds using <c>Vector2I.Clamp</c></returns>
    public Vector2I Clamp(Vector2I offset) => offset.Clamp(Vector2I.Zero, Size - Vector2I.One);

    /// <param name="cell">Coordinates of the cell.</param>
    /// <returns>A unique ID within this map of the given <paramref name="cell"/>.</returns>
    public int CellId(Vector2I cell) => cell.X*Size.X + cell.Y;

    /// <summary>
    /// Compute the total cost of a collection of cells. If the cells are a contiguous path, represents the total cost of moving along that
    /// path.
    /// </summary>
    /// <param name="path">List of cells to sum up.</param>
    /// <returns>The sum of the cost of each cell in the <paramref name="path"/>.</returns>
    public int Cost(IEnumerable<Vector2I> path) => path.Select((c) => Terrain[c.Y][c.X].Cost).Sum();

    /// <summary>Find all the cells that are exactly a specified Manhattan distance away from a center cell.</summary>
    /// <param name="cell">Cell at the center of the range.</param>
    /// <param name="distance">Distance away from the center cell to search.</param>
    /// <returns>A collection of cells that are on the grid and exactly the specified <paramref name="distance"/> away from the center <paramref name="cell"/>.</returns>
    public IEnumerable<Vector2I> GetCellsAtRange(Vector2I cell, int distance)
    {
        HashSet<Vector2I> cells = [];
        for (int i = 0; i < distance; i++)
        {
            Vector2I target;
            if (Contains(target = cell + new Vector2I(-distance + i, -i)))
                cells.Add(target);
            if (Contains(target = cell + new Vector2I(i, -distance + i)))
                cells.Add(target);
            if (Contains(target = cell + new Vector2I(distance - i, i)))
                cells.Add(target);
            if (Contains(target = cell + new Vector2I(-i, distance - i)))
                cells.Add(target);
        }
        return cells;
    }

    public void SetOccupant(Vector2I cell, GridOccupantState occupant)
    {
        if (occupant is null)
        {
            if (!_occupants.TryGetValue(cell, out GridOccupantState removed))
                throw new ArgumentException($"Cannot remove occupant from empty cell {cell}.");

            _occupants.Remove(cell);
            removed.Grid = null;
            EmitSignal(SignalName.OccupantRemoved, removed);
        }
        else
        {
            if (_occupants.ContainsKey(cell))
                throw new ArgumentException($"{cell} is already occupied. Remove the occupant first.");

            if (occupant.Grid is null)
            {
                occupant.Grid = this;
                occupant.Cell = cell;
                EmitSignal(SignalName.OccupantAdded, occupant, cell);
            }
            else if (occupant.Grid != this)
                throw new ArgumentException("Occupant belongs to a different grid.");
            else if (occupant.Cell != cell)
            {
                Vector2I prev = occupant.Cell;
                _occupants.Remove(occupant.Cell);
                _occupants[cell] = occupant;
                occupant.Cell = cell;
                EmitSignal(SignalName.OccupantMoved, occupant, prev, cell);
            }
        }
    }

    public void RemoveOccupant(Vector2I cell) => SetOccupant(cell, null);
}