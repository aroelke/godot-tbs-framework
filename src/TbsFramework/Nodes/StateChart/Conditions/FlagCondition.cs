using Godot;

namespace TbsTemplate.Nodes.StateCharts.Conditions;

/// <summary><see cref="StateChart"/> action condition that evaluates a single boolean <see cref="StateChart"/> property.</summary>
[GlobalClass, Tool]
public partial class FlagCondition : StateCondition
{
    /// <summary><see cref="StateChart"/> property to evaluate.</summary>
    [Export] public StringName Flag = "";

    public override bool IsSatisfied(ChartNode source) => source.StateChart.GetVariable<bool>(Flag);
}