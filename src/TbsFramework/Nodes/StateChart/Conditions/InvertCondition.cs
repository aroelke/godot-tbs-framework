using Godot;

namespace TbsTemplate.Nodes.StateCharts.Conditions;

/// <summary>
/// <see cref="StateChart"/> action condition that evaluates as the Boolean opposite of another condition, i.e. that condition should not be satisfied
/// for this one to be satisfied.  Is always unsatisfied if there is no condition.
/// </summary>
[GlobalClass, Tool]
public partial class InvertCondition : StateCondition
{
    /// <summary>Condition to invert.</summary>
    [Export] public StateCondition Condition;

    public override bool IsSatisfied(ChartNode source) => Condition is not null && !Condition.IsSatisfied(source);
}