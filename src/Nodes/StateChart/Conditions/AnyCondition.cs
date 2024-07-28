using System.Linq;
using Godot;

namespace Nodes.StateChart.Conditions;

/// <summary><see cref="Chart"/> action condition that is satisfied if any of a set of conditions is satisfied or if there are no conditions to satisfy.</summary>
[GlobalClass, Icon("res://icons/statechart/AnyCondition.svg"), Tool]
public partial class AnyCondition : Condition
{
    /// <summary>Conditions that can be satisfied for this one to be satisfied.</summary>
    [Export] public Condition[] Conditions = [];

    public override bool IsSatisfied(ChartNode source) => Conditions.Length == 0 || Conditions.Any((c) => c.IsSatisfied(source));
}