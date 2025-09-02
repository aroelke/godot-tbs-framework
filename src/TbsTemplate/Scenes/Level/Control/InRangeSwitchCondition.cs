using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control;

[Tool]
public partial class InRangeSwitchCondition : AreaSwitchCondition
{
    [Export] public Unit[] SourceUnits = [];

    [Export] public Army[] SourceArmies = [];

    public override HashSet<Vector2I> GetRegion()
    {
        List<Unit> sources = [.. SourceUnits];
        foreach (Army army in SourceArmies)
            sources.AddRange(army);
        return [.. sources.SelectMany((u) => u.AttackableCells())];
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (SourceUnits.Length == 0 && SourceArmies.Length == 0)
            warnings.Add("No source units have been defined.  There will be no range for trigger units to enter and cause a behavior switch.");

        return [.. warnings];
    }
}