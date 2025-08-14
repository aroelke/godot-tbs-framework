using System;
using Godot;

namespace TbsTemplate.Nodes.StateChart.Conditions;

/// <summary><see cref="Chart"/> action condition that evaluates a single boolean <see cref="Chart"/> property.</summary>
[GlobalClass, Icon("res://icons/statechart/FlagCondition.svg"), Tool]
public partial class FlagCondition : Condition
{
    /// <summary><see cref="Chart"/> property to evaluate.</summary>
    [Export] public StringName Flag = "";

    public override bool IsSatisfied(ChartNode source) => source.StateChart.GetExpressionProperty<bool>(Flag);
}