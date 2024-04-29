using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Object.StateChart.States;

/// <summary>A <see cref="State"/> that can't have any sub-<see cref="State"/>s.</summary>
[Icon("res://icons/statechart/AtomicState.svg"), Tool]
public partial class AtomicState : State
{
    public override void HandleTransition(Transition transition, State from) => GetParent<State>().HandleTransition(transition, from);

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        if (GetChildren().OfType<State>().Any())
            warnings.Add("Child states of atomic states can't be transitioned to.");

        return warnings.ToArray();
    }
}