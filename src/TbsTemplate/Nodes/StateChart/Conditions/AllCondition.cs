using System.Linq;
using Godot;

namespace TbsTemplate.Nodes.StateCharts.Conditions;

/// <summary><see cref="StateChart"/> action condition that is satisfied if all of a list of constituent conditions are satisfied, or if there aren't any.</summary>
[GlobalClass, Tool]
public partial class AllCondition : StateCondition
{
    /// <summary>Conditions that must be satisfied for this one to be satisfied.</summary>
    [Export] public StateCondition[] Conditions = [];

    public override bool IsSatisfied(ChartNode source) => Conditions.Length == 0 || Conditions.All((c) => c.IsSatisfied(source));
}