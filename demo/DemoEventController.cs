using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.UI;

namespace TbsTemplate.Demo;

/// <summary>
/// Demo implementation of an event controller.  Reacts to objective completion and sends the player to the game over screen
/// when one is completed.
/// </summary>
public partial class DemoEventController : EventController
{
    [Export(PropertyHint.File, "*.tscn")] public string GameOverScreen = null;

    public async void OnObjectiveCompleted(bool success)
    {
        await ToSignal(GetTree().CreateTimer(1), Timer.SignalName.Timeout);

        SceneManager.Singleton.Connect<DemoGameOverScene>(SceneManager.SignalName.SceneLoaded, (s) => {
            MusicController.Stop();
            s.win = success;
            QueueFree();
        }, (uint)ConnectFlags.OneShot);
        SceneManager.JumpToScene(GameOverScreen);
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            LevelEvents.Singleton.Connect(LevelEvents.SignalName.SuccessObjectiveComplete, () => OnObjectiveCompleted(true));
            LevelEvents.Singleton.Connect(LevelEvents.SignalName.FailureObjectiveComplete, () => OnObjectiveCompleted(false));
        }
    }
}