using Godot;

namespace UI;

/// <summary>
/// "Brain" controlling the camera. Given a target, it will follow it and smoothly move the camera to continue tracking it. It has two zones: a "dead zone,"
/// which defines the area where the target can be and the camera won't move, and a "soft zone," which defines an area outside the dead zone where the camera
/// will move smoothly to put the target back into the dead zone. If the target is outside that, the camera will jump to get the target into the soft zone.
/// The brain can also control zooming.
/// </summary>
[Icon("res://icons/Camera2DBrain.svg"), Tool]
public partial class Camera2DBrain : Node2D
{
    /// <summary>Signal that the camera has reached its target and stopped moving.</summary>
    [Signal] public delegate void ReachedTargetEventHandler();

    /// <param name="position">Position relative to the center of the screen to focus on.</param>
    /// <param name="deadzone">Rectangle defining the dead zone.</param>
    /// <param name="limits">Limits where the center of the camera can be.</param>
    /// <returns>The position the center of the camera should move to.</returns>
    private static Vector2 GetMoveTargetPosition(Vector2 position, Rect2 deadzone, Rect2 limits)
    {
        if (deadzone.HasPoint(position))
            return position;
        else
        {
            Vector2 displacement = Vector2.Zero;
            if (deadzone.Position.X > position.X)
                displacement = displacement with { X = position.X - deadzone.Position.X };
            else if (deadzone.End.X < position.X)
                displacement = displacement with { X = position.X - deadzone.End.X };
            if (deadzone.Position.Y > position.Y)
                displacement = displacement with { Y = position.Y - deadzone.Position.Y };
            else if (deadzone.End.Y < position.Y)
                displacement = displacement with { Y = position.Y - deadzone.End.Y };
            return (deadzone with { Position = deadzone.Position + displacement }).GetCenter().Clamp(limits.Position, limits.End);
        }
    }

    /// <summary>Clamp a zoom vector to ensure the camera doesn't zoom out too far to be able to see outside its limits.</summary>
    /// <param name="zoom">Zoom vector to clamp.</param>
    /// <returns>The zoom vector with its components clamped to ensure the viewport rect is inside the camera limits.</returns>
    private Vector2 ClampZoom(Vector2 zoom)
    {
        Vector2 mins = GetScreenRect().Size/Limits.Size;
        float min = Mathf.Min(mins.X, mins.Y);
        return zoom.Clamp(new(Mathf.Max(min, ZoomMin.X), Mathf.Max(min, ZoomMin.Y)), ZoomMax);
    }

    /// Compute the world projection of the zone with the given margins.
    /// <param name="left">Fraction of the distance from the center of the screen to the left edge of the screen.</param>
    /// <param name="top">Fraction of the distance from the center of the screen to the top edge of the screen.</param>
    /// <param name="right">Fraction of the distance from the center of the screen to the right edge of the screen.</param>
    /// <param name="bottom">Fraction of the distance from the center of the screen to the bottom edge of the screen.</param>
    /// <returns>The rectangle defining the zone in the world (rather than on screen).</returns>
    private Rect2 GetZone(float left, float top, float right, float bottom)
    {
        Rect2 viewport = GetScreenRect();
        Vector2 start = new(left, top), end = new(right, bottom);
        Rect2 viewportZone = new Rect2() with { Position = viewport.Position + viewport.Size*(Vector2.One - start)/2, End = viewport.Position + viewport.Size - viewport.Size*(Vector2.One - end)/2 };
        Rect2 localZone = Camera.GetCanvasTransform().AffineInverse()*viewportZone;
        return localZone;
    }

    /// <returns>The rectangle bounding the position of the center ("target") of the camera.</returns>
    private Rect2 GetTargetBounds()
    {
        Rect2 projected = GetProjectedViewportRect();
        return (Rect2)Limits with { Position = Limits.Position + projected.Size/2 - Position, End = Limits.End - projected.Size/2 - Position };
    }

    /// <returns>The bounds of the deadzone and the soft zone, relative to the camera center</returns>
    private Rect2 ComputeDeadZone()
    {
        Rect2 localDeadzone = GetZone(DeadZoneLeft, DeadZoneTop, DeadZoneRight, DeadZoneBottom);
        localDeadzone.Position -= Position;

        return localDeadzone;
    }

    private Camera2D _camera = null;
    private Camera2D Camera => _camera ??= GetNodeOrNull<Camera2D>("Camera2D");

    private Vector2 _targetPreviousPosition = Vector2.Zero;
    private Tween _moveTween = null;

