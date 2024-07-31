using Godot;
using System.Collections.Generic;
using System.Linq;
using System;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes;
using TbsTemplate.UI.Controls.Action;

namespace TbsTemplate.UI;

/// <summary>
/// "Brain" controlling the <see cref="Camera2D"/>. Given a target, it will follow it and smoothly move the camera to continue tracking it.
/// </summary>
[Icon("res://icons/Camera2DBrain.svg"), SceneTree, Tool]
public partial class Camera2DBrain : Node2D
{
    /// <summary>Signal that the camera has reached its target and stopped moving.</summary>
    [Signal] public delegate void ReachedTargetEventHandler();

    /// <param name="position">Position relative to the center of the <see cref="Viewport"/> to focus on.</param>
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

    /// <summary>
    /// Compute the point on the target's bounding box to move into the dead zone. If the bounding box is larger than the dead zone
    /// in either dimension, the center of the box along that dimension is used. Otherwise, the edge of the box along each dimension
    /// that's furthest away from the dead zone is used.
    /// </summary>
    /// <param name="box">Target's bounding box.</param>
    /// <param name="deadzone">Box to keep the focus point in.</param>
    /// <returns>The point on the target's bounding box to keep inside the dead zone.</returns>
    private static Vector2 GetTargetFocusPoint(Rect2 box, Rect2 deadzone)
    {
        Vector2 position = box.GetCenter();
        if (box.Size.X <= deadzone.Size.X)
        {
            if (box.Position.X < deadzone.Position.X)
                position = position with { X = box.Position.X };
            else if (box.End.X > deadzone.End.X)
                position = position with { X = box.End.X };
        }
        if (box.Size.Y <= deadzone.Size.Y)
        {
            if (box.Position.Y < deadzone.Position.Y)
                position = position with { Y = box.Position.Y };
            else if (box.End.Y > deadzone.End.Y)
                position = position with { Y = box.End.Y };
        }
        return position;
    }

    /// <summary>
    /// Compute the position to move the <see cref="Viewport"/> center to in order to keep the target point inside the dead zone.
    /// </summary>
    /// <param name="box">Target's bounding box.</param>
    /// <param name="deadzone">Box to keep the target's bounding box in.</param>
    /// <param name="limits">Box defining the limits of the camera's movement.</param>
    /// <returns>The point to move the camera's center to in order to keep the target's bounding box inside the dead zone.</returns>
    private static Vector2 GetMoveTargetPosition(Rect2 box, Rect2 deadzone, Rect2 limits)
    {
        Vector2 position = GetTargetFocusPoint(box, deadzone);
        if (deadzone.Contains(position, perimeter:true))
            return Vector2.Zero;
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

    private Rect2I _limits = new(-1000000, -1000000, 2000000, 2000000);

    private Vector2 _targetPreviousPosition = Vector2.Zero;
    private Tween _moveTween = null;

    private Vector2 _zoom = Vector2.One;
    private Vector2 _zoomTarget = Vector2.Zero;
    private Tween _zoomTween = null;
    private readonly Stack<Vector2> _savedZooms = new();

    private float _noiseY = 0;
    private double _trauma = 0;

    /// <summary>Clamp a zoom vector to ensure the <see cref="Camera2D"/> doesn't zoom out too far to be able to see outside its limits.</summary>
    /// <param name="zoom">Zoom vector to clamp.</param>
    /// <returns>
    /// The zoom vector with its components clamped to ensure the <see cref="Viewport"/> rect is inside the camera limits.
    /// </returns>
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

    /// <summary>Helper function for setting <see cref="Zoom"/> internally without clearing the zoom memory.</summary>
    private void SetZoom(Vector2 value)
    {
        _zoomTween?.Kill();
        _zoomTarget = _zoom = Engine.IsEditorHint() || !Camera.IsInsideTree() ? value : ClampZoom(value);
        if (Camera != null)
            Camera.Zoom = _zoom;
    }

    /// <summary>Helper function for setting <see cref="ZoomTarget"/> internally without clearing the zoom memory.</summary>
    private void SetZoomTarget(Vector2 value)
    {
        if (Engine.IsEditorHint())
            _zoomTarget = value;
        else
        {
            _zoomTarget = ClampZoom(value);

            if (_zoomTween.IsValid())
                _zoomTween.Kill();
            _zoomTween = CreateTween();
            _zoomTween
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out)
                .TweenMethod(Callable.From((Vector2 zoom) => Camera.Zoom = _zoom = zoom), Zoom, _zoomTarget, ZoomDuration);
        }
    }

    /// <summary>Object the camera is tracking. Can be null to not track anything.</summary>
    [Export] public BoundedNode2D Target = null;

    /// <summary>Box bounding the area in the world that the camera is allowed to see.</summary>
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

    /// <summary><see cref="Camera2D"/> zoom. Ratio of world pixel size to real pixel size (so a zoom of 2 presents everything in double size).</summary>
    /// <remarks>Setting this will clear the zoom memory (so calling <see cref="PopZoom"/> will fail).</remarks>
    [ExportGroup("Zoom")]
    [Export] public Vector2 Zoom
    {
        get => _zoom;
        set
        {
            SetZoom(value);
            if (!Engine.IsEditorHint())
                ClearZoomMemory();
        }
    }

