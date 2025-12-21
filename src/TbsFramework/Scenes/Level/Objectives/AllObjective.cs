using Godot;

namespace TbsTemplate.Scenes.Level.Objectives;

/// <summary>Aggregate objective that is accomplished when all of its child objectives are accomplished.</summary>
[Tool]
public partial class AllObjective : AggregateObjective
{
    public override bool Aggregator(bool a, bool b) => a && b;
    public override string Operator => "AND";
}