using System.Collections.Generic;
using Godot;
using TbsTemplate.Nodes.StateChart.Conditions;
using TbsTemplate.Nodes.StateChart.States;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary><see cref="State"/>-specific reaction to a trigger that should cause an action without a transition.</summary>
[GlobalClass, Icon("res://icons/statechart/Reaction.svg"), Tool]
public partial class BaseReaction : ChartNode
{
    private State _state = null;

    /// <summary>Condition guarding the reaction; must be satisfied for the reaction to occur when the trigger arrives.</summary>
    [Export] public Condition Condition = null;

    /// <summary>Whether or not the reaction can trigger.</summary>
    public bool Active => (_state?.Active ?? false) && Condition.IsSatisfied(this);

    public override void _Ready()
    {
        base._Ready();
        _state = GetParentOrNull<State>();
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (GetParent() is not State)
            warnings.Add("Reactions should be children of states.");

        return [.. warnings];
    }
}