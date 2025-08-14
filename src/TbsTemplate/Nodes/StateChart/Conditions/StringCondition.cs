using System;
using System.Linq;
using Godot;

namespace TbsTemplate.Nodes.StateChart.Conditions;

/// <summary>
/// <see cref="Chart"/> action condition that's satisfied based on the value of a string property. If it's exactly equal to the condition's value,
/// the condition is satisfied.
/// </summary>
[GlobalClass, Icon("res://icons/statechart/StringCondition.svg"), Tool]
public partial class StringCondition : Condition
{
    /// <summary>Name of the property to test.</summary>
    [Export] public StringName Property = "";

    /// <summary>Value of the property that satisfies the condition.</summary>
    [Export] public string Value = "";

    public override bool IsSatisfied(ChartNode source) => source.StateChart.GetVariable<string>(Property) == Value;
}