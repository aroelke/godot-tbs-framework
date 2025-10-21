using Godot;

namespace TbsTemplate.Nodes.StateCharts.Conditions;

/// <summary>
/// A <see cref="StateChart"/> action condition that is always satisfied. Mostly used as a default value for automatic
/// <see cref="States.StateTransition"/>s and <see cref="Reactions.Reaction"/>s.
/// </summary>
[GlobalClass, Tool]
public partial class UnCondition : StateCondition
{
    public override bool IsSatisfied(ChartNode _) => true;
}