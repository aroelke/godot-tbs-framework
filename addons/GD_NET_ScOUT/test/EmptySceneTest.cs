using Godot;

namespace GD_NET_ScOUT;

public partial class EmptySceneTest : RichTextLabel
{
    private bool _waiting;
    private double _timeout;

    public override void _Ready()
    {
        _waiting = true;
        _timeout = 1.0 + SceneScout.MaxTestRunnerMissingWaitSeconds;
    }

    public override void _Process(double delta)
    {
        if (!_waiting)
        {
            return;
        }

        if (!((_timeout -= delta) <= 0.0))
        {
            Text =
                $"[font_size=24][center]This scene will create an automatically failing test in {_timeout:0.##} seconds.[/center][/font_size]";
            return;
        }

        _waiting = false;
        Text = "";
        AddChild(GD.Load<PackedScene>(AutoFailTest.ScenePath).Instantiate());
    }
}
