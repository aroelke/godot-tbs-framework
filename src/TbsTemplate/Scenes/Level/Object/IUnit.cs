using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Data;
using TbsTemplate.Scenes.Level.Map;

namespace TbsTemplate.Scenes.Level.Object;

public interface IUnit
{
    protected static IEnumerable<Vector2I> TraversableCells(IUnit unit, IGrid grid)
    {
        int max = 2*(unit.Stats.Move + 1)*(unit.Stats.Move + 1) - 2*unit.Stats.Move - 1;

        Dictionary<Vector2I, int> cells = new(max) {{ unit.Cell, 0 }};
        Queue<Vector2I> potential = new(max);

        potential.Enqueue(unit.Cell);
        while (potential.Count > 0)
        {
            Vector2I current = potential.Dequeue();

            foreach (Vector2I direction in IGrid.Directions)
            {
                Vector2I neighbor = current + direction;
                if (grid.Contains(neighbor))
                {
                    int cost = cells[current] + grid.GetTerrain(neighbor).Cost;
                    if ((!cells.ContainsKey(neighbor) || cells[neighbor] > cost) && grid.IsTraversable(neighbor, unit.Faction) && cost <= unit.Stats.Move) // cost to get to cell is within range
                    {
                        cells[neighbor] = cost;
                        potential.Enqueue(neighbor);
                    }
                }
            }
        }

        return cells.Keys;
    }

    /// <summary>Get all cells in a set of ranges from a set of source cells.</summary>
    /// <param name="sources">Cells to compute ranges from.</param>
    /// <param name="ranges">Ranges to compute from <paramref name="sources"/>.</param>
    /// <returns>
    /// The set of all cells that are exactly within <paramref name="ranges"/> distance from at least one element of
    /// <paramref name="sources"/>.
    /// </returns>
    protected static IEnumerable<Vector2I> GetCellsInRange(IGrid grid, IEnumerable<Vector2I> sources, IEnumerable<int> ranges) => [.. sources.SelectMany((c) => ranges.SelectMany((r) => grid.GetCellsAtDistance(c, r)))];

    public Stats Stats { get; }

    public Faction Faction { get; }

    public Vector2I Cell { get; }

    /// <returns>The set of cells that this unit can reach from its position, accounting for <see cref="Terrain.Cost"/>, on <paramref name="grid"/>.</returns>
    public IEnumerable<Vector2I> TraversableCells(IGrid grid);
}