using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes.StateChart.States;

namespace TbsTemplate.Nodes.StateChart;

/// <summary>
/// A state chart modeled after <see href="https://github.com/derkork/godot-statecharts"/>. Contains a hierarchy of <see cref="State"/>s that have
/// zero or more <see cref="Transition"/>s to other states within the hierarchy and <see cref="Reactions.Reaction"/>s to specific events. While a
/// <see cref="State"/> is active, it can receive events, which cause it to trigger <see cref="Transition"/>s to other <see cref="State"/>s or
/// emit signals.
/// </summary>
[Icon("res://icons/statechart/Chart.svg"), Tool]
public partial class Chart : Node
{
    /// <summary>Signals that the state chart has received an event before any transitions are processed.</summary>
    /// <param name="event">Name of the received event.</param>
    [Signal] public delegate void EventReceivedEventHandler(StringName @event);

    private static void DoRunTransition(Transition transition, State from)
    {
        if (from.Active)
        {
            transition.EmitSignal(Transition.SignalName.Taken);
            from.HandleTransition(transition, from);
        }
        else
            GD.PushWarning($"Ignoring request to transition from inactive state {from.Name} to state {transition.To.Name}. This could be caused by multiple state changes in a single frame.");
    }

    private State _root = null;
    private readonly Dictionary<StringName, Variant> _properties = [];
    private readonly ConcurrentQueue<StringName> _eventQ = new();
    private readonly ConcurrentQueue<(Transition, State)> _transitionQ = new();
    private bool _transitionProcessingActive = false;
    private bool _propertyChangePending = false;
    private bool _busy = false;

    private void EnsureReady()
    {
        if (!IsNodeReady())
            throw new Exception($"State chart {Name} is not ready. Send events or set properties once it's finished initializing.");
        if (!IsInstanceValid(_root))
            throw new Exception($"State chart {Name} has no root state.");
    }

    private void RunChanges()
    {
        if (!_busy)
        {
            _busy = true;
            while (!_eventQ.IsEmpty || _propertyChangePending)
            {
                if (_propertyChangePending)
                {
                    _propertyChangePending = false;
                    _root.ProcessTransitions("", true);
                }
                if (_eventQ.TryDequeue(out StringName @event))
                {
                    EmitSignal(SignalName.EventReceived, @event);
                    _root.ProcessTransitions(@event, false);
                }
            }
            _busy = false;
        }
    }

    /// <summary>List of events available to the state chart.</summary>
    [Export] public StringName[] Events { get; private set; } = [];

    [Export] public Godot.Collections.Dictionary<StringName, Variant> InitialExpressionProperties { get; private set; } = [];

    /// <summary>Whether or not events sent to the state chart should be validated against <see cref="Events"/>.</summary>
    [Export] public bool ValidateEvents { get; private set; } = true;

    [Export] public bool ValidateExpressionNames { get; private set; } = true;

    [Export] public bool ValidateExpressionTypes { get; private set; } = true;

    /// <summary>Send an event to the active <see cref="State"/>, which could trigger a <see cref="Transition"/>.</summary>
    /// <param name="event">Name of the event to send.</param>
    public void SendEvent(StringName @event)
    {
        if (!ValidateEvents || Events.Contains(@event))
        {
            EnsureReady();
            _eventQ.Enqueue(@event);
            RunChanges();
        }
        else
            throw new ArgumentException($"State chart {Name} does not have an event {@event}");
    }

    public void SetExpressionProperty(StringName name, Variant value)
    {
        if (ValidateExpressionNames && !_properties.ContainsKey(name))
            throw new KeyNotFoundException(name);
        else if (ValidateExpressionTypes && value.VariantType != _properties[name].VariantType)
            throw new ArgumentException($"Variant type mismatch for expression property {name}: {value.VariantType} (expected {_properties[name].VariantType})");
        else if (!_properties[name].ValueEquals(value))
        {
            EnsureReady();
            _properties[name] = value;
            _propertyChangePending = true;
            RunChanges();
        }
    }

    public void SetExpressionProperty<[MustBeVariant] T>(StringName name, T value) => SetExpressionProperty(name, Variant.From(value));

    public Variant GetExpressionProperty(StringName name) => _properties[name];

    public T GetExpressionProperty<[MustBeVariant] T>(StringName name) => GetExpressionProperty(name).As<T>();

    /// <summary>Execute a <see cref="Transition"/> from a state to its target.</summary>
    /// <param name="transition">Transition to run.</param>
    /// <param name="from">State to transition from.</param>
    public void RunTransition(Transition transition, State from)
    {
        _transitionQ.Enqueue((transition, from));
        if (!_transitionProcessingActive)
        {
            _transitionProcessingActive = true;
            while (_transitionQ.TryDequeue(out (Transition, State) next))
            {
                (Transition t, State s) = next;
                DoRunTransition(t, s);
            }
            _transitionProcessingActive = false;
        }
    }

    public IEnumerable<StringName> GetExpressionProperties() => _properties.Keys;

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (GetChildCount() != 1 || GetChild(0) is not State)
            warnings.Add("A state chart must have exactly one child node that's a state.");

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            foreach ((StringName name, Variant value) in InitialExpressionProperties)
                _properties[name] = value;

            if (GetChildCount() != 1 || GetChild(0) is not State)
                throw new Exception("A state chart must have exactly one child that's a state.");
            
            _root = GetChild<State>(0);
            _root.Initialize();
            Callable.From<bool>(_root.Enter).CallDeferred(false);
        }
    }
}