using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.State.Occupants;

namespace TbsTemplate.Scenes.Level.Control.Behavior;

/// <summary>
/// A <see cref="Unit"/> resource that provides information about how the AI uses it in a
/// specific situation.
/// </summary>
[GlobalClass, Tool]
public abstract partial class UnitBehavior : Resource
{
    public abstract IEnumerable<Vector2I> Destinations(UnitState unit);

    public abstract Dictionary<StringName, IEnumerable<Vector2I>> Actions(UnitState unit);

    /// <summary>Determine the path the unit will traverse between two cells.</summary>
    /// <param name="unit">Unit that will move along the path.</param>
    /// <param name="from">Point to move from.</param>
    /// <param name="to">Point to move to.</param>
    /// <returns>The path from <paramref name="from"/> to <paramref name="to"/> that <paramref name="unit"/> will traverse.</returns>
    /// <exception cref="ArgumentException">If either <paramref name="from"/> or <paramref name="to"/> is not traversable by <paramref name="unit"/>.</exception>
    public virtual Path GetPath(UnitState unit, Vector2I from, Vector2I to)
    {
        IEnumerable<Vector2I> traversable = unit.TraversableCells();
        if (!traversable.Contains(from) || !traversable.Contains(to))
            throw new ArgumentException($"Cannot compute path from {from} to {to}; at least one is not traversable.");
        return Path.Empty(unit.Grid, traversable).Add(from).Add(to);
    }

    /// <summary>Determine the path the unit will take from its cell to a destination.</summary>
    /// <param name="unit">Unit that will move along the path.</param>
    /// <param name="to">Destination cell.</param>
    /// <returns>The path from <paramref name="unit"/>'s cell to <paramref name="to"/> that <paramref name="unit"/> will take.</returns>
    public Path GetPath(UnitState unit, Vector2I to) => GetPath(unit, unit.Cell, to);
}