using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TbsTemplate.Nodes.StateCharts.States;

/// <summary>Base class implementing common behavior of all <see cref="StateCharts"/> states.</summary>
[GlobalClass, Tool]
public abstract partial class State : ChartNode
{
    /// <summary>Signals that the state is being entered.</summary>
    [Signal] public delegate void StateEnteredEventHandler();

    /// <summary>Signals that an event has been received while the state is active.</summary>
    /// <param name="event">Name of the event.</param>
    [Signal] public delegate void EventReceivedEventHandler(StringName @event);

    /// <summary>Signals that the state is being exited.</summary>
    [Signal] public delegate void StateExitedEventHandler();

    private static StateChart FindChart(Node node) => node == null ? null : node as StateChart ?? FindChart(node.GetParent());

    private bool _active = false;
    private readonly List<Transition> _transitions = [];

    /// <summary>Whether or not the state is active. Setting the state to inactive also disables processing (preventing signal emission).</summary>
    public bool Active
    {
        get => _active;
        private set
        {
            _active = value;
            ProcessMode = _active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        }
    }

    /// <summary>Initialize the state while building the chart.</summary>
    public virtual void Initialize()
    {
        Active = false;

        _transitions.Clear();
        _transitions.AddRange(GetChildren().OfType<Transition>());
    }

    /// <summary>Activate the state.</summary>
    /// <param name="transit">Whether or not to immediately handle a transition, to avoid activating a default state if there is one.</param>
    public virtual void Enter(bool transit=false)
    {
        Active = true;

        EmitSignal(SignalName.StateEntered);
        foreach (Transition transition in _transitions)
            if (transition.Automatic && transition.EvaluateCondition())
                StateChart.RunTransition(transition, this);
    }

    /// <summary>Process all transitions and run the first one that is triggered by the event.</summary>
    /// <param name="event">Event that triggered the transition.</param>
    /// <param name="property">This transition was triggered by a property change.</param>
    /// <returns><c>true</c> if a transition ran, and <c>false</c> otherwise.</returns>
    public virtual bool ProcessTransitions(StringName @event, bool property=false)
    {
        if (!Active)
            return false;
        
        if (!property)
            EmitSignal(SignalName.EventReceived, @event);
        
        foreach (Transition transition in _transitions)
        {
            if ((transition.Automatic || (!property && transition.Event == @event)) && transition.EvaluateCondition())
            {
                StateChart.RunTransition(transition, this);
                return true;
            }
        }
        return false;
    }

    public abstract void HandleTransition(Transition transition, State from);

    /// <summary>Deactivate the state.</summary>
    public virtual void Exit()
    {
        Active = false;
        EmitSignal(SignalName.StateExited);
    }

    public virtual StateRecord SaveHistory()
    {
        if (!Active)
            throw new InvalidOperationException($"Failed to save inactive state {Name}");
        else
            return new() { Active = GetChildren().OfType<State>().Where(static (s) => s.Active).ToDictionary(static (s) => s, static (s) => s.SaveHistory()) };
    }

    public virtual void RestoreHistory(StateRecord record)
    {
        if (!Active)
            Enter();
        foreach (State state in GetChildren().OfType<State>())
        {
            if (record.Active.TryGetValue(state, out StateRecord r))
                state.RestoreHistory(r);
            else if (state.Active)
                state.Exit();
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (FindChart(GetParent()) is null)
            warnings.Add("A state needs to have a StateChart ancestor.");

        return [.. warnings];
    }
}