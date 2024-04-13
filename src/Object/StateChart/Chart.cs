using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Object.StateChart.States;

namespace Object.StateChart;

/// <summary>
/// A state chart modeled after <see href="https://github.com/derkork/godot-statecharts"/>. Contains a hierarchy of states that have zero or more
/// transitions to other states within the hierarchy. While a state is active, it can receive events, which cause it to trigger transitions to other
/// states or emit signals.
/// </summary>
[Icon("res://icons/statechart/Chart.svg"), Tool]
public partial class Chart : Node
{
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
    private ImmutableDictionary<StringName, Variant> _properties = ImmutableDictionary<StringName, Variant>.Empty;
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
            while (_eventQ.Any() || _propertyChangePending)
            {
                if (_propertyChangePending)
                {
                    _propertyChangePending = false;
                    _root.ProcessTransitions("", true);
                }
                if (_eventQ.TryDequeue(out StringName @event))
                    _root.ProcessTransitions(@event, false);
            }
            _busy = false;
        }
    }

    /// <summary>
    /// Dictionary of state chart properties and their values. Setting can cause a transition if the update causes a condition of
    /// a transition from the active state to become true.
    /// </summary>
    public ImmutableDictionary<StringName, Variant> ExpressionProperties
    {
        get => _properties;
        set
        {
            if (_properties != value)
            {
                EnsureReady();

                _properties = value;
                _propertyChangePending = true;
                RunChanges();
            }
        }
    }

    /// <summary>Send an event to the active state, which could trigger a transition or action.</summary>
    /// <param name="event">Name of the event to send.</param>
    public void SendEvent(StringName @event)
    {
        EnsureReady();

        _eventQ.Enqueue(@event);
        RunChanges();
    }

    /// <summary>Execute a transition from a state to its target.</summary>
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
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        if (GetChildCount() != 1 || GetChild(0) is not State)
            warnings.Add("A state chart must have exactly one child node that's a state.");

        return warnings.ToArray();
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            if (GetChildCount() != 1 || GetChild(0) is not State)
                throw new Exception("A state chart must have exactly one child that's a state.");
            
            _root = GetChild<State>(0);
            _root.Initialize();
            Callable.From(() => _root.Enter()).CallDeferred();
        }
    }
}