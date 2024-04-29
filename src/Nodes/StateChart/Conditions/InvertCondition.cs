using Godot;

namespace Nodes.StateChart.Conditions;

/// <summary>
/// <see cref="Chart"/> action condition that evaluates as the Boolean opposite of another condition, i.e. that condition should not be satisfied
/// for this one to be satisfied.  Is always unsatisfied if there is no condition.
/// </summary>
[GlobalClass, Icon("res://icons/statechart/InvertCondition.svg"), Tool]
public partial class InvertCondition : Condition
{
    /// <summary>Condition to invert.</summary>
    [Export] public Condition Condition;

    public override bool IsSatisfied(ChartNode source) => Condition is not null && !Condition.IsSatisfied(source);
}