using Godot;

namespace TbsFramework.Scenes.Transitions;

/// <summary>A transition animation between scenes to play when switching.</summary>
[GlobalClass, Tool]
public abstract partial class SceneTransition : Control
{
    /// <summary>Signals that the transition out from the last scene is finished.</summary>
    [Signal] public delegate void TransitionedOutEventHandler();

    /// <summary>Signals that the transition into the next scene is finished.</summary>
    [Signal] public delegate void TransitionedInEventHandler();

    /// <summary>Total time to complete the transition.</summary>
    [Export] public double TransitionTime = 0;

    /// <summary>Whether or not the transition is currently running.</summary>
    public bool Active { get; protected set; } = false;

    /// <summary>Play the part of the transition animation that enters the next scene (goes out of the current scene).</summary>
    public abstract void TransitionOut();

    /// <summary>Play the part of the transition animation that leaves the current scene (goes into the next scene).</summary>
    public abstract void TransitionIn();

    public override void _Ready()
    {
        base._Ready();
        PropagateCall(MethodName.SetMouseFilter, [Variant.From(MouseFilter)]);
    }
}