using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Util;

namespace Level.Map;

public class Path : ICollection<Vector2I>, IEnumerable<Vector2I>, IReadOnlyCollection<Vector2I>, IReadOnlyList<Vector2I>, ICollection, IEnumerable
{
    private static Path Empty(Grid grid, AStar2D astar, IEnumerable<Vector2I> traversable) => new(grid, astar, traversable, ImmutableList<Vector2I>.Empty);

    public static Path Empty(Grid grid, IEnumerable<Vector2I> traversable)
    {
        AStar2D astar = new();
        foreach (Vector2I cell in traversable)
            astar.AddPoint(grid.CellId(cell), cell, grid.GetTerrain(cell).Cost);
        foreach (Vector2I cell in traversable)
        {
            foreach (Vector2I direction in Vector2IExtensions.Directions)
            {
                Vector2I neighbor = cell + direction;
                if (!astar.ArePointsConnected(grid.CellId(cell), grid.CellId(neighbor)) && traversable.Contains(neighbor))
                    astar.ConnectPoints(grid.CellId(cell), grid.CellId(neighbor));
            }
        }
        return Empty(grid, astar, traversable);
    }

    private readonly Grid _grid;
    private readonly AStar2D _astar;
    private readonly IEnumerable<Vector2I> _traversable;
    private readonly ImmutableList<Vector2I> _cells;

    private Path(Grid grid, AStar2D astar, IEnumerable<Vector2I> traversable, ImmutableList<Vector2I> initial)
    {
        _grid = grid;
        _astar = astar;
        _traversable = traversable;
        _cells = initial;
    }

    public Vector2I this[int index] => _cells[index];

    public int Count => _cells.Count;

    public bool IsReadOnly => true;

    public bool IsSynchronized => false;

    public object SyncRoot => this;

    public int IndexOf(Vector2I item, int index, int count, IEqualityComparer<Vector2I> equalityComparer) => _cells.IndexOf(item, index, count, equalityComparer);

    public int IndexOf(Vector2I item) => _cells.IndexOf(item);

    public int LastIndexOf(Vector2I item, int index, int count, IEqualityComparer<Vector2I> equalityComparer) => _cells.LastIndexOf(item, index, count, equalityComparer);

    public int LastIndexOf(Vector2I item) => _cells.LastIndexOf(item);

    public Path Add(Vector2I value)
    {
        ImmutableList<Vector2I> cells = ImmutableList<Vector2I>.Empty;
        if (_cells.Count == 0 || _cells.Last().IsAdjacent(value))
        {
            // Append the cell if it's adjacent to the last cell in the path or the path is empty
            cells = _cells.Add(value);
        }
        else
        {
            // Append the cell and the shortest path between it and the last cell in the path
            cells = _cells.AddRange(_astar.GetPointPath(_grid.CellId(_cells.Last()), _grid.CellId(value)).Select((c) => (Vector2I)c));
        }
        cells = cells.Disentangle().ToImmutableList();
        return new(_grid, _astar, _traversable, cells);
    }

    public Path AddRange(IEnumerable<Vector2I> items) => items.Aggregate(this, (p, item) => p.Add(item));

    public Path Insert(int index, Vector2I element)
    {
        throw new NotImplementedException();
    }

    public Path InsertRange(int index, IEnumerable<Vector2I> items)
    {
        throw new NotImplementedException();
    }

    public Path Replace(Vector2I oldValue, Vector2I newValue, IEqualityComparer<Vector2I> equalityComparer)
    {
        throw new NotImplementedException();
    }

    public Path SetItem(int index, Vector2I value)
    {
        throw new NotImplementedException();
    }

    public Path RemoveAll(Predicate<Vector2I> match)
    {
        throw new NotImplementedException();
    }

    public Path RemoveRange(IEnumerable<Vector2I> items, IEqualityComparer<Vector2I> equalityComparer)
    {
        throw new NotImplementedException();
    }

    public Path RemoveRange(int index, int count)
    {
        throw new NotImplementedException();
    }

    public Path Clamp(int cost)
    {
        if (_grid.Cost(_cells.TakeLast(_cells.Count - 1)) > cost)
            return Clear().AddRange(_astar.GetPointPath(_grid.CellId(_cells.First()), _grid.CellId(_cells.Last())).Select((c) => (Vector2I)c));
        else
            return this;
    }

    public Path Clear() => Empty(_grid, _astar, _traversable);

    public bool Contains(Vector2I item) => _cells.Contains(item);
    public void CopyTo(Vector2I[] array, int arrayIndex) => _cells.CopyTo(array, arrayIndex);
    public void CopyTo(Array array, int index) => ((ICollection)_cells).CopyTo(array, index);

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_cells).GetEnumerator();
    public IEnumerator<Vector2I> GetEnumerator() => _cells.GetEnumerator();

    public bool Remove(Vector2I item) => throw new NotSupportedException();
    void ICollection<Vector2I>.Add(Vector2I item) => throw new NotSupportedException();
    void ICollection<Vector2I>.Clear() => throw new NotSupportedException();
}