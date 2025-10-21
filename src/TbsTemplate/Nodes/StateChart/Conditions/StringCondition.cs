using Godot;

namespace TbsTemplate.Nodes.StateCharts.Conditions;

/// <summary>
/// <see cref="StateChart"/> action condition that's satisfied based on the value of a string property. If it's exactly equal to the condition's value,
/// the condition is satisfied.
/// </summary>
[GlobalClass, Tool]
public partial class StringCondition : StateCondition
{
    /// <summary>Name of the property to test.</summary>
    [Export] public StringName Property = "";

    /// <summary>Value of the property that satisfies the condition.</summary>
    [Export] public string Value = "";

    public override bool IsSatisfied(ChartNode source) => source.StateChart.GetVariable<string>(Property) == Value;
}