    [ExportGroup("Zoom", "Zoom")]

    /// <summary>Minimum allowable zoom (largest view of the world).</summary>
    [Export] public Vector2 ZoomMin = new(0.5f, 0.5f);

    /// <summary>Maximum allowable zoom (smallest view of the world).</summary>
    [Export] public Vector2 ZoomMax = new(3, 3);

    /// <summary>Amount of time to take to reach the next zoom setting.</summary>
    [Export] public double ZoomDuration = 0.2;

    [ExportGroup("Zoom/Factor", "ZoomFactor")]

    /// <summary>Amount to zoom the <see cref="Camera2D"/> each time it's digitally zoomed.</summary>
    [Export] public float ZoomFactorDigital = 0.25f;

    /// <summary>Amount to zoom the <see cref="Camera2D"/> while it's being zoomed with an analog stick.</summary>
    [Export] public float ZoomFactorAnalog = 2;

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

    [ExportGroup("Shake", "Shake")]

    /// <summary>Amount to reduce shake magnitude every second</summary>
    [Export] public float ShakeDecay = 0.6f;

    /// <summary>Maximum amount the camera can offset while shaking.</summary>
    [Export] public Vector2 ShakeMaxOffset = new(75, 150);

    /// <summary>Maximum amount the camera can rotate while shaking.</summary>
    [Export] public float ShakeMaxRoll = 0.2f;

    /// <summary>Exponent to apply to shake magnitude when computing camera offset. Higher amount means smoother shake (because the magnitude is between 0 and 1).</summary>
    [Export] public double ShakeTraumaExponent = 2;

    /// <summary>Noise generator for camera shaking.</summary>
    [Export] public FastNoiseLite ShakeNoise = null;

    /// <summary>Draw the camera zones for help with debugging and visualization.</summary>
    [ExportGroup("Editor")]
    [Export] public bool DrawZones = false;

    /// <summary>Draw camera limits for help with debugging and visualization.</summary>
    [ExportGroup("Editor")]
    [Export] public bool DrawLimits = false;

    /// <summary>Draw camera target points for help with debugging and visualization. Does not draw in the editor.</summary>
    [ExportGroup("Editor")]
    [Export] public bool DrawTargets = false;

    /// <summary>Target zoom level to smoothly zoom to.</summary>
    /// <remarks>Setting this will clear the zoom memory (so calling <see cref="PopZoom"/> will fail).</remarks>
    public Vector2 ZoomTarget
    {
        get => _zoomTarget;
        set
        {
            SetZoomTarget(value);
            if (!Engine.IsEditorHint())
                ClearZoomMemory();
        }
    }

    /// <summary>Magnitude the camera is shaking.</summary>
    public double Trauma
    {
        get => _trauma;
        set => _trauma = Mathf.Clamp(value, 0, 1);
    }

    /// <returns>The <see cref="Viewport"/> rectangle.</returns>
    public Rect2 GetScreenRect()
    {
        if (Engine.IsEditorHint())
            return new(Vector2.Zero, new Vector2(ProjectSettings.GetSetting("display/window/size/viewport_width").As<int>(), ProjectSettings.GetSetting("display/window/size/viewport_height").As<int>())/Zoom);
        else
            return Camera.GetViewportRect();
    }

    /// <returns>The <see cref="Viewport"/> rectangle projected onto the world.</returns>
    public Rect2 GetProjectedViewportRect() => Camera.GetCanvasTransform().AffineInverse()*GetScreenRect();

    /// <summary>Set a new zoom vector, saving the old one to be restored later.</summary>
    /// <param name="zoom">New zoom vector.</param>
    /// <param name="smooth">Whether or not the transition to the new zoom should be smooth.</param>
    public void PushZoom(Vector2 zoom, bool smooth=true)
    {
        _savedZooms.Push(Zoom);
        if (smooth)
            SetZoomTarget(zoom);
        else
            SetZoom(zoom);
    }

    /// <summary>Restore the most recent previous zoom vector.</summary>
    /// <param name="smooth">Wehther or not hte transition to the new zoom should be smooth.</param>
    /// <returns>The current zoom vector before restoring the previous one.</returns>
    public Vector2 PopZoom(bool smooth=true)
    {
        Vector2 zoom = Zoom;
        if (smooth)
            SetZoomTarget(_savedZooms.Pop());
        else
            SetZoom(_savedZooms.Pop());
        return zoom;
    }

    /// <returns><c>true</c> if there are any saved zoom vectors that can be restored, and <c>false</c> otherwise.</returns>
    public bool HasZoomMemory() => _savedZooms.Count != 0;

    /// <summary>Delete all memory of previous zoom vectors.</summary>
    public void ClearZoomMemory() => _savedZooms.Clear();

