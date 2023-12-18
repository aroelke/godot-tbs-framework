using System.Collections.Generic;
using System.Linq;
using Godot;
using level.map;
using level.unit;

namespace util;

/// <summary>Helper for finding movement, attack, and support ranges on a <c>LevelMap</c>.</summary>
public static class PathFinder
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
    public static IEnumerable<Vector2I> GetTraversableCells(LevelMap map, Unit unit)
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
                    if ((!cells.ContainsKey(neighbor) || cells[neighbor] > cost) && cost <= unit.MoveRange)
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
    public static IEnumerable<Vector2I> GetCellsInRange(LevelMap map, IEnumerable<int> ranges, Vector2I source)
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
    public static IEnumerable<Vector2I> GetCellsInRange(LevelMap map, IEnumerable<int> ranges, IEnumerable<Vector2I> traversable)
    {
        HashSet<Vector2I> cells = new();
        foreach (Vector2I source in traversable)
            foreach (Vector2I target in GetCellsInRange(map, ranges, source))
                cells.Add(target);
        return cells;
    }

    /// <summary>Join two lists of cell coordinates, removing the first loop if there is one.</summary>
    /// <param name="a">First coordinate list to join.</param>
    /// <param name="b">Second coordinate list to join.</param>
    /// <returns>A list of coordinates consisting of the two lists concatenated, but with the first loop removed.</returns>
    public static List<Vector2I> Join(IList<Vector2I> a, IList<Vector2I> b)
    {
        List<Vector2I> result = new();
        for (int i = 0; i < a.Count; i++)
        {
            for (int j = 0; j < b.Count; j++)
            {
                if (a[i] == b[j])
                {
                    result.AddRange(a.Take(i));
                    result.AddRange(b.TakeLast(b.Count - j));
                    return result;
                }
            }
        }
        result.AddRange(a);
        result.AddRange(b);
        return result;
    }
}