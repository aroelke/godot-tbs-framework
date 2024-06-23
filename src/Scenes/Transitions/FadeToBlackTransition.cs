using Godot;
using Nodes;

namespace Scenes.Transitions;

/// <summary>Simple scene transition that fades to a color and then back into the next scene.</summary>
public partial class FadeToBlackTransition : SceneTransition
{
    private readonly NodeCache _cache;
    public FadeToBlackTransition() : base() => _cache = new(this);

    private Color _color = Colors.Black;
    private Tween _tween = null;

    private ColorRect Overlay => _cache.GetNodeOrNull<ColorRect>("Overlay");

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

    public override void TransitionOut()
    {
        if (_tween.IsValid())
            _tween.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(Overlay, $"{ColorRect.PropertyName.Modulate}:a", 1, TransitionTime/2).Finished += () => EmitSignal(SignalName.TransitionedOut);
    }

    public override void TransitionIn()
    {
        if (_tween.IsValid())
            _tween.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(Overlay, $"{ColorRect.PropertyName.Modulate}:a", 0, TransitionTime/2).Finished += () => EmitSignal(SignalName.TransitionedIn);
    }

    public override void _Ready()
    {
        base._Ready();
        _tween = CreateTween();
        _tween.Kill();
        Overlay.Modulate = _color with { A = 0 };
    }
}