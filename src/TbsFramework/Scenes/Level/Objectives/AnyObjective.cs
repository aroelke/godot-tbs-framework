using Godot;

namespace TbsFramework.Scenes.Level.Objectives;

/// <summary>Aggregate objective that is accomplished when any of its child objectives are accomplished.</summary>
[Tool]
public partial class AnyObjective : AggregateObjective
{
    public override bool Aggregator(bool a, bool b) => a || b;
    public override string Operator => "OR";
}