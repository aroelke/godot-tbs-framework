using System;
using System.Collections.Generic;
using Godot;
using TbsFramework.Demo;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes;
using TbsFramework.UI.Controls.Device;

public partial class DemoPauseOverlay : Control
{
    [Signal] public delegate void GamePausedEventHandler();

    [Signal] public delegate void GameResumedEventHandler();

    private readonly NodeCache _cache = null;
    private ContextMenu       Menu => _cache.GetNode<ContextMenu>("Menu");
    private AudioStreamPlayer SelectSound => _cache.GetNode<AudioStreamPlayer>("SelectSound");
    private readonly Dictionary<StringName, Action> _actions = null;

    [Export(PropertyHint.File, "*.tscn")] public string RestartTarget = null;

    public DemoPauseOverlay() : base()
    {
        _cache = new(this);
        _actions = new()
        {
            { "Quit Game", () => GetTree().Quit() },
            { "Restart Game", () => {
                GetTree().Paused = false;
                SceneManager.CallScene(RestartTarget);
            }},
            { "Resume", Resume }
        };
    }

    public void Pause()
    {
        GetTree().Paused = true;
        Visible = Menu.Visible = true;
        if (DeviceManager.Mode != InputMode.Mouse)
            Menu.GrabFocus();
        EmitSignal(SignalName.GamePaused);
    }

    public void OnMenuItemSelected(StringName item)
    {
        SelectSound.Play();
        _actions[item]();
    }

    public void Resume()
    {
        Visible = Menu.Visible = false;
        GetTree().Paused = false;
        EmitSignal(SignalName.GameResumed);
    }

    public override void _Ready()
    {
        base._Ready();
        Menu.Visible = Visible;
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