using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Demo;

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
            s.win = success;
            QueueFree();
        }, (uint)ConnectFlags.OneShot);
        SceneManager.JumpToScene(GameOverScreen);
    }

    public void OnSuccessObjectiveCompleted() => OnObjectiveCompleted(true);
    public void OnFailureObjectiveCompleted() => OnObjectiveCompleted(false);

    public override void _EnterTree()
    {
        base._EnterTree();

        if (!Engine.IsEditorHint())
        {
            LevelEvents.SuccessObjectiveCompleted += OnSuccessObjectiveCompleted;
            LevelEvents.FailureObjectiveCompleted += OnFailureObjectiveCompleted;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (!Engine.IsEditorHint())
        {
            LevelEvents.SuccessObjectiveCompleted -= OnSuccessObjectiveCompleted;
            LevelEvents.FailureObjectiveCompleted -= OnFailureObjectiveCompleted;
        }
    }
}