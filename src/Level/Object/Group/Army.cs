using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Level.Object.Group;

/// <summary>A group of <c>Unit</c> <c>GridNode</c>s that has allies and enemies.</summary>
[Tool]
public partial class Army : GridNodeGroup, IEnumerable<Unit>
{
    /// <summary>Color to use for units in this army.</summary>
    [Export] public Color Color = Colors.White;

    /// <summary>Armies to which this army is allied (not including itself).</summary>
    [Export] public Army[] Allies = Array.Empty<Army>();

    /// <param name="other">Army to check.</param>
    /// <returns><c>true</c> if the other army is allied with this one, and <c>false</c> otherwise.</returns>
    public bool AlliedTo(Army other) => other == this || Allies.Contains(other);

    /// <param name="unit">Unit to check.</param>
    /// <returns><c>true</c> if the unit is in this army or an allied one, and <c>false</c> otherwise.</returns>
    public bool AlliedTo(Unit unit) => Contains(unit) || AlliedTo(unit.Affiliation);

    public Unit Previous(Unit unit)
    {
        Unit[] units = GetChildren().OfType<Unit>().ToArray();
        if (units.Length <= 1)
            return null;

        int index = Array.IndexOf(units, unit);
        if (index < 0)
            return null;
        else if (index == 0)
            return units.Last();
        else
            return units[index - 1];
    }

    public Unit Next(Unit unit)
    {
        Unit[] units = GetChildren().OfType<Unit>().ToArray();
        if (units.Length <= 1)
            return null;

        int index = Array.IndexOf(units, unit);
        if (index < 0)
            return null;
        else if (index < units.Length - 1)
            return units[index + 1];
        else
            return units[0];
    }

    /// <summary>When a <c>Unit</c> is added to the army, update its affiliation to this army.</summary>
    /// <param name="child">Node that was just added.</param>
    public void OnChildEnteredTree(Node child)
    {
        if (child is Unit unit)
            unit.Affiliation = this;
    }

    /// <summary>When a <c>Unit</c> is about to leave the army, clear its affiliation.</summary>
    /// <param name="child">Node that's leaving the tree.</param>
    public void OnChildExitingTree(Node child)
    {
        if (child is Unit unit && unit.Affiliation == this)
            unit.Affiliation = null;
    }

    public override void _Ready()
    {
        base._Ready();

        foreach (Unit unit in (IEnumerable<Unit>)this)
            unit.Affiliation = this;
    }

    IEnumerator<Unit> IEnumerable<Unit>.GetEnumerator() => GetChildren().OfType<Unit>().GetEnumerator();
}