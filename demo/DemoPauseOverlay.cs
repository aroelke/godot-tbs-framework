using System.Linq;
using Godot;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes;
using TbsFramework.UI.Controls.Device;

public partial class DemoPauseOverlay : Control
{
    [Signal] public delegate void GamePausedEventHandler();

    [Signal] public delegate void GameResumedEventHandler();

    private int _selected = 0;
    private Button[] _buttons = null;

    private NodeCache _cache = null;
    private AudioStreamPlayer HighlightSound => _cache.GetNode<AudioStreamPlayer>("HighlightSound");

    [Export(PropertyHint.File, "*.tscn")] public string RestartTarget = null;

    public DemoPauseOverlay() : base() { _cache = new(this); }

    public void Pause()
    {
        GetTree().Paused = true;
        Visible = true;
        _buttons[_selected = 0].GrabFocus();
        EmitSignal(SignalName.GamePaused);
    }

    public void OnButtonFocusEntered()
    {
        if (DeviceManager.Mode != InputMode.Mouse)
            HighlightSound.Play();
    }

    public void OnQuitGamePressed() => GetTree().Quit();

    public void OnRestartGamePressed()
    {
        GetTree().Paused = false;
        SceneManager.CallScene(RestartTarget);
    }

    public void Resume()
    {
        Visible = false;
        GetTree().Paused = false;
        EmitSignal(SignalName.GameResumed);
    }

    public void OnDirectionPressed(Vector2I direction)
    {
        _buttons[_selected = (_selected + direction.Y) % _buttons.Length].GrabFocus();
    }

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