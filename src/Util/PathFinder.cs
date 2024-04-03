using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Level.Map;
using Level.Object;

namespace Util;

/// <summary>
/// Helper for finding movement, attack, and support ranges on a <c>LevelMap</c> for a <c>Unit</c>. Also acts as storage for computed ranges for the <c>Unit</c>.
/// A new one should be created each time movement ranges need to be recomputed.
/// </summary>
public class PathFinder
{
    /// <summary>Directions to look when finding cell neighbors.</summary>
    public static readonly Vector2I[] Directions = { Vector2I.Up, Vector2I.Right, Vector2I.Down, Vector2I.Left };

    /// <summary>Determine if two cell coordinate pairs are adjacent.</summary>
    /// <param name="a">First pair for comparison.</param>
    /// <param name="b">Second pair for comparison.</param>
    /// <returns><c>true</c> if the two coordinate pairs are adjacent, and <c>false</c> otherwise.</returns>
    public static bool IsAdjacent(Vector2I a, Vector2I b)
    {
        foreach (Vector2I direction in Directions)
            if (b - a == direction || a - b == direction)
                return true;
        return false;
    }

    /// <summary>Get all grid cells that a unit can walk on or pass through.</summary>
    /// <param name="map">Map the unit is walking on.</param>
    /// <param name="unit">Unit compute traversable cells for.</param>
    /// <returns>The set of cells, in any order, that the unit can traverse.</returns>
    public static IEnumerable<Vector2I> GetTraversableCells(Grid map, Unit unit)
    {
        int max = 2*(unit.MoveRange + 1)*(unit.MoveRange + 1) - 2*unit.MoveRange - 1;

        Dictionary<Vector2I, int> cells = new(max) {{ unit.Cell, 0 }};
        Queue<Vector2I> potential = new(max);

        potential.Enqueue(unit.Cell);
        while (potential.Count > 0)
        {
            Vector2I current = potential.Dequeue();

            foreach (Vector2I direction in Directions)
            {
                Vector2I neighbor = current + direction;
                if (map.Contains(neighbor))
                {
                    int cost = cells[current] + map.GetTerrain(neighbor).Cost;
                    if ((!cells.ContainsKey(neighbor) || cells[neighbor] > cost) && // cell hasn't been examined yet or this path is shorter to get there
                        (!map.Occupants.ContainsKey(neighbor) || ((map.Occupants[neighbor] as Unit)?.Affiliation.AlliedTo(unit) ?? false)) && // cell is empty or it's an allied unit
                        cost <= unit.MoveRange) // cost to get to cell is within range
                    {
                        cells[neighbor] = cost;
                        potential.Enqueue(neighbor);
                    }
                }
            }
        }

        return cells.Keys;
    }

    /// <summary>Find all the cells that can be attacked from a source cell.</summary>
    /// <param name="map">Map on which the attack is to be made.</param>
    /// <param name="ranges">Distances from the source cell that can be attacked.</param>
    /// <param name="source">Source cell to attack from.</param>
    /// <returns>A collection of grid cells containing all the cells that are exactly the given distances away from the source.</returns>
    public static IEnumerable<Vector2I> GetCellsInRange(Grid map, IEnumerable<int> ranges, Vector2I source)
    {
        HashSet<Vector2I> cells = new();
        foreach (int range in ranges)
        {
            for (int i = 0; i < range; i++)
            {
                Vector2I target;
                if (map.Contains(target = source + new Vector2I(-range + i, -i)))
                    cells.Add(target);
                if (map.Contains(target = source + new Vector2I(i, -range + i)))
                    cells.Add(target);
                if (map.Contains(target = source + new Vector2I(range - i, i)))
                    cells.Add(target);
                if (map.Contains(target = source + new Vector2I(-i, range - i)))
                    cells.Add(target);
            }
        }
        return cells;
    }

    /// <summary>Find all the cells that can be attacked from a collection of source cells.</summary>
    /// <param name="map">Map containing the cells to be attacked.</param>
    /// <param name="ranges">Distances from each source cell that can be attacked.</param>
    /// <param name="traversable">Source cells.</param>
    /// <returns>A collection of grid cells containing all the cells that are exactly the given distances away from any of the source cells.</returns>
    public static IEnumerable<Vector2I> GetCellsInRange(Grid map, IEnumerable<int> ranges, IEnumerable<Vector2I> traversable)
    {
        HashSet<Vector2I> cells = new();
        foreach (Vector2I source in traversable)
            foreach (Vector2I target in GetCellsInRange(map, ranges, source))
                cells.Add(target);
        return cells;
    }

