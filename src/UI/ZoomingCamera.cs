using Godot;

namespace UI;

/// <summary><c>Camera2D</c> that can be smoothly zoomed. Also stops zooming once the viewport hits camera limits, even if it's not at min.</summary>
public partial class ZoomingCamera : Camera2D
{
    private Vector2 _target = new(0, 0);
    private Tween _tween = null;

    /// <summary>Minimum allowable zoom (largest view of the world).</summary>
    [Export] public Vector2 MinZoom = new(0.5f, 0.5f);

    /// <summary>Maximum allowable zoom (smallest view of the world).</summary>
    [Export] public Vector2 MaxZoom = new(3, 3);

    /// <summary>Amount of time to take to reach the next zoom setting.</summary>
    [Export] public double ZoomDuration = 0.2;

    private Vector2 Clamp(Vector2 zoom)
    {
        Vector2 mins = GetViewportRect().Size/new Vector2(LimitRight - LimitLeft, LimitBottom - LimitTop);
        float min = Mathf.Min(mins.X, mins.Y);
        return zoom.Clamp(new(Mathf.Max(min, MinZoom.X), Mathf.Max(min, MinZoom.Y)), MaxZoom);
    }

    /// <summary>The current zoom setting. When set, the camera will take <c>ZoomDuration</c> seconds to reach that setting.</summary>
    public Vector2 ZoomTarget
    {
        get => _target;
        set
        {
            _target = Clamp(value);

            if (_tween != null && _tween.IsValid())
                _tween.Kill();
            _tween = CreateTween();
            _tween.TweenMethod(Callable.From((Vector2 zoom) => base.Zoom = zoom), base.Zoom, _target, ZoomDuration);
            _tween.SetTrans(Tween.TransitionType.Sine);
            _tween.SetEase(Tween.EaseType.Out);
        }
    }

    /// <summary>Directly set the zoom value without smoothing its transition.</summary>
    public new Vector2 Zoom
    {
        get => base.Zoom;
        set => _target = base.Zoom = Clamp(value);
    }

    public override void _Ready()
    {
        base._Ready();
        _target = Zoom;
    }
}