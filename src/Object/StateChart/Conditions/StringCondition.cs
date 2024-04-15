using System;
using Godot;
using Object.StateChart.States;

namespace Object.StateChart.Conditions;

[GlobalClass, Tool]
public partial class StringCondition : Condition
{
    [Export] public StringName Property = "";

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