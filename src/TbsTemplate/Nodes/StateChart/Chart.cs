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
    private readonly Dictionary<StringName, Variant> _variables = [];
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

    private void Update()
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

    /// <summary>Initial values of variables. Also used to set the type of each variable.</summary>
    [Export] public Godot.Collections.Dictionary<StringName, Variant> InitialVariableValues { get; private set; } = [];

    /// <summary>Whether or not events sent to the state chart should be validated against <see cref="Events"/>.</summary>
    [Export] public bool ValidateEvents { get; private set; } = true;

    /// <summary>
    /// Whether or not to validate variable names when setting values. If true, <see cref="SetVariable"/> will throw an exception if a name is
    /// used that's not in <see cref="InitialVariableValues"/>. If false, calling <see cref="SetVariable"/> with a new name will create a new
    /// variable.
    /// </summary>
    [Export] public bool ValidateVariableNames { get; private set; } = true;

    /// <summary>
    /// Whether or not to validate variable types when setting values. If true, <see cref="SetVariable"/> will throw an exception if the value
    /// doesn't match the type of the value of the variable.
    /// </summary>
    [Export] public bool ValidateVariableTypes { get; private set; } = true;

    /// <summary>Send an event to the active <see cref="State"/>, which could trigger a <see cref="Transition"/>.</summary>
    /// <param name="event">Name of the event to send.</param>
    public void SendEvent(StringName @event)
    {
        if (!ValidateEvents || Events.Contains(@event))
        {
            EnsureReady();
            _eventQ.Enqueue(@event);
            Update();
        }
        else
            throw new ArgumentException($"State chart {Name} does not have an event {@event}");
    }

    /// <summary>Set the value of a variable. Transitions and reactions are only performed if the new value is different than the old one.</summary>
    /// <param name="name">Name of the variable to set.</param>
    /// <param name="value">New value of the variable.</param>
    /// <exception cref="KeyNotFoundException">If <see cref="ValidateVariableNames"/> is checked and <paramref name="name"/> doesn't match an existing variable name.</exception>
    /// <exception cref="ArgumentException">If <see cref="ValidateVariableTypes"/> is checked and <paramref name="value"/> is the wrong type for the variable.</exception>
    public void SetVariable(StringName name, Variant value)
    {
        if (ValidateVariableNames && !_variables.ContainsKey(name))
            throw new KeyNotFoundException(name);
        else if (ValidateVariableTypes && value.VariantType != _variables[name].VariantType)
            throw new ArgumentException($"Variant type mismatch for expression property {name}: {value.VariantType} (expected {_variables[name].VariantType})");
        else if (!_variables.TryGetValue(name, out Variant current) || !value.ValueEquals(current))
        {
            EnsureReady();
            _variables[name] = value;
            _propertyChangePending = true;
            Update();
        }
    }

    /// <inheritdoc cref="SetVariable"/>
    /// <typeparam name="T">Type of the variable being set.</typeparam>
    public void SetVariable<[MustBeVariant] T>(StringName name, T value) => SetVariable(name, Variant.From(value));

    /// <summary>Get the value of a variable.</summary>
    /// <param name="name">Name of the variable to get.</param>
    /// <returns>The value of the variable, if such a variable is defined.</returns>
    /// <exception cref="KeyNotFoundException">If <paramref name="name"/> doesn't match an existing variable name.</exception>
    public Variant GetVariable(StringName name) => _variables[name];

    /// <inheritdoc cref="GetVariable"/>
    /// <typeparam name="T">Type of the variable.</typeparam>
    public T GetVariable<[MustBeVariant] T>(StringName name) => GetVariable(name).As<T>();

    /// <returns>The list of variable names in arbitrary order.</returns>
    public IEnumerable<StringName> GetVariables() => _variables.Keys;

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
            foreach ((StringName name, Variant value) in InitialVariableValues)
                _variables[name] = value;

            if (GetChildCount() != 1 || GetChild(0) is not State)
                throw new Exception("A state chart must have exactly one child that's a state.");
            
            _root = GetChild<State>(0);
            _root.Initialize();
            Callable.From<bool>(_root.Enter).CallDeferred(false);
        }
    }
}