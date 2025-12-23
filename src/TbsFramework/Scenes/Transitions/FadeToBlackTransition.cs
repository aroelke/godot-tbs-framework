using Godot;

namespace TbsFramework.Scenes.Transitions;

/// <summary>Simple scene transition that fades to a color and then back into the next scene.</summary>
public partial class FadeToBlackTransition : SceneTransition
{
    private Color _color = Colors.Black;
    private Tween _tween = null;
    private ColorRect _overlay = null;

    private ColorRect Overlay => _overlay ??= GetNode<ColorRect>("Overlay");

    /// <summary>Color to fade to. Does not have to be black, despite the class name.</summary>
    /// <remarks>Make sure the color isn't fully transparent, or no fading will happen!</remarks>
    [Export] public Color Color
    {
        get => _color;
        set
        {
            if (_color != value)
            {
                _color = value;
                if (Overlay is not null)
                    Overlay.Modulate = _color with { A = Overlay.Modulate.A };
            }
        }
    }

    private void Transition(float target, StringName signal)
    {
        Active = true;
        if (_tween.IsValid())
            _tween.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(Overlay, $"{PropertyName.Modulate}:a", target, TransitionTime/2).Finished += () => {
            Active = false;
            EmitSignal(signal);
        };
    }

    public override void TransitionOut() => Transition(1, SignalName.TransitionedOut);
    public override void TransitionIn() => Transition(0, SignalName.TransitionedIn);

    public override void _Ready()
    {
        base._Ready();
        _tween = CreateTween();
        _tween.Kill();
        Overlay.Modulate = _color with { A = 0 };
        Visible = true; // Assume this was made invisible in editor so the scene it's covering is visible
    }
}