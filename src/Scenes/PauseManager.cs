using Godot;
using UI.Controls.Action;

namespace Scenes;

/// <summary>Scene component that controls pausing.</summary>
public partial class PauseManager : Node
{
    /// <summary>Signals that the pause state has changed.</summary>
    [Signal] public delegate void PauseStateChangedEventHandler(bool paused);

    /// <summary>Whether or not to use the built-in pause feature.</summary>
    [Export] public bool UseEnginePause = true;

    /// <summary>Current pause state of the scene.</summary>
    public bool Paused { get; private set; } = false;

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionPressed(InputActions.Pause))
        {
            Paused = !Paused;
            if (UseEnginePause)
                GetTree().Paused = Paused;
            EmitSignal(SignalName.PauseStateChanged, Paused);
        }
    }
}