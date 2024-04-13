using System;
using Godot;
using Object.StateChart.States;

namespace Object.StateChart.Conditions;

/// <summary><see cref="Transition"/> condition that evaluates a single boolean <see cref="Chart"/> property.</summary>
[GlobalClass, Icon("res://icons/statechart/FlagCondition.svg"), Tool]
public partial class FlagCondition : Condition
{
    /// <summary><see cref="Chart"/> property to evaluate.</summary>
    [Export] public StringName Flag = "";

    public override bool IsSatisfied(Transition transition, State from)
    {
        Node node = from;
        while (IsInstanceValid(node) && node is not Chart)
            node = node.GetParent();
        Chart chart = node as Chart;
        if (!IsInstanceValid(chart))
            throw new ArgumentException("Could not find state chart node.");
        
        return chart.ExpressionProperties[Flag].AsBool();
    }
}