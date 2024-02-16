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
    /// <param name="distance">Distance to the camera's target position.</param>
    /// <param name="deadzone">Rectangle defining the dead zone.</param>
    /// <param name="limits">Limits where the center of the camera can be.</param>
    /// <returns>The position the center of the camera should move to.</returns>
    private static Vector2 GetMoveTargetPosition(Vector2 distance, Rect2 deadzone, Rect2 limits)
    {
        Rect2 moveBox = new Rect2().Expand((distance - distance.Clamp(deadzone.Position, deadzone.End)).Clamp(limits.Position, limits.End));
        return distance.Clamp(moveBox.Position, moveBox.End);
    }

    private Camera2D _camera = null;
    private Camera2D Camera => _camera ??= GetNodeOrNull<Camera2D>("Camera2D");

    private Vector2 _zoom = Vector2.One;
    private Rect2I _limits = new(-1000000, -1000000, 2000000, 2000000);

    /// <summary>Object the camera is tracking. Can be null to not track anything.</summary>
    [ExportGroup("Camera")]
    [Export] public Node2D Target = null;

    /// <summary>Camera zoom. Ratio of world pixel size to real pixel size (so a zoom of 2 presents everything in double size).</summary>
    [ExportGroup("Camera")]
    [Export] public Vector2 Zoom
    {
        get => _zoom;
        set
        {
            _zoom = value;
            if (Camera != null)
                Camera.Zoom = value;
        }
    }

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

    /// <summary>Speed the camera should move to get the target to re-enter the dead zone.</summary>
    [Export(PropertyHint.None, "suffix:px/s")] public double DeadZoneSmoothSpeed = 7.5f;

    /// <summary>Draw the camera margins for help with debugging and visualization. Camera target points are only drawn in-game.</summary>
    [Export] public bool DrawMargins = false;

    /// <returns>The viewport rectangle projected onto the world.</returns>
    public Rect2 GetProjectedViewportRect() => Camera.GetCanvasTransform().AffineInverse()*Camera.GetViewportRect();

    /// <returns>The rectangle bounding the position of the center ("target") of the camera.</returns>
    public Rect2 GetTargetBounds()
    {
        Rect2 projected = GetProjectedViewportRect();
        return (Rect2)Limits with { Position = Limits.Position + projected.Size/2 - Position, End = Limits.End - projected.Size/2 - Position };
    }

    /// <returns>The rectangle defining the dead zone in the world (rather than on screen).</returns>
    public Rect2 GetDeadZone()
    {
        Rect2 viewport = Camera.GetViewportRect();
        Vector2 deadzoneStart = new(DeadZoneLeft, DeadZoneTop), deadzoneEnd = new(DeadZoneRight, DeadZoneBottom);
        Rect2 viewportDeadzone = new Rect2() with { Position = viewport.Position + viewport.Size*(Vector2.One - deadzoneStart)/2, End = viewport.Position + viewport.Size - viewport.Size*(Vector2.One - deadzoneEnd)/2 };
        Rect2 localDeadzone = Camera.GetCanvasTransform().AffineInverse()*viewportDeadzone;
        return localDeadzone;
    }

    public override void _Ready()
    {
        base._Ready();

        Camera.Zoom = _zoom;
        (Camera.LimitLeft, Camera.LimitTop) = _limits.Position;
        (Camera.LimitRight, Camera.LimitBottom) = _limits.End;

        Position = Camera.GetScreenCenterPosition();
    }

    public override void _Draw()
    {
        if (DrawMargins)
        {
            if (!Engine.IsEditorHint())
                DrawCircle(Vector2.Zero, 3, Colors.Black);
            
            Rect2 bounds = GetTargetBounds();
            DrawRect(bounds, Colors.Red, filled:false);

            Rect2 localDeadzone = GetDeadZone();
            localDeadzone.Position -= Position;
            DrawRect(localDeadzone, Colors.Black, filled:false);

            if (!Engine.IsEditorHint() && Target != null)
            {
                Vector2 targetRelative = Target.Position - Position;
                if (!localDeadzone.HasPoint(targetRelative))
                {
                    DrawCircle(targetRelative, 3, Colors.Red);
                    DrawCircle(targetRelative.Clamp(localDeadzone.Position, localDeadzone.End), 3, Colors.Purple);
                    DrawCircle(GetMoveTargetPosition(targetRelative, localDeadzone, bounds), 3, Colors.Blue);
                }
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            Rect2 bounds = GetTargetBounds();
            Rect2 localDeadzone = GetDeadZone();
            localDeadzone.Position -= Position;

            if (Target != null)
            {
                Vector2 targetRelative = Target.Position - Position;
                if (!localDeadzone.HasPoint(targetRelative))
                    Position = Position.Lerp(Position + GetMoveTargetPosition(targetRelative, localDeadzone, bounds), (float)(DeadZoneSmoothSpeed*delta));
            }
            QueueRedraw();
        }
    }
}