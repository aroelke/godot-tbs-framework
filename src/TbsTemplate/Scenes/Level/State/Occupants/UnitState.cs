using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Data;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Level.Control.Behavior;
using TbsTemplate.Scenes.Level.State.Components;

namespace TbsTemplate.Scenes.Level.State.Occupants;

[GlobalClass, Tool]
public partial class UnitState : GridOccupantState
{
    /// <summary>Get all cells in a set of ranges from a set of source cells.</summary>
    /// <param name="sources">Cells to compute ranges from.</param>
    /// <param name="ranges">Ranges to compute from <paramref name="sources"/>.</param>
    /// <returns>
    /// The set of all cells that are exactly within <paramref name="ranges"/> distance from at least one element of
    /// <paramref name="sources"/>.
    /// </returns>
    private IEnumerable<Vector2I> GetCellsInRange(IEnumerable<Vector2I> sources, IEnumerable<int> ranges) => sources.SelectMany((c) => ranges.SelectMany((r) => Grid.GetCellsAtRange(c, r)));

    private (IEnumerable<Vector2I>, IEnumerable<Vector2I>, IEnumerable<Vector2I>) ExcludeOccupants(IEnumerable<Vector2I> move, IEnumerable<Vector2I> attack, IEnumerable<Vector2I> support)
    {
        IEnumerable<UnitState> allies = Grid.Occupants.Select(static (e) => e.Value).OfType<UnitState>().Where((u) => Faction.AlliedTo(u.Faction));
        IEnumerable<UnitState> enemies = Grid.Occupants.Select(static (e) => e.Value).OfType<UnitState>().Where((u) => !Faction.AlliedTo(u.Faction));
        return (
            move.Where((c) => !enemies.Any((u) => u.Cell == c)),
            attack.Where((c) => !allies.Any((u) => u.Cell == c)),
            support.Where((c) => !enemies.Any((u) => u.Cell == c))
        );
    }

    [Export] public Stats Stats = new();

    [Export] public Faction Faction = null;

    [Export] public UnitBehavior Behavior = new StandBehavior() { AttackInRange = false };

    [Export] public HealthState Health = new();

    /// <returns>The set of cells that this unit can reach from its position, accounting for <see cref="Terrain.Cost"/>.</returns>
    public IEnumerable<Vector2I> TraversableCells()
    {
        int max = 2*(Stats.Move + 1)*(Stats.Move + 1) - 2*Stats.Move - 1;

        Dictionary<Vector2I, int> cells = new(max) {{ Cell, 0 }};
        Queue<Vector2I> potential = new(max);

        potential.Enqueue(Cell);
        while (potential.Count > 0)
        {
            Vector2I current = potential.Dequeue();

            foreach (Vector2I direction in Vector2IExtensions.Directions)
            {
                Vector2I neighbor = current + direction;
                if (Grid.Contains(neighbor))
                {
                    int cost = cells[current] + Grid.Terrain[neighbor.Y][neighbor.X].Cost;
                    bool occupied = Grid.Occupants.TryGetValue(neighbor, out GridOccupantState occupant);
                    if ((!cells.ContainsKey(neighbor) || cells[neighbor] > cost) && // cell hasn't been examined yet or this path is shorter to get there
                        (!occupied || (occupant is UnitState unit && unit.Faction.AlliedTo(Faction))) && // cell is empty or contains an allied unit
                        cost <= Stats.Move) // cost to get to cell is within range
                    {
                        cells[neighbor] = cost;
                        potential.Enqueue(neighbor);
                    }
                }
            }
        }

        return cells.Keys;
    }

    /// <summary>Compute all of the cells this unit could attack from the given set of source cells.</summary>
    /// <param name="sources">Cells to compute attack range from.</param>
    /// <returns>The set of all cells that could be attacked from any of the cell <paramref name="sources"/>.</returns>
    public IEnumerable<Vector2I> AttackableCells(IEnumerable<Vector2I> sources) => GetCellsInRange(sources, Stats.AttackRange);

    /// <inheritdoc cref="AttackableCells"/>
    /// <remarks>Uses a singleton set of cells constructed from the single <paramref name="source"/> cell.</remarks>
    public IEnumerable<Vector2I> AttackableCells(Vector2I source) => AttackableCells([source]);

    /// <inheritdoc cref="AttackableCells"/>
    /// <remarks>Uses the unit's current <see cref="Cell"/> as the source.</remarks>
    public IEnumerable<Vector2I> AttackableCells() => AttackableCells(Cell);

    /// <summary>Compute all of the cells this unit could support from the given set of source cells.</summary>
    /// <param name="sources">Cells to compute support range from.</param>
    /// <returns>The set of all cells that could be supported from any of the source cells.</returns>
    public IEnumerable<Vector2I> SupportableCells(IEnumerable<Vector2I> sources) => GetCellsInRange(sources, Stats.SupportRange);

    /// <inheritdoc cref="SupportableCells"/>
    /// <remarks>Uses a singleton set of cells constructed from the single <paramref name="source"/> cell.</remarks>
    public IEnumerable<Vector2I> SupportableCells(Vector2I source) => SupportableCells([source]);

    /// <inheritdoc cref="SupportableCells"/>
    /// <remarks>Uses the unit's current <see cref="Cell"/> as the source.</remarks>
    public IEnumerable<Vector2I> SupportableCells() => SupportableCells(Cell);

    /// <returns>The complete sets of cells this unit can act on.</returns>
    public (IEnumerable<Vector2I> traversable, IEnumerable<Vector2I> attackable, IEnumerable<Vector2I> supportable) ActionRanges()
    {
        IEnumerable<Vector2I> traversable = TraversableCells();
        return ExcludeOccupants(
            traversable,
            AttackableCells(traversable.Where((c) => !Grid.Occupants.ContainsKey(c) || Grid.Occupants[c] == this)),
            SupportableCells(traversable.Where((c) => !Grid.Occupants.ContainsKey(c) || Grid.Occupants[c] == this))
        );
    }
}