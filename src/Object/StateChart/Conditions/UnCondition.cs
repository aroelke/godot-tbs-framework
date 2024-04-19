using Godot;
using Object.StateChart.States;

namespace Object.StateChart.Conditions;

/// <summary>
/// A <see cref="Transition"/> condition that is always satisfied. Mostly used as a default value for automatic
/// <see cref="Transition"/>s.
/// </summary>
[GlobalClass, Icon("res://icons/statechart/UnCondition.svg"), Tool]
public partial class UnCondition : Condition
{
    public override bool IsSatisfied(Transition transition, State from) => true;
}