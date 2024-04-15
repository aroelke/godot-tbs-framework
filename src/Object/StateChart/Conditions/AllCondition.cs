using System;
using System.Linq;
using Godot;
using Object.StateChart.States;

namespace Object.StateChart.Conditions;

/// <summary><see cref="Transition"/> condition that is satisfied if all of a list of constituent conditions are satisfied, or if there aren't any.</summary>
[GlobalClass, Icon("res://icons/statechart/AllCondition.svg"), Tool]
public partial class AllCondition : Condition
{
    /// <summary>Conditions that must be satisfied for this one to be satisfied.</summary>
    [Export] public Condition[] Conditions = Array.Empty<Condition>();

    public override bool IsSatisfied(Transition transition, State from) => !Conditions.Any() || Conditions.All((c) => c.IsSatisfied(transition, from));
}