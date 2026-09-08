using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>Structure containing information required to perform a potential action available to a unit.</summary>
/// <param name="Action">Action to be performed.</param>
/// <param name="Actor">Unit that would perform <paramref name="Action"/>.</param>
/// <param name="Target">Cell <paramref name="Action"/> would be performed on.</param>
/// <param name="Sources">Cells from which <paramref name="Actor"/> could perform <paramref name="Action"/> on <paramref name="Target"/>.</param>
public readonly record struct PerformableAction(UnitAction Action, UnitData Actor, Vector2I Target, IEnumerable<Vector2I> Sources)
{
    private const int initial = 50671;
    private const int coefficient = 149;

    public bool Equals(PerformableAction other)
    {
        if (Action != other.Action)
            return false;
        if (Actor != other.Actor)
            return false;
        if (Target != other.Target)
            return false;
        if (Sources.Count() != other.Sources.Count() || !Sources.ToHashSet().SetEquals(other.Sources))
            return false;
        return true;
    }

    public override int GetHashCode() => HashCode.Combine(Action, Actor, Target, Sources.Aggregate(initial, (hash, cell) => hash + coefficient*cell.GetHashCode()));
}

/// <summary>Provides an interface for customizable aspects of AI decision-making for each unit.</summary>
[GlobalClass, Tool, Icon("uid://cvtdbcchxcvon")]
public abstract partial class Behavior : Node
{
    /// <summary>Whether or not the unit is allowed to perform actions that target opposing units.</summary>
    [Export] public bool AllowAttack = true;

    /// <summary>Whether or not the unit is allowed to perform actions that target allied units.</summary>
    [Export] public bool AllowSupport = true;

    /// <returns>The set of cells <paramref name="unit"/> is allowed to move through from its current cell.</returns>
    public virtual IEnumerable<Vector2I> GetTraversableCells(UnitData unit) => unit.GetTraversableCells();

    /// <returns>One of the elements of <paramref name="destinations"/> based on an implementation-defined decision based on the cells <paramref name="unit"/> can traverse.</returns>
    /// <exception cref="ArgumentException">
    /// If no path can be constructed from <paramref name="unit"/>'s cell to any cell in <paramref name="destinations"/> or if <paramref name="destinations"/>
    /// is empty.
    /// </exception>
    public abstract Vector2I ChooseDestination(UnitData unit, IEnumerable<Vector2I> destinations, IEnumerable<Vector2I> traversable);

    /// <inheritdoc cref="ChooseDestination(UnitData, IEnumerable{Vector2I}, IEnumerable{Vector2I})"/>
    public Vector2I ChooseDestination(UnitData unit, IEnumerable<Vector2I> destinations) => ChooseDestination(unit, destinations, GetTraversableCells(unit));

    /// <returns>A path <paramref name="unit"/> can traverse between <paramref name="start"/> and <paramref name="destination"/> through <paramref name="traversable"/>.</returns>
    /// <exception cref="ArgumentException">If a path cannot be constructed between <paramref name="start"/> and <paramref name="destination"/> within <paramref name="traversable"/>.</exception>
    public virtual Path GetPath(UnitData unit, Vector2I start, Vector2I destination, IEnumerable<Vector2I> traversable) => Path.Empty(traversable, unit.CellCost).Add(start).Add(destination);

    /// <inheritdoc cref="GetPath(UnitData, Vector2I, Vector2I, IEnumerable{Vector2I})"/>
    public Path GetPath(UnitData unit, Vector2I start, Vector2I destination) => GetPath(unit, start, destination, GetTraversableCells(unit));

    /// <inheritdoc cref="GetPath(UnitData, Vector2I, Vector2I, IEnumerable{Vector2I})"/>
    public Path GetPath(UnitData unit, Vector2I destination, IEnumerable<Vector2I> traversable) => GetPath(unit, unit.Cell, destination, traversable);

    /// <inheritdoc cref="GetPath(UnitData, Vector2I, Vector2I, IEnumerable{Vector2I})"/>
    public Path GetPath(UnitData unit, Vector2I destination) => GetPath(unit, destination, GetTraversableCells(unit));

    /// <returns>The actions <paramref name="unit"/> can perform based on the ones available to it and where it can move. Avoid returning an empty collection.</returns>
    public abstract IEnumerable<PerformableAction> GetActions(UnitData unit, IEnumerable<UnitAction> available, IEnumerable<Vector2I> traversable);

    /// <inheritdoc cref="GetActions(UnitData, IEnumerable{UnitAction}, IEnumerable{Vector2I})"/>
    public IEnumerable<PerformableAction> GetActions(UnitData unit, IEnumerable<UnitAction> available) => GetActions(unit, available, GetTraversableCells(unit));
}