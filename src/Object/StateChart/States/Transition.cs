using System;
using System.Collections.Generic;
using Godot;
using Object.StateChart.Conditions;

namespace Object.StateChart.States;

/// <summary>Transition between state chart <see cref="State"/>s. </summary>
[Icon("res://icons/statechart/Transition.svg"), Tool]
public partial class Transition : ChartNode
{
    /// <summary>Signals the transition is taken, but before the active <see cref="State"/> is actually exited.</summary>
    [Signal] public delegate void TakenEventHandler();

    /// <summary>State to activate if the transition is taken.</summary>
    [Export] public State To = null;

    /// <summary>Event triggering the transition. Leave blank to cause the transition to immediately trigger upon entering the state.</summary>
    [Export] public StringName Event = "";

    /// <summary>Condition guarding the transition. The transition will only be taken if the condition is satisfied.</summary>
    [Export] public Condition Condition = new UnCondition();

    /// <summary>Whether or not the transition should wait for an event before triggering.</summary>
    public bool Automatic => Event.IsEmpty;

    /// <returns><c>true</c> if there is no <see cref="Condition"/> or if it is satisfied, and <c>false</c> otherwise.</returns>
    /// <exception cref="InvalidCastException">If this transition's parent isn't a <see cref="State"/></exception>
    public bool EvaluateCondition() => Condition.IsSatisfied(this);

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        if (GetChildCount() > 0)
            warnings.Add("Transitions should not have children.");

        if (GetParentOrNull<State>() is null)
            warnings.Add("Transitions must be children of states.");

        return warnings.ToArray();
    }
}