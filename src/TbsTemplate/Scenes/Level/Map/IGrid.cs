using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Data;

namespace TbsTemplate.Scenes.Level.Map;

/// <summary>An object that can be used to represent a state of the map, including its dimensions, terrain, and occupants.</summary>
public interface IGrid
{
    // List of the four cardinal directions
    public static readonly Vector2I[] Directions = [Vector2I.Up, Vector2I.Right, Vector2I.Down, Vector2I.Left];

    /// <summary>Grid dimensions. Both elements should be positive.</summary>
    public Vector2I Size { get; }

    /// <summary>Check if a cell is in the grid.</summary>
    /// <param name="cell">offset to check.</param>
    /// <returns><c>true</c> if the <paramref name="cell"/> is within the grid bounds, and <c>false</c> otherwise.</returns>
    public bool Contains(Vector2I cell);

    /// <returns>The terrain information for a cell, or <see cref="DefaultTerrain"/> if the terrain hasn't been set.</returns>
    /// <exception cref="IndexOutOfRangeException">If the <paramref name="cell"/> is outside the grid.</exception>
    public Terrain GetTerrain(Vector2I cell);

    /// <summary>Whether or not a cell is traversible for a member of a faction.</summary>
    public bool IsTraversable(Vector2I cell, Faction faction);

    /// <summary>Get the neighbors of a cell in the grid.</summary>
    public IEnumerable<Vector2I> GetNeighbors(Vector2I cell) => Directions.Select((d) => cell + d).Where(Contains);

    /// <summary>Determine if two cell coordinate pairs are adjacent.</summary>
    /// <param name="a">First pair for comparison.</param>
    /// <param name="b">Second pair for comparison.</param>
    /// <returns><c>true</c> if the two coordinate pairs are adjacent, and <c>false</c> otherwise.</returns>
    public bool IsAdjacent(Vector2I a, Vector2I b)
    {
        foreach (Vector2I direction in Directions)
            if (b - a == direction || a - b == direction)
                return true;
        return false;
    }

    /// <summary>Compute the cells that can be reached from a starting point given a maximum path cost.</summary>
    /// <param name="faction">Faction of what's moving on the grid.</param>
    /// <param name="start">Starting cell.</param>
    /// <param name="move">Maximum path cost.</param>
    /// <returns>A collection containing all cells that can be reached from <paramref name="start"/>.</returns>
    public IEnumerable<Vector2I> TraversableCells(Faction faction, Vector2I start, int move)
    {
        int capacity = 2*(move + 1)*(move + 1) - 2*move - 1;
        Dictionary<Vector2I, int> cells = new(capacity) {{ start, 0 }};
        Queue<Vector2I> potential = new(capacity);

        potential.Enqueue(start);
        while (potential.Count > 0)
        {
            Vector2I current = potential.Dequeue();
            foreach (Vector2I neighbor in GetNeighbors(current))
            {
                Terrain terrain = GetTerrain(neighbor);
                if (IsTraversable(neighbor, faction))
                {
                    int cost = cells[current] + terrain.Cost;
                    if ((!cells.TryGetValue(neighbor, out int lowest) || lowest > cost) && cost <= move)
                    {
                        cells[neighbor] = cost;
                        potential.Enqueue(neighbor);
                    }
                }
            }
        }

        return cells.Keys;
    }

    /// <summary>Find all the cells that are exactly a specified Manhattan distance away from a center cell.</summary>
    /// <param name="cell">Cell at the center of the range.</param>
    /// <param name="distance">Distance away from the center cell to search.</param>
    /// <returns>A collection of cells that are on the grid and exactly the specified <paramref name="distance"/> away from the center <paramref name="cell"/>.</returns>
    public IEnumerable<Vector2I> CellsAtDistance(Vector2I cell, int distance)
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

    /// <summary>
    /// Compute the total cost of a collection of cells. If the cells are a contiguous path, represents the total cost of moving along that
    /// path.
    /// </summary>
    /// <param name="path">List of cells to sum up.</param>
    /// <returns>The sum of the cost of each cell in the <paramref name="path"/>.</returns>
    public int Cost(IEnumerable<Vector2I> path) => path.Select((c) => GetTerrain(c).Cost).Sum();
}