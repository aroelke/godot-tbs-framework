using Godot;
using TbsTemplate.Scenes;

namespace TbsTemplate.Demo;

/// <summary>Simple demo game over screen that notifies the player whether they won or lost.</summary>
public partial class DemoGameOverScene : Node
{
    [Export] public bool win = true;

    [Export(PropertyHint.File, "*.tscn")] public string RestartTarget = null;

    public void RestartGame() => SceneManager.CallScene(RestartTarget);

    public void EndGame() => GetTree().Quit();

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
            GetNode<Label>("CanvasLayer/ResultLabel").Text = win ? "You win!" : "You lose...";
    }
}