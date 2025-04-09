using System.Collections.Generic;
using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Objectives;

/// <summary>Objective that's accomplished when a specific unit is defeated. Reassigning the target unit will uncomplete the objective.</summary>
[Tool]
public partial class DefeatUnitObjective : Objective
{
    /// <summary>Unit to defeat to accomplish the objective.</summary>
    [Export] public UnitRenderer Target = null;

    public override bool Complete => Target is not null && (!IsInstanceValid(Target) || !Target.IsInsideTree());
    public override string Description => Target is null ? "" : $"Defeat {Target.Name}";

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (Target is null)
            warnings.Add("A target needs to be set for this objective to be completable.");
        
        return [.. warnings];
    }
}