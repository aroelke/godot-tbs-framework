using System.Collections.Generic;
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

    public Stats Stats { get; }

    public Faction Faction { get; }

    public Vector2I Cell { get; }

    /// <returns>The set of cells that this unit can reach from its position, accounting for <see cref="Terrain.Cost"/>, on <paramref name="grid"/>.</returns>
    public IEnumerable<Vector2I> TraversableCells(IGrid grid);
}