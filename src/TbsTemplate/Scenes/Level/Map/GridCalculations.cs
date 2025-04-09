using System;
using System.Collections.Generic;
using Godot;

namespace TbsTemplate.Scenes.Level.Map;

/// <summary>Structure defining cell data used for computing regions on a grid.</summary>
/// <param name="Cost">Cost of moving to the cell from an adjacent cell.</param>
/// <param name="Allowed">Whether or not the cell can be moved into.</param>
/// <remarks>Note that this structure can be defined for cells not on the Grid. Set <paramref name="Allowed"/> to false for such cells.</remarks>
public readonly record struct CellData(int Cost, bool Allowed);

/// <summary>Utility class for computing grid ranges.</summary>
public static class GridCalculations
{
    // List of the four cardinal directions
    public static readonly Vector2I[] Directions = [Vector2I.Up, Vector2I.Right, Vector2I.Down, Vector2I.Left];

    /// <summary>Determine if two cell coordinate pairs are adjacent.</summary>
    /// <param name="a">First pair for comparison.</param>
    /// <param name="b">Second pair for comparison.</param>
    /// <returns><c>true</c> if the two coordinate pairs are adjacent, and <c>false</c> otherwise.</returns>
    public static bool IsAdjacent(this Vector2I a, Vector2I b)
    {
        foreach (Vector2I direction in Directions)
            if (b - a == direction || a - b == direction)
                return true;
        return false;
    }

    /// <summary>Compute the cells that can be reached from a starting point given a maximum path cost.</summary>
    /// <param name="start">Starting cell.</param>
    /// <param name="move">Maximum path cost.</param>
    /// <param name="GetCellData">Function computing the data for a cell.</param>
    /// <param name="GetCellNeighbors">Function computing the neighbors of a cell.</param>
    /// <returns>A collection containing all cells that can be reached from <paramref name="start"/>.</returns>
    public static IEnumerable<Vector2I> TraversableCells(Vector2I start, int move, Func<Vector2I, CellData> GetCellData, Func<Vector2I, IEnumerable<Vector2I>> GetCellNeighbors)
    {
        int capacity = 2*(move + 1)*(move + 1) - 2*move - 1;
        Dictionary<Vector2I, int> cells = new(capacity) {{ start, 0 }};
        Queue<Vector2I> potential = new(capacity);

        potential.Enqueue(start);
        while (potential.Count > 0)
        {
            Vector2I current = potential.Dequeue();
            foreach (Vector2I neighbor in GetCellNeighbors(current))
            {
                CellData data = GetCellData(neighbor);
                if (data.Allowed)
                {
                    int cost = cells[current] + data.Cost;
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
}