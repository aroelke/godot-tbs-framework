using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Data;
using TbsFramework.Scenes.Level.Control;

namespace TbsFramework.Scenes.Level.Object.Group;

/// <summary>A group of <see cref="Unit"/> <see cref="GridNode"/>s that has allies and enemies.</summary>
[GlobalClass, Tool]
public partial class Army : GridNodeGroup, IEnumerable<Unit>
{
    private ArmyController _controller = null;
    public ArmyController Controller => _controller ??= GetChildren().OfType<ArmyController>().FirstOrDefault();

    /// <summary>Faction units in this army belong to.</summary>
    [Export] public Faction Faction = null;

    /// <returns>The collection of units that belong to this army.</returns>
    public IEnumerable<Unit> Units() => GetChildren().OfType<Unit>();

    /// <summary>Find the "previous" unit in the list, looping around to the end if needed.</summary>
    /// <remarks>"Previous" is arbitrarily defined by the order each unit was inserted into the <see cref="SceneTree"/>.</remarks>
    /// <param name="unit">Unit to find the previous of.</param>
    /// <returns>
    /// The <see cref="Unit"/> before <paramref name="unit"/> in the army's children, or the last one if <paramref name="unit"/>
    /// is the first.
    /// </returns>
    public Unit Previous(Unit unit)
    {
        Unit[] units = [.. Units()];
        if (units.Length <= 1)
            return null;

        int index = Array.IndexOf(units, unit);
        if (index < 0)
            return null;
        else if (index == 0)
            return units[^1];
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
        Unit[] units = [.. Units()];
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
            unit.Army = this;
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (GetChildren().OfType<ArmyController>().Count() > 1)
            warnings.Add("There are too many unit controllers.  Only the first one will be used.");

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        foreach (Unit unit in (IEnumerable<Unit>)this)
            unit.Army = this;
    }

    IEnumerator<Unit> IEnumerable<Unit>.GetEnumerator() => Units().GetEnumerator();
}