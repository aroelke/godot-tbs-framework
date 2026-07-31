using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Information about a unit's potential action.</summary>
/// <param name="Action">Resource describing the action.</param>
/// <param name="Source">Cells the action could be performed from.</param>
/// <param name="Target">Cell the action will be performed on.</param>
/// <param name="Traversable">Cells the acting unit can move on.</param>
public record class ActionInfo(UnitAction Action, IEnumerable<Vector2I> Source, Vector2I Target, IEnumerable<Vector2I> Traversable);

/// <summary>A <see cref="Unit"/> component that provides information about how the AI uses it in a specific situation.</summary>
[Tool, Icon("uid://cvtdbcchxcvon")]
public abstract partial class Behavior : Node
{
    
    /// <summary>Determine the cells a unit is allowed to end its movement on.</summary>
    /// <param name="unit">Unit that's moving.</param>
    public abstract IEnumerable<Vector2I> Destinations(UnitData unit);

    /// <summary>Determine the actions a unit is able to perform and which cell(s) those actions can be performed on.</summary>
    /// <param name="unit">Unit that could act.</param>
    /// <returns>The set of actions that can be performed.</returns>
    public abstract IEnumerable<ActionInfo> Actions(UnitData unit, IEnumerable<UnitAction> available);

    /// <summary>Choose a destination from a set of options. Destinations are assumed to all be valid for the action this decision is for.</summary>
    /// <param name="unit">Unit whose destination is being determined.</param>
    /// <param name="choices">Options for destination.</param>
    /// <param name="traversable">Cells <paramref name="unit"/> can move on in case the decision involves pathing.</param>
    public abstract Vector2I ChooseDestination(UnitData unit, IEnumerable<Vector2I> choices, IEnumerable<Vector2I> traversable);

    /// <inheritdoc cref="ChooseDestination"/>
    public Vector2I ChooseDestination(UnitData unit, IEnumerable<Vector2I> choices) => ChooseDestination(unit, choices, unit.GetTraversableCells());

    /// <summary>Determine the path the unit will traverse between two cells.</summary>
    /// <param name="unit">Unit that will move along the path.</param>
    /// <param name="from">Point to move from.</param>
    /// <param name="to">Point to move to.</param>
    /// <param name="traversable">Set of cells that can be traversed to make the path.</param>
    /// <returns>The path from <paramref name="from"/> to <paramref name="to"/> that <paramref name="unit"/> will traverse.</returns>
    /// <exception cref="ArgumentException">If either <paramref name="from"/> or <paramref name="to"/> is not traversable by <paramref name="unit"/>.</exception>
    public virtual Path GetPath(UnitData unit, Vector2I from, Vector2I to, IEnumerable<Vector2I> traversable)
    {
        if (!traversable.Contains(from) || !traversable.Contains(to))
            throw new ArgumentException($"Cannot compute path from {from} to {to}; at least one is not traversable.");
        return Path.Empty(unit.Grid, traversable).Add(from).Add(to);
    }

    /// <inheritdoc cref="GetPath(UnitData, Vector2I, Vector2I, IEnumerable{Vector2I})"/>
    public Path GetPath(UnitData unit, Vector2I from, Vector2I to) => GetPath(unit, from, to, unit.GetTraversableCells());

    /// <summary>Determine the path the unit will take from its cell to a destination.</summary>
    /// <param name="unit">Unit that will move along the path.</param>
    /// <param name="dest">Destination cell.</param>
    /// <returns>The path from <paramref name="unit"/>'s cell to <paramref name="dest"/> that <paramref name="unit"/> will take.</returns>
    public Path GetPath(UnitData unit, Vector2I dest, IEnumerable<Vector2I> traversable) => GetPath(unit, unit.Cell, dest, traversable);

    /// <inheritdoc cref="GetPath(UnitData, Vector2I, IEnumerable{Vector2I})"/>
    public Path GetPath(UnitData unit, Vector2I dest) => GetPath(unit, dest, unit.GetTraversableCells());
}