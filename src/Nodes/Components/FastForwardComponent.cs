using Godot;
using UI.Controls.Action;

namespace Nodes;

/// <summary>Node component implementing a "fast-forward" function for the node.</summary>
public partial class FastForwardComponent : Node
{
    /// <summary>Signals that fast-forwarding has begun.</summary>
    [Signal] public delegate void AccelerateEventHandler();

    /// <summary>Signals that fast-forwarding has ended.</summary>
    [Signal] public delegate void DecelerateEventHandler();

    /// <summary>Input action to toggle fast-forwarding.</summary>
    [Export] public InputActionReference Action = null;

    /// <summary>Whether or not fast-forwarding is active.</summary>
    public bool Active { get; private set; } = false;

    public override void _EnterTree()
    {
        base._EnterTree();
        if (Input.IsActionPressed(Action))
        {
            Active = true;
            EmitSignal(SignalName.Accelerate);
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event.IsActionPressed(Action))
        {
            Active = true;
            EmitSignal(SignalName.Accelerate);
        }
        else if (@event.IsActionReleased(Action))
        {
            Active = false;
            EmitSignal(SignalName.Decelerate);
        }
    }
}