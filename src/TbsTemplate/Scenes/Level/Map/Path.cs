using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsTemplate.Extensions;

namespace TbsTemplate.Scenes.Level.Map;

/// <summary>
/// An ordered list of <see cref="Vector2I"/>s that guarantees sequential elements are (orthogonally) adjacent and there are no loops. When an attempt
/// is made to add a new element, if it's not adjacent to the last element, additional elements are added in between to make sure each element
/// is adjacent to its neighbors in the list. Individual elements cannot be removed, as the space they were in would just need to be filled back
/// in.
/// 
/// Modifying elements within the list and removing sections are potentially planned future features that will be implemented as needed.
/// 
/// Paths are immutable, so any functions that cause changes instead return a new Path with the change made, preserving the old one, as in
/// <see cref="ImmutableList{T}"/>.
/// 
/// Paths exist on a <see cref="Grid"/> within traversable cells that they use to compute segments when needed.
/// </summary>
public class Path : ICollection<Vector2I>, IEnumerable<Vector2I>, IReadOnlyCollection<Vector2I>, IReadOnlyList<Vector2I>, ICollection, IEnumerable
{
    /// <summary>Create a new, empty path.</summary>
    /// <param name="grid">Grid containing the cells the path goes through.</param>
    /// <param name="astar">Instance of the A-Star algorithm used to compute segments.</param>
    /// <param name="traversable">Collecton of traversable cells the path can use.</param>
    /// <returns>An empty path.</returns>
    private static Path Empty(Grid grid, AStar2D astar, IEnumerable<Vector2I> traversable) => new(grid, astar, traversable, []);

    public static implicit operator List<Vector2I>(Path path) => [.. path];

