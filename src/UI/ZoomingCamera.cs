using Godot;

namespace UI;

public partial class ZoomingCamera : Camera2D
{
    private Vector2 _target = new(0, 0);
    private Tween _tween = null;

    [Export] public Vector2 MinZoom = new(0.5f, 0.5f);

    [Export] public Vector2 MaxZoom = new(3, 3);

    [Export] public Vector2 ZoomFactor = new(0.25f, 0.25f);

    [Export] public double ZoomDuration = 0.2;

    public Vector2 ZoomTarget
    {
        get => _target;
        set
        {
            Vector2 mins = GetViewportRect().Size/new Vector2(LimitRight - LimitLeft, LimitBottom - LimitTop);
            float min = Mathf.Min(mins.X, mins.Y);

            _target = value.Clamp(new(Mathf.Max(min, MinZoom.X), Mathf.Max(min, MinZoom.Y)), MaxZoom);

            if (_tween != null && _tween.IsValid())
                _tween.Kill();
            _tween = CreateTween();
            _tween.TweenProperty(this, PropertyName.Zoom.ToString(), _target, ZoomDuration);
            _tween.SetTrans(Tween.TransitionType.Sine);
            _tween.SetEase(Tween.EaseType.Out);
        }
    }

    public override void _Ready()
    {
        base._Ready();
        _target = Zoom;
    }
}