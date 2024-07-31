using Godot;

namespace TbsTemplate.Nodes.StateChart.Conditions;

/// <summary>
/// A <see cref="Chart"/> action condition that is always satisfied. Mostly used as a default value for automatic
/// <see cref="States.Transition"/>s and <see cref="Reactions.Reaction"/>s.
/// </summary>
[GlobalClass, Icon("res://icons/statechart/UnCondition.svg"), Tool]
public partial class UnCondition : Condition
{
    public override bool IsSatisfied(ChartNode _) => true;
}