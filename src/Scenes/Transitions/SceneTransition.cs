using System.Threading.Tasks;
using Godot;

namespace Scenes.Transitions;

[GlobalClass, Tool]
public partial class SceneTransition : Control
{
    [Signal] public delegate void TransitionFinishedEventHandler();

    public virtual Task TransitionIn()
    {
        EmitSignal(SignalName.TransitionFinished);
        return Task.CompletedTask;
    }

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