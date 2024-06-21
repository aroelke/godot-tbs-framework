using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Scenes.Level.Object.Group;

/// <summary>A group of <see cref="Unit"/> <see cref="GridNode"/>s that has allies and enemies.</summary>
[Tool]
public partial class Army : GridNodeGroup, IEnumerable<Unit>
{
    /// <summary>Color to use for units in this army.</summary>
    [Export] public Color Color = Colors.White;

    /// <summary>Armies to which this army is allied (not including itself).</summary>
    [Export] public Army[] Allies = Array.Empty<Army>();

    /// <param name="other">Army to check.</param>
    /// <returns><c>true</c> if <paramref name="other"/> is allied with this one, and <c>false</c> otherwise.</returns>
    public bool AlliedTo(Army other) => other == this || Allies.Contains(other);

    /// <param name="unit">Unit to check.</param>
    /// <returns><c>true</c> if <paramref name="unit"/> is in this army or an allied one, and <c>false</c> otherwise.</returns>
    public bool AlliedTo(Unit unit) => Contains(unit) || AlliedTo(unit.Affiliation);

    /// <summary>Find the "previous" unit in the list, looping around to the end if needed.</summary>
    /// <remarks>"Previous" is arbitrarily defined by the order each unit was inserted into the <see cref="SceneTree"/>.</remarks>
    /// <param name="unit">Unit to find the previous of.</param>
    /// <returns>
    /// The <see cref="Unit"/> before <paramref name="unit"/> in the army's children, or the last one if <paramref name="unit"/>
    /// is the first.
    /// </returns>
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

    /// <summary>Find the "next" unit in the list, looping around to the beginning if needed.</summary>
    /// <remarks>"Next" is arbitrarily defined by the order each unit was inserted into the <see cref="SceneTree"/>.</remarks>
    /// <param name="unit">Unit to find the next of.</param>
    /// <returns>
    /// The <see cref="Unit"/> after <paramref name="unit"/> in the army's children, or the first one if <paramref name="unit"/>
    /// is the last.
    /// </returns>
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

    /// <summary>When a <see cref="Unit"/> is added to the army, update its <see cref="Unit.Affiliation"/> to this army.</summary>
    /// <param name="child">Node that was just added.</param>
    public void OnChildEnteredTree(Node child)
    {
        if (child is Unit unit)
            unit.Affiliation = this;
    }

    public override void _Ready()
    {
        base._Ready();

        foreach (Unit unit in (IEnumerable<Unit>)this)
            unit.Affiliation = this;
    }

    IEnumerator<Unit> IEnumerable<Unit>.GetEnumerator() => GetChildren().OfType<Unit>().GetEnumerator();
}