using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control;

[Tool]
public partial class InRangeSwitchCondition : AreaSwitchCondition
{
    [Export] public bool RequiresEveryone = false;

    [Export] public bool Inside = true;

    [Export] public Unit[] SourceUnits = [];

    [Export] public Army[] SourceArmies = [];

    public override void Update(Unit unit)
    {
        List<Unit> sources = [.. SourceUnits];
        foreach (Army army in SourceArmies)
            sources.AddRange(army);

        IEnumerable<Unit> applicable = GetApplicableUnits();
        HashSet<Vector2I> region = [.. sources.SelectMany((u) => u.AttackableCells())];
        Func<Func<Unit, bool>, bool> matcher = RequiresEveryone ? GetApplicableUnits().All : GetApplicableUnits().Any;
        Func<Unit, bool> container = Inside ? (u) => region.Contains(u.Cell) : (u) => !region.Contains(u.Cell);

        Satisfied = matcher(container);
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (SourceUnits.Length == 0 && SourceArmies.Length == 0)
            warnings.Add("No source units have been defined.  There will be no range for trigger units to enter and cause a behavior switch.");

        return [.. warnings];
    }
}