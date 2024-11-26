using Godot;
using TbsTemplate.Scenes;

public partial class TestGameOver : Node
{
    [Export] public bool win = true;

    [Export(PropertyHint.File, "*.tscn")] public string RestartTarget = null;

    public void RestartGame() => SceneManager.ChangeScene(RestartTarget);

    public void EndGame() => GetTree().Quit();

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
            GetNode<Label>("CanvasLayer/ResultLabel").Text = win ? "You win!" : "You lose...";
    }
}