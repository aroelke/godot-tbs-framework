using System.Threading.Tasks;
using Godot;

namespace Scenes.Transitions;

/// <summary>A transition animation between scenes to play when switching.</summary>
[GlobalClass, Tool]
public partial class SceneTransition : Control
{
    /// <summary>Signals that the transition (in or out) is finished.</summary>
    [Signal] public delegate void TransitionFinishedEventHandler();

    /// <summary>Play the part of the transition animation that leaves the current scene (goes into the next scene).</summary>
    public virtual Task TransitionIn()
    {
        EmitSignal(SignalName.TransitionFinished);
        return Task.CompletedTask;
    }

    /// <summary>Play the part of the transition animation that enters the next scene (goes out of the current scene).</summary>
    public virtual Task TransitionOut()
    {
        EmitSignal(SignalName.TransitionFinished);
        return Task.CompletedTask;
    }

    public override void _Ready()
    {
        base._Ready();
        PropagateCall(MethodName.SetMouseFilter, new() { Variant.From(MouseFilter) });
    }
}