using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace level.Object.Group;

/// <summary>A group of <c>Unit</c> <c>GridNode</c>s that has allies and enemies.</summary>
public partial class Army : GridNodeGroup, IEnumerable<Unit>
{
    public Army[] Allies = Array.Empty<Army>();

    /// <param name="other">Army to check.</param>
    /// <returns><c>true</c> if the other army is allied with this one, and <c>false</c> otherwise.</returns>
    public bool AlliedTo(Army other) => other == this || Allies.Contains(other);

    /// <param name="unit">Unit to check.</param>
    /// <returns><c>true</c> if the unit is in this army or an allied one, and <c>false</c> otherwise.</returns>
    public bool AlliedTo(Unit unit) => Contains(unit) || AlliedTo(unit.Affiliation);

    /// <summary>Add a new <c>Unit</c> to this army and, if successful, update its affiliation.</summary>
    /// <see cref="Node.AddChild(Node, bool, InternalMode)"/>
    public void AddUnit(Unit unit)
    {
        if (unit.GetParent() is null)
            unit.Affiliation = this;
        AddChild(unit);
    }

    /// <summary>Remove a <c>Unit</c> and clear its affiliation if it's in this army.</summary>
    /// <see cref="Node.RemoveChild(Node)"/>
    public void RemoveUnit(Unit unit)
    {
        if (Contains(unit))
        {
            unit.Affiliation = null;
            RemoveChild(unit);
        }
    }

    public override void _Ready()
    {
        base._Ready();

        foreach (Unit unit in (IEnumerable<Unit>)this)
            unit.Affiliation = this;
    }

    IEnumerator<Unit> IEnumerable<Unit>.GetEnumerator() => GetChildren().OfType<Unit>().GetEnumerator();
}