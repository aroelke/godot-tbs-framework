using System;
using System.Linq;
using Godot;

namespace Nodes.StateChart.Conditions;

/// <summary>
/// <see cref="Chart"/> action condition that's satisfied based on the value of a string property. If it's exactly equal to the condition's value,
/// the condition is satisfied.
/// </summary>
[GlobalClass, Icon("res://icons/statechart/StringCondition.svg"), Tool]
public partial class StringCondition : Condition
{
    private static readonly Variant.Type[] types = new[] { Variant.Type.String, Variant.Type.StringName };

    /// <summary>Name of the property to test.</summary>
    [Export] public StringName Property = "";

    /// <summary>Value of the property that satisfies the condition.</summary>
    [Export] public string Value = "";

    public override bool IsSatisfied(ChartNode source)
    {
        if (!types.Contains(source.StateChart.ExpressionProperties[Property].VariantType))
            throw new ArgumentException($"Condition value {source.StateChart.ExpressionProperties[Property]} is not a string.");
        return source.StateChart.ExpressionProperties[Property].AsString() == Value;
    }
}