using System;
using Godot;

namespace Nodes.StateChart.Conditions;

/// <summary><see cref="Chart"/> action condition that evaluates a single boolean <see cref="Chart"/> property.</summary>
[GlobalClass, Icon("res://icons/statechart/FlagCondition.svg"), Tool]
public partial class FlagCondition : Condition
{
    /// <summary><see cref="Chart"/> property to evaluate.</summary>
    [Export] public StringName Flag = "";

    public override bool IsSatisfied(ChartNode source)
    {
        if (source.StateChart.ExpressionProperties[Flag].VariantType != Variant.Type.Bool)
            throw new ArgumentException($"Condition value {source.StateChart.ExpressionProperties[Flag]} is not Boolean.");
        return source.StateChart.ExpressionProperties[Flag].AsBool();
    }
}