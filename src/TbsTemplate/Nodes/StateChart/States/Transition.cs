using System;
using System.Collections.Generic;
using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes.StateChart.Conditions;

namespace TbsTemplate.Nodes.StateChart.States;

/// <summary>Transition between <see cref="State"/>s. </summary>
[Icon("res://icons/statechart/Transition.svg"), Tool]
public partial class Transition : ChartNode
{
    /// <summary>Signals the transition is taken, but before the active <see cref="State"/> is actually exited.</summary>
    [Signal] public delegate void TakenEventHandler();

    /// <summary><see cref="State"/> to activate if the transition is taken.</summary>
    [Export] public State To = null;

    /// <summary>Condition guarding the transition. The transition will only be taken if the condition is satisfied.</summary>
    [Export] public Condition Condition = null;

    /// <summary>Event triggering the transition. Leave blank to cause the transition to immediately trigger upon entering the <see cref="State"/>.</summary>
    public StringName Event { get; private set; } = "";

    /// <summary>Whether or not the transition should wait for an event before triggering.</summary>
    public bool Automatic => Event.IsEmpty;

    /// <returns><c>true</c> if the <see cref="Conditions.Condition"/> is satisfied, and <c>false</c> otherwise.</returns>
    /// <exception cref="InvalidCastException">If this transition's parent isn't a <see cref="State"/></exception>
    public bool EvaluateCondition() => Condition.IsSatisfied(this);

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (GetChildCount() > 0)
            warnings.Add("Transitions should not have children.");

        if (GetParentOrNull<State>() is null)
            warnings.Add("Transitions must be children of states.");

        return [.. warnings];
    }

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = new(base._GetPropertyList() ?? []);

        if (StateChart is not null)
            properties.Add(StateChart.CreateEventProperty(PropertyName.Event));
        else
        {
            properties.Add(new ObjectProperty(
                PropertyName.Event,
                Variant.Type.StringName,
                Usage:PropertyUsageFlags.ReadOnly
            ));
        }

        return properties;
    }

    public override Variant _Get(StringName property) => property == PropertyName.Event ? Event : base._Get(property);

    public override bool _Set(StringName property, Variant value)
    {
        if (property == PropertyName.Event)
        {
            Event = value.AsStringName();
            return true;
        }
        else
            return base._Set(property, value);
    }

    public override bool _PropertyCanRevert(StringName property) => property == PropertyName.Event || base._PropertyCanRevert(property);

    public override Variant _PropertyGetRevert(StringName property) => property == PropertyName.Event ? new StringName("") : base._PropertyGetRevert(property);
}