    /// <summary>Remove all loops from a list of elements.  A loop is any sequence within the list that starts and ends with the same element.</summary>
    /// <typeparam name="T">Type of the elements of the list.</typeparam>
    /// <param name="items">List to straighten.</param>
    /// <returns>A new list containing all of the items in the input list with no loops. If the input list has no loops, then the new list is a copy of it.</returns>
    public static List<T> Disentangle<T>(IImmutableList<T> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            for (int j = items.Count - 1; j > i; j--)
            {
                if (EqualityComparer<T>.Default.Equals(items[i], items[j]))
                {
                    List<T> result = new();
                    result.AddRange(items.Take(i));
                    result.AddRange(items.TakeLast(items.Count - j));
                    return Disentangle(result.ToImmutableList());
                }
            }
        }
        return new(items);
    }

    private readonly Grid _map;
    private readonly Unit _unit;
    private readonly AStar2D _astar = new();

    /// <summary>Set of cells the unit can traverse on the map from its position.</summary>
    public IEnumerable<Vector2I> TraversableCells = Array.Empty<Vector2I>();
    
    /// <summary>Set of all cells the unit can attack from any position it could traverse on the map. Is not distinct from <c>TraversableCells</c>.</summary>
    public IEnumerable<Vector2I> AttackableCells = Array.Empty<Vector2I>();

    /// <summary>
    /// Set of all cells the unit can support from any position it could traverse on the map. Is not distinct from <c>TraversableCells</c> or
    /// <c>AttackableCells</c>.
    /// </summary>
    public IEnumerable<Vector2I> SupportableCells = Array.Empty<Vector2I>();

    /// <summary>Computed path for the unit to move along.</summary>
    public readonly List<Vector2I> Path = new();

    /// <summary>Create a new PathFinder for a unit on a map.</summary>
    /// <param name="map">Map on which paths are to be computed.</param>
    /// <param name="unit">Unit for which paths are to be computed.</param>
    public PathFinder(Grid map, Unit unit)
    {
        _map = map;
        _unit = unit;

        TraversableCells = GetTraversableCells(_map, _unit);
        AttackableCells = GetCellsInRange(_map, _unit.AttackRange, TraversableCells);
        SupportableCells = GetCellsInRange(_map, _unit.SupportRange, TraversableCells);

        foreach (Vector2I cell in TraversableCells)
            _astar.AddPoint(_map.CellId(cell), cell, _map.GetTerrain(cell).Cost);
        foreach (Vector2I cell in TraversableCells)
        {
            foreach (Vector2I direction in Directions)
            {
                Vector2I neighbor = cell + direction;
                if (!_astar.ArePointsConnected(_map.CellId(cell), _map.CellId(neighbor)) && TraversableCells.Contains(neighbor))
                    _astar.ConnectPoints(_map.CellId(cell), _map.CellId(neighbor));
            }
        }
        Path.Add(_unit.Cell);
    }

    /// <summary>
    /// Add a cell to the path. If the cell is not adjacent to the end of the path, then the AStar algorithm is used to compute intermediate
    /// cells. Then, if there are any loops in the path, they are removed. Then, if the path is still too long for the unit to traverse
    /// (accounting for cost), it is replaced with one computed using the AStar algorithm that is of the correct length.
    /// </summary>
    /// <param name="cell"></param>
    public void AddToPath(Vector2I cell)
    {
        List<Vector2I> extension = new();
        if (Path.Count == 0 || IsAdjacent(cell, Path.Last()))
            extension.Add(cell);
        else
            extension.AddRange(_astar.GetPointPath(_map.CellId(Path.Last()), _map.CellId(cell)).Select((c) => (Vector2I)c));

        List<Vector2I> temp = new(Path);
        Path.Clear();
        Path.AddRange(Disentangle(temp.Concat(extension).ToImmutableList()));
        if (_map.Cost(Path.TakeLast(Path.Count - 1)) > _unit.MoveRange)
        {
            Vector2I start = Path[0];
            Path.Clear();
            Path.AddRange(_astar.GetPointPath(_map.CellId(start), _map.CellId(cell)).Select((c) => (Vector2I)c));
        }
    }
}