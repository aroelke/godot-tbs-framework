using Godot;
using Object.StateChart.States;

namespace Object.StateChart.Conditions;

/// <summary>Condition guarding a state <see cref="Transition"/>.</summary>
[GlobalClass, Tool]
public abstract partial class Condition : Resource
{
    /// <summary>Determine if the condition for a transition from a state is satisfied.</summary>
    /// <returns><c>true</c> if the transition should be taken, and <c>false</c> otherwise.</returns>
    public abstract bool IsSatisfied(Transition transition, State from);
}