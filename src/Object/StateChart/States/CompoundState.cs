using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Object.StateChart.States;

/// <summary>A <see cref="State"/> that can have one or more sub-<see cref="State"/>s.</summary>
[Icon("res://icons/statechart/CompoundState.svg"), Tool]
public partial class CompoundState : State
{
    /// <summary>Signals that one of this state's sub-states is being entered.</summary>
    [Signal] public delegate void ChildStateEnteredEventHandler();

    /// <summary>Signals that one of this state's sub-states is being exited.</summary>
    [Signal] public delegate void ChildStateExitedEventHandler();

    private State _active = null;

    /// <summary>State to go to when transitioning to this state. Transition directly to sub-states to ignore this.</summary>
    [Export] public State InitialState = null;

    // Initialize this state and its child states
    public override void Initialize()
    {
        base.Initialize();

        foreach (State state in GetChildren().OfType<State>())
        {
            state.Initialize();
            state.StateEntered += () => EmitSignal(SignalName.ChildStateEntered);
            state.StateExited += () => EmitSignal(SignalName.ChildStateExited);
        }
    }

    // If we're not expecting to immediately transition to another state, activate the initial state
    public override void Enter(bool transit=false)
    {
        base.Enter(transit);

        if (!transit && !IsInstanceValid(_active) && Active)
        {
            _active = InitialState;
            _active.Enter();
        }
    }

    public override bool ProcessTransitions(StringName @event, bool property=false)
    {
        if (!Active)
            return false;
        
        if (IsInstanceValid(_active) && _active.ProcessTransitions(@event, property))
        {
            if (!property)
                EmitSignal(SignalName.EventReceived, @event);
            return true;
        }

        return base.ProcessTransitions(@event, property);
    }

    public override void HandleTransition(Transition transition, State from)
    {
        if (transition.To == this)
        {
            Exit();
            Enter(false);
        }
        else if (GetChildren().Contains(transition.To))
        {
            if (IsInstanceValid(_active))
                _active.Exit();
            _active = transition.To;
            _active.Enter(false);
        }
        else if (IsAncestorOf(transition.To))
        {
            foreach (State state in GetChildren().OfType<State>())
            {
                if (state.IsAncestorOf(transition.To))
                {
                    if (_active != state)
                    {
                        if (IsInstanceValid(_active))
                            _active.Exit();
                        _active = state;
                        _active.Enter(true);
                    }
                    state.HandleTransition(transition, from);
                    return;
                }
            }
        }
        else
            GetParent<State>().HandleTransition(transition, from);
    }

    public override void Exit()
    {
        if (_active is not null)
        {
            _active.Exit();
            _active = null;
        }
        base.Exit();
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        int substates = GetChildren().OfType<State>().Count();
        if (substates < 1)
            warnings.Add("Compound states should have at least one child state.");
        else if (substates < 2)
            warnings.Add("Compound states with only children are not useful. Consider replacing with an AtomicState.");
        
        if (InitialState is null)
            warnings.Add("A compound state needs an initial sub state.");
        else if (InitialState.GetParent() != this)
            warnings.Add("Initial state must be a direct child of this state.");

        return warnings.ToArray();
    }
}