    /// <summary>Create a new, empty path.</summary>
    /// <param name="grid">Grid containing the cells the path goes through.</param>
    /// <param name="traversable">Collecton of traversable cells the path can use.</param>
    /// <returns>An empty path.</returns>
    public static Path Empty(Grid grid, IEnumerable<Vector2I> traversable)
    {
        AStar2D astar = new();
        foreach (Vector2I cell in traversable)
            astar.AddPoint(grid.CellId(cell), cell, grid.GetTerrain(cell).Cost);
        foreach (Vector2I cell in traversable)
        {
            foreach (Vector2I direction in GridCalculations.Directions)
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

    /// <summary>Private constructor; use <see cref="Empty(Grid, AStar2D, IEnumerable{Vector2I})"/> instead.</summary>
    private Path(Grid grid, AStar2D astar, IEnumerable<Vector2I> traversable, ImmutableList<Vector2I> initial)
    {
        _grid = grid;
        _astar = astar;
        _traversable = traversable;
        _cells = initial;
    }

    public Vector2I this[int index] => _cells[index];

    /// <summary>
    /// Movement <see cref="Terrain.Cost"/> to traverse the path from beginning to end. Since each cell's cost represents the cost to move onto it, the first cell
    /// is ignored
    /// </summary>
    public int Cost => _grid.Cost(_cells.TakeLast(_cells.Count - 1));

    public int Count => _cells.Count;
    public bool IsReadOnly => true;
    public bool IsSynchronized => false;
    public object SyncRoot => this;

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the first occurrence within the range of elements in the path that
    /// starts at the specified index and contains the specified number of elements.
    /// </summary>
    /// <param name="item">The object to locate in the path. This value can be <c>null</c> for reference types.</param>
    /// <param name="index">The zero-based starting indexes of the search. 0 (zero) is valid in an empty list.</param>
    /// <param name="count">The number of elements in the section to search.</param>
    /// <param name="equalityComparer">The equality comparer to use to locate <paramref name="item"/>.</param>
    /// <returns>The zero-based index of the first occurrence of <paramref name="item"/> within the range of elements in the path that starts at
    /// <paramref name="index"/> and contains <paramref name="count"/> number of elements if found; otherwise -1.</returns>
    public int IndexOf(Vector2I item, int index, int count, IEqualityComparer<Vector2I> equalityComparer) => _cells.IndexOf(item, index, count, equalityComparer);

    /// <inheritdoc cref="IndexOf"/>
    /// <remarks>Uses a default index of 0, count of <see cref="Count"/>, and equality comparer.</remarks>
    public int IndexOf(Vector2I item) => _cells.IndexOf(item);

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the last occurrence within the range of elements in the path that contains
    /// the specified number of elements and ends at the specified index.
    /// </summary>
    /// <param name="item">The object to locate in the path. The value can be <c>null</c> for reference types.</param>
    /// <param name="index">The zero-based starting index of the search. 0 (zero) is valid in an empty path.</param>
    /// <param name="count">The number of elements in the section to search.</param>
    /// <param name="equalityComparer">The equality comparer to match <paramref name="item"/>.</param>
    /// <returns>
    /// The zero-based index of the last occurrence of <paramref name="item"/> within the range of elements in the path that contains <paramref name="count"/>
    /// number of elements and ends at <paramref name="item"/>, if found; otherwise, -1.
    /// </returns>
    public int LastIndexOf(Vector2I item, int index, int count, IEqualityComparer<Vector2I> equalityComparer) => _cells.LastIndexOf(item, index, count, equalityComparer);

    /// <inheritdoc cref="LastIndexOf"=/>
    /// <remarks>Uses a default index of 0, count <see cref="Count"/>, and equality comparer.</remarks>
    public int LastIndexOf(Vector2I item) => _cells.LastIndexOf(item);

    /// <summary>
    /// Appends a cell to the end of the path. If that cell is not adjacent to the prior end of the path, also computes the shortest path between them and inserts that as
    /// well.
    /// </summary>
    /// <param name="value">Cell to add.</param>
    /// <returns>A new path with <paramref name="value"/> appended, and potentially a contiguous segment between it and the previous end of the path as well.</returns>
    public Path Add(Vector2I value)
    {
        ImmutableList<Vector2I> cells = [];
        if (_cells.Count == 0 || _cells[^1].IsAdjacent(value))
        {
            // Append the cell if it's adjacent to the last cell in the path or the path is empty
            cells = _cells.Add(value);
        }
        else if (_cells[^1] == value)
        {
            // Don't have to do anything if it's the same cell as the end of the path
            return this;
        }
        else
        {
            // Append the cell and the shortest path between it and the last cell in the path
            cells = _cells.AddRange(_astar.GetPointPath(_grid.CellId(_cells[^1]), _grid.CellId(value)).Select(static (c) => (Vector2I)c));
        }
        cells = [.. cells.Disentangle()];
        return new(_grid, _astar, _traversable, cells);
    }

    /// <summary>Add a collection of cells to the path, inserting segments before and between as needed to ensure that all neighbors are adjacent.</summary>
    /// <param name="items">Cells to add.</param>
    /// <returns>A new path with <paramref name="items"/> appended, as well as segments before and between in case the first cell in <paramref name="items"/>
    /// is not adjacent to the previous path end and in case any sequential cells in <paramref name="items"/> are not adjacent.</returns>
    public Path AddRange(IEnumerable<Vector2I> items) => items.Aggregate(this, static (p, item) => p.Add(item));

    /// <summary>Create a new path using the same grid and set of traversable cells.</summary>
    /// <param name="items">Cells in the new path.</param>
    public Path SetTo(IEnumerable<Vector2I> items) => Clear().AddRange(items);

    /// <summary>
    /// Insert a cell at the specified index. If it's not adjacent to either of its neighbors, add a path segment on each side that connects them.
    /// </summary>
    /// <param name="index">Path index to add the cell at.</param>
    /// <param name="element">Cell coordinates to insert.</param>
    /// <returns>A new path with <paramref name="element"/> at <paramref name="index"/> and new segments make sure it's adjacent to its neighbors.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public Path Insert(int index, Vector2I element)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Insert a collection of cells at the specified index. If the starting cell and/or ending cells are not adjacent to their neighbors, or if any cells in the
    /// collection aren't adjacent to their neighbors, add path segments before, after, and/or between them to make sure they are.
    /// </summary>
    /// <param name="index">Path index to add the cell at.</param>
    /// <param name="items">Collection of cell coordinates to add.</param>
    /// <returns>
    /// A new path with <paramref name="items"/> at <paramref name="index"/> and new segments making sure each new element is adjacent to its neighbors.
    /// </returns>
    /// <exception cref="NotImplementedException"></exception>
    public Path InsertRange(int index, IEnumerable<Vector2I> items)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Replace the first matching cell with the specified cell, then add segments on either side of the new cell to make it contiguous with its neighbors.
    /// </summary>
    /// <param name="oldValue">Cell to replace.</param>
    /// <param name="newValue">Cell to replace with.</param>
    /// <param name="equalityComparer">The equality comparer to match old and new values.</param>
    /// <returns>
    /// A new path with the first instance of <paramref name="oldValue"/> replaced with <paramref name="newValue"/> and additional segments on either side to
    /// make sure <paramref name="newValue"/> is adjacent to its neighbors.
    /// </returns>
    /// <exception cref="NotImplementedException"></exception>
    public Path Replace(Vector2I oldValue, Vector2I newValue, IEqualityComparer<Vector2I> equalityComparer)
    {
        throw new NotImplementedException();
    }

    /// <summary>Set the cell at the specified index to a new value, then add segments on either side to ensure that it's adjacent to its neighbors.</summary>
    /// <param name="index">Index to change the cell at.</param>
    /// <param name="value">New cell at <paramref name="index"/>.</param>
    /// <returns>
    /// A new path with <paramref name="value"/> inserted at <paramref name="index"/> and segments added on either side to ensure the path is contiguous.
    /// </returns>
    /// <exception cref="NotImplementedException"></exception>
    public Path SetItem(int index, Vector2I value)
    {
        throw new NotImplementedException();
    }

    /// <summary>Remove a sequence of cells from the path, then connect the old neighbors together with the shortest path between them, if necessary.</summary>
    /// <param name="index">Starting index to remove cells from.</param>
    /// <param name="count">Number of cells in the sequence to remove.</param>
    /// <returns>
    /// A new path with <paramref name="count"/> cells removed starting from <paramref name="index"/>. If this results in a path with a hole in it, the new path
    /// will have a segment inserted that joins the two ends.
    /// </returns>
    /// <exception cref="NotImplementedException"></exception>
    public Path RemoveRange(int index, int count)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// If the total <see cref="Terrain.Cost"/> of the cells in the path, except for the first, is greater than the specified value, compute the shortest path
    /// between the first and last cells.
    /// </summary>
    /// <param name="cost">Maximum <see cref="Cost"/> of the path.</param>
    /// <returns>
    /// The path if its <see cref="Cost"/> is less than or equal to <paramref name="cost"/>, and the shortest path between the endpoints otherwise.
    /// </returns>
    public Path Clamp(int cost)
    {
        if (Cost > cost)
            return Clear().AddRange(_astar.GetPointPath(_grid.CellId(_cells[0]), _grid.CellId(_cells[^1])).Select((c) => (Vector2I)c));
        else
            return this;
    }

    /// <returns>An empty path on the same grid and with the same set of traversable cells as this one.</returns>
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