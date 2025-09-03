using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Behavior switch condition that triggers based on units being in attack range of other units.</summary>
[Tool]
public partial class InRangeSwitchCondition : AreaSwitchCondition
{
    /// <summary>Set of units explicitly used to determine the attack range.</summary>
    [Export] public Unit[] SourceUnits = [];

    /// <summary>Armies containing the units to use for determining attack range, even if they're created later.</summary>
    [Export] public Army[] SourceArmies = [];

    public override HashSet<Vector2I> GetRegion()
    {
        List<Unit> sources = [.. SourceUnits];
        foreach (Army army in SourceArmies)
            sources.AddRange(army);
        return [.. sources.SelectMany((u) => u.AttackableCells(u.TraversableCells()))];
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (SourceUnits.Length == 0 && SourceArmies.Length == 0)
            warnings.Add("No source units have been defined.  There will be no range for trigger units to enter and cause a behavior switch.");

        return [.. warnings];
    }
}