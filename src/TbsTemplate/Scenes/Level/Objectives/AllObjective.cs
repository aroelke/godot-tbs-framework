using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TbsTemplate.Scenes.Level.Objectives;

/// <summary>Aggregate objective that is accomplished when all of its child objectives are accomplished.</summary>
[Tool]
public partial class AllObjective : Objective
{
    public override string Description => string.Join(" AND ", GetChildren().OfType<Objective>().Select((o) => o.Description));

    private void Update() => Completed = GetChildren().OfType<Objective>().Select(static (o) => o.Completed).Aggregate(static (a, b) => a && b);

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (!GetChildren().OfType<Objective>().Any())
            warnings.Add("Aggregate objective has no children.");

        foreach (Node child in GetChildren())
        {
            if (child is not Objective)
            {
                warnings.Add("The only children of Objectives should be Objectives.");
                break;
            }
        }

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            foreach (Objective objective in GetChildren().OfType<Objective>())
            {
                objective.Accomplished += Update;
                objective.Relinquished += Update;
            }
        }
    }
}