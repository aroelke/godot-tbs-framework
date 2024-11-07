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

    public override string Description => Target is null ? "" : $"Route {Target.Name}";

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (Target is null)
            warnings.Add("There is no army to track. This objective can't be completed.");

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint() && Target is not null)
        {
            UnitEvents.Singleton.UnitDefeated += (u) => {
                if (Target.Contains(u) && Target.Count == 1 /* The dying unit has not yet left the army */)
                    Completed = true;
            };
        }
    }
}