using System.Collections.Generic;
using Godot;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control;

public abstract partial class SwitchCondition : Node
{
    [Export] public Unit[] ApplicableUnits = [];

    [Export] public Army[] ApplicableArmies = [];

    public IEnumerable<Unit> GetApplicableUnits()
    {
        List<Unit> applicable = [.. ApplicableUnits];
        foreach (Army army in ApplicableArmies)
            applicable.AddRange(army);
        return applicable;
    }

    public abstract bool Satisfied();

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (ApplicableUnits.Length == 0 || ApplicableArmies.Length == 0)
            warnings.Add("This condition doesn't apply to any units and will never be satisfied.");

        return [.. warnings];
    }
}