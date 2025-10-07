using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>A <see cref="Unit"/> component that provides information about how the AI uses it in a specific situation.</summary>
public abstract partial class Behavior : Node
{
    
    /// <summary>Determine the cells a unit is allowed to end its movement on.</summary>
    /// <param name="unit">Unit that's moving.</param>
    /// <param name="grid">Grid the unit is moving on.</param>
    public abstract IEnumerable<Vector2I> Destinations(IUnit unit, IGrid grid);

    /// <summary>Determine the actions a unit is able to perform and which cell(s) those actions can be performed on.</summary>
    /// <param name="unit">Unit that could act.</param>
    /// <param name="grid">Grid on which the unit will act.</param>
    /// <returns>The set of actions that can be performed.</returns>
    public abstract IEnumerable<UnitAction> Actions(IUnit unit, IGrid grid);

    /// <summary>Determine the path the unit will traverse between two cells.</summary>
    /// <param name="unit">Unit that will move along the path.</param>
    /// <param name="grid">Grid that the unit will move on.</param>
    /// <param name="from">Point to move from.</param>
    /// <param name="to">Point to move to.</param>
    /// <returns>The path from <paramref name="from"/> to <paramref name="to"/> that <paramref name="unit"/> will traverse.</returns>
    /// <exception cref="ArgumentException">If either <paramref name="from"/> or <paramref name="to"/> is not traversable by <paramref name="unit"/>.</exception>
    public virtual Path GetPath(IUnit unit, IGrid grid, Vector2I from, Vector2I to)
    {
        IEnumerable<Vector2I> traversable = unit.TraversableCells(grid);
        if (!traversable.Contains(from) || !traversable.Contains(to))
            throw new ArgumentException($"Cannot compute path from {from} to {to}; at least one is not traversable.");
        return Path.Empty(grid, traversable).Add(from).Add(to);
    }

    /// <summary>Determine the path the unit will take from its cell to a destination.</summary>
    /// <param name="unit">Unit that will move along the path.</param>
    /// <param name="grid">Grid that the unit will move on.</param>
    /// <param name="dest">Destination cell.</param>
    /// <returns>The path from <paramref name="unit"/>'s cell to <paramref name="dest"/> that <paramref name="unit"/> will take.</returns>
    public Path GetPath(IUnit unit, IGrid grid, Vector2I dest) => GetPath(unit, grid, unit.Cell, dest);
}