    private Vector2 _zoom = Vector2.One;
    private Vector2 _zoomTarget = Vector2.Zero;
    private Tween _zoomTween = null;
    private Rect2I _limits = new(-1000000, -1000000, 2000000, 2000000);

    /// <summary>Object the camera is tracking. Can be null to not track anything.</summary>
    [Export] public Node2D Target = null;

    /// <summary>Camera zoom. Ratio of world pixel size to real pixel size (so a zoom of 2 presents everything in double size).</summary>
    [ExportGroup("Zoom")]
    [Export] public Vector2 Zoom
    {
        get => _zoom;
        set
        {
            _zoomTarget = _zoom = Engine.IsEditorHint() || !Camera.IsInsideTree() ? value : ClampZoom(value);
            if (Camera != null)
                Camera.Zoom = _zoom;
        }
    }

    [ExportGroup("Zoom", "Zoom")]

    /// <summary>Minimum allowable zoom (largest view of the world).</summary>
    [Export] public Vector2 ZoomMin = new(0.5f, 0.5f);

    /// <summary>Maximum allowable zoom (smallest view of the world).</summary>
    [Export] public Vector2 ZoomMax = new(3, 3);

    /// <summary>Amount of time to take to reach the next zoom setting.</summary>
    [Export] public double ZoomDuration = 0.2;

    /// <summary>Box bounding the area in the world that the camera is allowed to see.</summary>
    [ExportGroup("Camera")]
    [Export(PropertyHint.None, "suffix:px")] public Rect2I Limits
    {
        get => _limits;
        set
        {
            _limits = value;
            if (Camera != null)
            {
                (Camera.LimitLeft, Camera.LimitTop) = value.Position;
                (Camera.LimitRight, Camera.LimitBottom) = value.End;
            }
        }
    }

    [ExportGroup("Dead Zone", "DeadZone")]

    /// <summary>Left margin of the dead zone, as a fraction of the distance from the center of the screen to the edge.</summary>
    [Export(PropertyHint.Range, "0, 1")] public float DeadZoneLeft = 0.5f;

    /// <summary>Top margin of the dead zone, as a fraction of the distance from the center of the screen to the edge.</summary>
    [Export(PropertyHint.Range, "0, 1")] public float DeadZoneTop = 0.5f;

    /// <summary>Right margin of the dead zone, as a fraction of the distance from the center of the screen to the edge.</summary>
    [Export(PropertyHint.Range, "0, 1")] public float DeadZoneRight = 0.5f;

    /// <summary>Bottom margin of the dead zone, as a fraction of the distance from the center of the screen to the edge.</summary>
    [Export(PropertyHint.Range, "0, 1")] public float DeadZoneBottom = 0.5f;

    /// <summary>Time in seconds the camera should move to get the target to re-enter the dead zone from within the soft zone.</summary>
    [Export(PropertyHint.None, "suffix:s")] public double DeadZoneSmoothTime = 0.25;

    /// <summary>Draw the camera zones for help with debugging and visualization.</summary>
    [ExportGroup("Editor")]
    [Export] public bool DrawZones = false;

    /// <summary>Draw camera limits for help with debugging and visualization.</summary>
    [ExportGroup("Editor")]
    [Export] public bool DrawLimits = false;

    /// <summary>Draw camera target points for help with debugging and visualization. Does not draw in the editor.</summary>
    [ExportGroup("Editor")]
    [Export] public bool DrawTargets = false;

