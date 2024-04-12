using Godot;
using Object.StateChart.States;

namespace Object.StateChart.Conditions;

/// <summary>
/// <see cref="Transition"/> condition that evaluates as the Boolean opposite of another condition, i.e. that condition should not be satisfied
/// for this one to be satisfied.  Is always unsatisfied if there is no condition.
/// </summary>
public partial class InvertCondition : Condition
{
    /// <summary>Condition to invert.</summary>
    [Export] public Condition Condition;

    public override bool IsSatisfied(Transition transition, State from) => Condition is not null && !Condition.IsSatisfied(transition, from);
}