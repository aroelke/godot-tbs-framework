using Godot;

namespace TbsFramework.Nodes.StateCharts.Conditions;

/// <summary><see cref="StateChart"/> number condition that compares integer values.</summary>
[GlobalClass, Tool]
public partial class IntCondition : NumberCondition<int>
{
    [Export] public override int Value { get; set; } = 0;
}