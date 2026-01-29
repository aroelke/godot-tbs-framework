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