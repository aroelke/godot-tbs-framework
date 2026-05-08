using System.Linq;
using Godot;
using TbsFramework.UI.Controls.Device;

public partial class DemoPauseOverlay : Control
{
    [Signal] public delegate void GamePausedEventHandler();

    [Signal] public delegate void GameResumedEventHandler();

    private int _selected = 0;
    private Button[] _buttons = null;

    public void Pause()
    {
        GetTree().Paused = true;
        Visible = true;
        _buttons[_selected = 0].GrabFocus();
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

    public void OnDirectionPressed(Vector2I direction) => _buttons[_selected = (_selected + direction.Y) % _buttons.Length].GrabFocus();

    public override void _Ready()
    {
        base._Ready();
        _buttons = [.. GetNode("Buttons").GetChildren().OfType<Button>()];
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