using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TbsTemplate.Scenes.Level.Objectives;

/// <summary>Objective that acts as an aggregate of other objectives, combining a set of them together to determine overall completion.</summary>
public abstract partial class AggregateObjective : Objective
{
    /// <summary>String describing how two objectives will be combined. Used for displaying the combined description of all child objectives.</summary>
    public abstract string Operator { get; }

    public override bool Complete => GetChildren().OfType<Objective>().Select(static (o) => o.Complete).Aggregate(Aggregator);
    public override string Description => string.Join($" {Operator} ", GetChildren().OfType<Objective>().Select((o) => o.Description));

    /// <summary>Function combining the completion status of two objectives. Aggregated over all child objectives to determine overall completion.</summary>
    /// <param name="a">Completion of the first objective.</param>
    /// <param name="b">Completion of the second objective.</param>
    /// <returns>The combined completion status of the two objectives.</returns>
    public abstract bool Aggregator(bool a, bool b);

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
}