using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TbsFramework.Nodes.StateCharts.States;

/// <summary>A <see cref="State"/> that can't have any sub-<see cref="State"/>s.</summary>
[Icon("res://icons/statechart/SimpleState.svg"), Tool]
public partial class SimpleState : State
{
    public override void HandleTransition(StateTransition transition, State from) => GetParent<State>().HandleTransition(transition, from);

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (GetChildren().OfType<State>().Any())
            warnings.Add("Child states of atomic states can't be transitioned to.");

        return [.. warnings];
    }
}