    public Vector2 ZoomTarget
    {
        get => _zoomTarget;
        set
        {
            if (Engine.IsEditorHint())
                _zoomTarget = value;
            else
            {
                _zoomTarget = ClampZoom(value);

                if (_zoomTween.IsValid())
                    _zoomTween.Kill();
                _zoomTween = CreateTween();
                _zoomTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out).TweenMethod(Callable.From((Vector2 zoom) => Camera.Zoom = _zoom = zoom), Zoom, _zoomTarget, ZoomDuration);
            }
        }
    }

    public Rect2 GetScreenRect()
    {
        if (Engine.IsEditorHint())
            return new(Vector2.Zero, new Vector2(ProjectSettings.GetSetting("display/window/size/viewport_width").As<int>(), ProjectSettings.GetSetting("display/window/size/viewport_height").As<int>())/Zoom);
        else
            return Camera.GetViewportRect();
    }

    /// <returns>The viewport rectangle projected onto the world.</returns>
    public Rect2 GetProjectedViewportRect() => Camera.GetCanvasTransform().AffineInverse()*GetScreenRect();

    public override void _Ready()
    {
        base._Ready();

        if (Target != null)
            _targetPreviousPosition = Target.Position;
        _moveTween = CreateTween();
        _moveTween.Kill();

        _zoom = ClampZoom(_zoom);
        Camera.Zoom = _zoom;
        (Camera.LimitLeft, Camera.LimitTop) = _limits.Position;
        (Camera.LimitRight, Camera.LimitBottom) = _limits.End;
        _zoomTarget = _zoom;
        _zoomTween = CreateTween();
        _zoomTween.Kill();

        Position = Camera.GetScreenCenterPosition();
    }

    public override void _Draw()
    {
        if (DrawZones && !Engine.IsEditorHint())
            DrawCircle(Vector2.Zero, 7, Colors.Black);
        
        Rect2 bounds = GetTargetBounds();
        if (DrawLimits)
            DrawRect(bounds, Colors.Black, filled:false);

        Rect2 localDeadzone = ComputeDeadZone();
        if (DrawZones)
        {
            void FillInterior(Rect2 inner, Rect2 outer, Color color)
            {
                Rect2 deadLeft = new Rect2(new(outer.Position.X, inner.Position.Y), Vector2.Zero) with { End = new(inner.Position.X, inner.End.Y) };
                Rect2 deadTop = new Rect2(outer.Position, Vector2.Zero) with { End = new(outer.End.X, inner.Position.Y) };
                Rect2 deadRight = new Rect2(new(inner.End.X, inner.Position.Y), Vector2.Zero) with { End = new(outer.End.X, inner.End.Y ) };
                Rect2 deadBottom = new Rect2(new(outer.Position.X, inner.End.Y), Vector2.Zero) with { End = new(outer.End.X, outer.End.Y) };
                DrawRect(deadLeft, color, filled:true);
                DrawRect(deadTop, color, filled:true);
                DrawRect(deadRight, color, filled:true);
                DrawRect(deadBottom, color, filled:true);
            }

            Rect2 localLimits = Engine.IsEditorHint() ? GetProjectedViewportRect() : (Rect2)Limits;
            localLimits.Position -= Position;

            DrawLine(new Vector2(localLimits.Position.X, localDeadzone.Position.Y), new Vector2(localLimits.End.X, localDeadzone.Position.Y), Colors.Purple);
            DrawLine(new Vector2(localDeadzone.Position.X, localLimits.Position.Y), new Vector2(localDeadzone.Position.X, localLimits.End.Y), Colors.Purple);
            DrawLine(new Vector2(localLimits.Position.X, localDeadzone.End.Y), new Vector2(localLimits.End.X, localDeadzone.End.Y), Colors.Purple);
            DrawLine(new Vector2(localDeadzone.End.X, localLimits.Position.Y), new Vector2(localDeadzone.End.X, localLimits.End.Y), Colors.Purple);

            FillInterior(localDeadzone, localLimits,  new(Colors.Purple.R, Colors.Purple.G, Colors.Purple.B, 0.25f));
        }

        if (!Engine.IsEditorHint() && DrawTargets && Target != null)
        {
            Vector2 targetRelative = Target.Position - Position;
            if (!localDeadzone.HasPoint(targetRelative))
            {
                DrawCircle(targetRelative, 5, Colors.Purple);
                DrawCircle(targetRelative.Clamp(localDeadzone.Position, localDeadzone.End), 3, Colors.Purple.Darkened(0.5f));
                DrawCircle(GetMoveTargetPosition(targetRelative, localDeadzone, bounds), 3, Colors.Blue);
            }
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            if (Target is not null)
            {
                if (!_moveTween.IsValid() || _targetPreviousPosition != Target.Position)
                {
                    if (_moveTween.IsValid())
                        _moveTween.Kill();

                    Rect2 bounds = GetTargetBounds();
                    Rect2 localDeadzone = ComputeDeadZone();
                    Vector2 targetRelative = Target.Position - Position;
                    if (!localDeadzone.HasPoint(targetRelative))
                    {
                        _moveTween = CreateTween();
                        _moveTween
                            .SetTrans(Tween.TransitionType.Cubic)
                            .SetEase(Tween.EaseType.Out)
                            .TweenProperty(this, PropertyName.Position.ToString(), Position + GetMoveTargetPosition(targetRelative, localDeadzone, bounds), DeadZoneSmoothTime);
                        _moveTween.Finished += () => EmitSignal(SignalName.ReachedTarget);
                    }
                }

                _targetPreviousPosition = Target.Position;
            }
        }
        if (Engine.IsEditorHint() || DrawZones || DrawLimits || DrawTargets)
            QueueRedraw();
    }
}