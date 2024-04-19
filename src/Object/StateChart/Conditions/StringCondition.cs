using System;
using Godot;
using Object.StateChart.States;

namespace Object.StateChart.Conditions;

/// <summary>
/// <see cref="Transition"/> condition that's satisfied based on the value of a string property. If it's exactly equal to the condition's value,
/// the condition is satisfied.
/// </summary>
[GlobalClass, Icon("res://icons/statechart/StringCondition.svg"), Tool]
public partial class StringCondition : Condition
{
    /// <summary>Name of the property to test.</summary>
    [Export] public StringName Property = "";

    /// <summary>Value of the property that satisfies the condition.</summary>
    [Export] public string Value = "";

    public override bool IsSatisfied(Transition transition, State from)
    {
        Node node = from;
        while (IsInstanceValid(node) && node is not Chart)
            node = node.GetParent();
        Chart chart = node as Chart;
        if (!IsInstanceValid(chart))
            throw new ArgumentException("Could not find state chart node.");

        if (chart.ExpressionProperties[Property].VariantType != Variant.Type.String && chart.ExpressionProperties[Property].VariantType != Variant.Type.StringName)
            throw new ArgumentException($"Condition value {chart.ExpressionProperties[Property]} is not a string.");

        return chart.ExpressionProperties[Property].AsString() == Value;
    }
}