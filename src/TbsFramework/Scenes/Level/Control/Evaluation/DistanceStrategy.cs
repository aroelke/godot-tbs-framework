using System;
using System.Collections.Generic;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>Strategy to use when making a decision based on distance between two cells.</summary>
public enum DistanceStrategy
{
    /// <summary>Use the Manhattan distance, or the sum of the differences between cell coordinates.</summary>
    ManhattanDistance,
    /// <summary>Use the number of cells contained in the shortest path between the two cells (accounting for cell cost).</summary>
    PathLength,
    /// <summary>
    /// Use the number of cells contained in teh shortest path between the two cells only accounting for walls (defined as cells
    /// whose cost is greater than the moving unit's movement range).
    /// </summary>
    PathLengthNoTerrain,
    /// <summary>Use the cost of moving along the shortest path between the two cells.</summary>
    PathCost
}

static class DistanceStrategyExtensions
{
    /// <summary>Compute the distance between two cells based on the strategy.</summary>
    /// <param name="strategy">Strategy defining how the distance is calculated.</param>
    /// <param name="from">Starting cell.</param>
    /// <param name="to">Ending cell.</param>
    /// <param name="traversable">Cells that can be used to compute a path through, if applicable.</param>
    /// <param name="unit">Unit for which the distance is being calculated.</param>
    /// <returns>A number representing the distance between <paramref name="from"/> and <paramref name="to"/> based on the distance strategy.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If an unknown distance strategy is used.</exception>
    public static int GetCost(this DistanceStrategy strategy, Vector2I from, Vector2I to, IEnumerable<Vector2I> traversable, UnitData unit) => strategy switch {
        DistanceStrategy.ManhattanDistance   => from.ManhattanDistanceTo(to),
        DistanceStrategy.PathLength          => Path.Empty(traversable, unit.CellCost).Add(from).Add(to).Count - 1,
        DistanceStrategy.PathLengthNoTerrain => Path.Empty(traversable, (c) => unit.CellCost(c) > unit.Stats.MoveDistance ? int.MaxValue : 1).Add(from).Add(to).Count - 1,
        DistanceStrategy.PathCost            => unit.PathCost(Path.Empty(unit.Grid.AllCells, unit.CellCost).Add(from).Add(to)),
        _ => throw new ArgumentOutOfRangeException(Enum.GetName(strategy))
    };
}