    public override void _Ready()
    {
        base._Ready();

        if (Target != null)
            _targetPreviousPosition = Target.GlobalPosition;
        _moveTween = CreateTween();
        _moveTween.Kill();

        _zoom = ClampZoom(_zoom);
        Camera.Zoom = _zoom;
        (Camera.LimitLeft, Camera.LimitTop) = _limits.Position;
        (Camera.LimitRight, Camera.LimitBottom) = _limits.End;
        _zoomTarget = _zoom;
        _zoomTween = CreateTween();
        _zoomTween.Kill();

        GD.Randomize();
        ShakeNoise.Seed = (int)GD.Randi();

        GlobalPosition = Camera.GetScreenCenterPosition();
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
            localLimits.Position -= GlobalPosition;

            DrawLine(new Vector2(localLimits.Position.X, localDeadzone.Position.Y), new Vector2(localLimits.End.X, localDeadzone.Position.Y), Colors.Purple);
            DrawLine(new Vector2(localDeadzone.Position.X, localLimits.Position.Y), new Vector2(localDeadzone.Position.X, localLimits.End.Y), Colors.Purple);
            DrawLine(new Vector2(localLimits.Position.X, localDeadzone.End.Y), new Vector2(localLimits.End.X, localDeadzone.End.Y), Colors.Purple);
            DrawLine(new Vector2(localDeadzone.End.X, localLimits.Position.Y), new Vector2(localDeadzone.End.X, localLimits.End.Y), Colors.Purple);

            FillInterior(localDeadzone, localLimits,  new(Colors.Purple.R, Colors.Purple.G, Colors.Purple.B, 0.25f));

            Rect2 targetDeadzone = localDeadzone with { Size = localDeadzone.Size*Zoom/ZoomTarget };
            targetDeadzone.Position += (localDeadzone.Size - targetDeadzone.Size)/2;
            DrawRect(targetDeadzone, Colors.Blue, filled:false);
        }

        if (!Engine.IsEditorHint() && DrawTargets && Target is not null)
        {
            Rect2 boxRelative = Target.GlobalBoundingBox with { Position = Target.GlobalPosition - Position };
            DrawRect(boxRelative, Colors.Purple, filled:false);
            if (!localDeadzone.Contains(boxRelative, perimeter:true))
            {
                DrawCircle(GetTargetFocusPoint(boxRelative, localDeadzone), 3, Colors.Purple);
                DrawCircle(GetTargetFocusPoint(boxRelative, localDeadzone).Clamp(localDeadzone.Position, localDeadzone.End), 3, Colors.Purple.Darkened(0.5f));
                DrawCircle(GetMoveTargetPosition(boxRelative, localDeadzone, bounds), 3, Colors.Blue);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionPressed(InputActions.DigitalZoomIn))
            ZoomTarget += Vector2.One*ZoomFactorDigital;
        if (@event.IsActionPressed(InputActions.DigitalZoomOut))
            ZoomTarget -= Vector2.One*ZoomFactorDigital;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            if (Target is not null)
            {
                if (!_moveTween.IsValid() || _targetPreviousPosition != Target.GlobalPosition)
                {
                    if (_moveTween.IsValid())
                        _moveTween.Kill();

                    Rect2 localDeadzone = ComputeDeadZone();
                    Rect2 targetDeadzone = localDeadzone with { Size = localDeadzone.Size*Zoom/ZoomTarget };
                    targetDeadzone.Position += (localDeadzone.Size - targetDeadzone.Size)/2;
                    Rect2 boxRelative = Target.GlobalBoundingBox with { Position = Target.GlobalPosition - Position };
                    if (!targetDeadzone.Contains(boxRelative, perimeter:true))
                    {
                        Vector2 moveTarget = GetMoveTargetPosition(boxRelative, targetDeadzone, GetTargetBounds());
                        if (moveTarget != Vector2.Zero)
                        {
                            _moveTween = CreateTween();
                            _moveTween
                                .SetTrans(Tween.TransitionType.Cubic)
                                .SetEase(Tween.EaseType.Out)
                                .TweenProperty(this, PropertyName.GlobalPosition.ToString(), GlobalPosition + moveTarget, DeadZoneSmoothTime);
                            _moveTween.Finished += () => EmitSignal(SignalName.ReachedTarget);
                        }
                    }
                }

                _targetPreviousPosition = Target.GlobalPosition;

                float zoom = Input.GetAxis(InputActions.AnalogZoomIn, InputActions.AnalogZoomOut);
                if (zoom != 0)
                    Zoom += Vector2.One*(float)(ZoomFactorAnalog*zoom*delta);
            }

            if (_trauma != 0)
            {
                Trauma -= ShakeDecay*delta;
                double amount = Math.Pow(_trauma, ShakeTraumaExponent);
                _noiseY++;
                Camera.Rotation = (float)(ShakeMaxRoll*amount*ShakeNoise.GetNoise2D(ShakeNoise.Seed, _noiseY));
                Camera.Offset = ShakeMaxOffset*(float)amount*new Vector2(ShakeNoise.GetNoise2D(1000, _noiseY), ShakeNoise.GetNoise2D(2000, _noiseY));
            }
        }
        if (Engine.IsEditorHint() || DrawZones || DrawLimits || DrawTargets)
            QueueRedraw();
    }
}