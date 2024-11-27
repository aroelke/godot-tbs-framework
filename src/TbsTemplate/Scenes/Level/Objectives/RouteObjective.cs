using System.Collections.Generic;
using Godot;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Objectives;

/// <summary>Objective that's accomplished when all units in an army are defeated.</summary>
[Tool]
public partial class RouteObjective : Objective
{
    /// <summary>Army that needs to be routed.</summary>
    [Export] public Army Target = null;

    public override bool Complete => Target is not null && Target.GetChildCount() == 0;
    public override string Description => Target is null ? "" : $"Route {Target.Name}";

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (Target is null)
            warnings.Add("There is no army to track. This objective can't be completed.");

        return [.. warnings];
    }
}