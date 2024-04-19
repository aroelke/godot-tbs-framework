using System;
using System.Linq;
using Godot;
using Object.StateChart.States;

namespace Object.StateChart.Conditions;

/// <summary><see cref="Transition"/> condition that is satisfied if any of a set of conditions is satisfied or if there are no conditions to satisfy.</summary>
[GlobalClass, Icon("res://icons/statechart/AnyCondition.svg"), Tool]
public partial class AnyCondition : Condition
{
    /// <summary>Conditions that can be satisfied for this one to be satisfied.</summary>
    [Export] public Condition[] Conditions = Array.Empty<Condition>();

    public override bool IsSatisfied(Transition transition, State from) => !Conditions.Any() || Conditions.Any((c) => c.IsSatisfied(transition, from));
}