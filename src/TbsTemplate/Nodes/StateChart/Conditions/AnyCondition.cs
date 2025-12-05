using System.Linq;
using Godot;

namespace TbsTemplate.Nodes.StateCharts.Conditions;

/// <summary><see cref="StateChart"/> action condition that is satisfied if any of a set of conditions is satisfied or if there are no conditions to satisfy.</summary>
[GlobalClass, Tool]
public partial class AnyCondition : StateCondition
{
    /// <summary>Conditions that can be satisfied for this one to be satisfied.</summary>
    [Export] public StateCondition[] Conditions = [];

    public override bool IsSatisfied(ChartNode source) => Conditions.Length == 0 || Conditions.Any((c) => c.IsSatisfied(source));
}