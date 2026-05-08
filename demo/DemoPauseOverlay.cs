using Godot;
using TbsFramework.UI.Controls.Device;

public partial class DemoPauseOverlay : Control
{
    [Signal] public delegate void GamePausedEventHandler();

    [Signal] public delegate void GameResumedEventHandler();

    public void Pause()
    {
        GetTree().Paused = true;
        Visible = true;
        EmitSignal(SignalName.GamePaused);
    }

    public void OnQuitGamePressed() => GetTree().Quit();

    public void OnRestartGamePressed() => GetTree().ReloadCurrentScene();

    public void Resume()
    {
        Visible = false;
        GetTree().Paused = false;
        EmitSignal(SignalName.GameResumed);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event.IsActionPressed(InputManager.Pause))
        {
            Resume();
            GetViewport().SetInputAsHandled();
        }
    }
}