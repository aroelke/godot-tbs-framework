using Godot;
using TbsFramework.Scenes;
using TbsFramework.UI.Controls.Device;

namespace TbsFramework.Demo;

/// <summary>Simple demo game over screen that notifies the player whether they won or lost.</summary>
public partial class DemoGameOverScene : Node
{
    private static readonly StringName RestartItem = "Restart";
    private static readonly StringName QuitItem = "Quit";

    [Export] public bool win = true;

    [Export(PropertyHint.File, "*.tscn")] public string RestartTarget = null;

    public void OnItemSelected(StringName item)
    {
        if (item == RestartItem)
            SceneManager.CallScene(RestartTarget);
        else if (item == QuitItem)
            GetTree().Quit();
    }

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
        {
            DeviceManager.EnableSystemMouse = true;
            GetNode<Label>("CanvasLayer/ResultLabel").Text = win ? "You win!" : "You lose...";
        }
    }
}