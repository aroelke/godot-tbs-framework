using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Behavior switch condition that triggers based on units being in range to perform an action on other units.</summary>
[Tool]
public partial class InRangeSwitchCondition : AreaSwitchCondition
{
    /// <summary>Action whose range determines the region that controls the behavior switch.</summary>
    [Export] public UnitAction Action = null;

    /// <summary>Set of units explicitly used to determine the attack range.</summary>
    [Export] public Unit[] SourceUnits = [];

    /// <summary>Armies containing the units to use for determining attack range, even if they're created later.</summary>
    [Export] public Army[] SourceArmies = [];

    public override HashSet<Vector2I> GetRegion()
    {
        List<Unit> sources = [.. SourceUnits.Where(IsInstanceValid)];
        foreach (Army army in SourceArmies)
            sources.AddRange(army);
        return Action is null ? [] : [.. sources.SelectMany((u) => Action.GetValidTargetCells(u.UnitData))];
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (SourceUnits.Length == 0 && SourceArmies.Length == 0)
            warnings.Add("No source units have been defined. There will be no range for trigger units to enter and cause a behavior switch.");
        if (Action is null)
            warnings.Add("No action defined to determine the range for. This will prevent the behavior from switching.");

        return [.. warnings];
    }
}