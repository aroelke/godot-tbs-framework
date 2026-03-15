using Godot;

namespace TbsFramework.Nodes.StateCharts.Conditions;

/// <summary>Condition guarding a state <see cref="Transition"/> or <see cref="Reactions.StateReaction"/>.</summary>
[GlobalClass, Icon("uid://cw1kpwg35770c"), Tool]
public abstract partial class StateCondition : Resource
{
    /// <param name="source">State chart node providing information about the condition.</param>
    /// <returns><c>true</c> if the transition should be taken, and <c>false</c> otherwise.</returns>
    public abstract bool IsSatisfied(ChartNode source);
}