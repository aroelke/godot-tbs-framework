using Godot;

namespace TbsTemplate.Nodes.StateChart.Conditions;

/// <summary>Condition guarding a state <see cref="Transition"/> or <see cref="Reactions.Reaction"/>.</summary>
[GlobalClass, Tool]
public abstract partial class Condition : Resource
{
    /// <param name="source">State chart node providing information about the condition.</param>
    /// <returns><c>true</c> if the transition should be taken, and <c>false</c> otherwise.</returns>
    public abstract bool IsSatisfied(ChartNode source);
}