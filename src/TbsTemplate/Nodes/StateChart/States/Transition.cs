using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes.StateCharts.Conditions;

namespace TbsTemplate.Nodes.StateCharts.States;

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

    public StringName Event { get; private set; } = "";

    /// <summary>Whether or not the transition should wait for an event before triggering.</summary>
    public bool Automatic => Event.IsEmpty;

    /// <returns><c>true</c> if the <see cref="Conditions.Condition"/> is satisfied, and <c>false</c> otherwise.</returns>
    /// <exception cref="InvalidCastException">If this transition's parent isn't a <see cref="State"/></exception>
    public bool EvaluateCondition() => Condition.IsSatisfied(this);

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (GetChildCount() > 0)
            warnings.Add("Transitions should not have children.");

        if (GetParentOrNull<State>() is null)
            warnings.Add("Transitions must be children of states.");

        if (!Event.IsEmpty && (!StateChart?.Events.Contains(Event) ?? false))
            warnings.Add($"State chart does not have event {Event}");

        return [.. warnings];
    }

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = base._GetPropertyList() ?? [];

        if (StateChart is not null)
            properties.Add(ObjectProperty.CreateEnumProperty(PropertyName.Event, StateChart.Events));
        else
        {
            properties.Add(new ObjectProperty(
                "Event",
                Variant.Type.StringName,
                Usage:PropertyUsageFlags.ReadOnly
            ));
        }

        return properties;
    }

    public override Variant _Get(StringName property)
    {
        if (property == PropertyName.Event)
            return Event;
        else
            return base._Get(property);
    }

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

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (property == PropertyName.Event)
            return new StringName("");
        else
            return base._PropertyGetRevert(property);
    }
}