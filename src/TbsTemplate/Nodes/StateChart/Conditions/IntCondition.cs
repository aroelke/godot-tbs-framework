using Godot;

namespace TbsTemplate.Nodes.StateChart.Conditions;

/// <summary><see cref="Chart"/> number condition that compares integer values.</summary>
[GlobalClass, Tool]
public partial class IntCondition : NumberCondition<int>
{
    [Export] public override int Value { get; set; } = 0;
}