using System;
using Godot;

namespace TbsTemplate.Nodes.StateCharts.Conditions;

/// <summary><see cref="StateChart"/> action condition that evaluates a single boolean <see cref="StateChart"/> property.</summary>
[GlobalClass, Icon("res://icons/statechart/FlagCondition.svg"), Tool]
public partial class FlagCondition : Condition
{
    /// <summary><see cref="StateChart"/> property to evaluate.</summary>
    [Export] public StringName Flag = "";

    public override bool IsSatisfied(ChartNode source) => source.StateChart.GetVariable<bool>(Flag);
}