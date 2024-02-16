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

    [ExportGroup("Soft Zone", "SoftZone")]

    [Export(PropertyHint.Range, "0, 1")] public float SoftZoneLeft = 0.5f;
    [Export(PropertyHint.Range, "0, 1")] public float SoftZoneTop = 0.5f;
    [Export(PropertyHint.Range, "0, 1")] public float SoftZoneRight = 0.5f;
    [Export(PropertyHint.Range, "0, 1")] public float SoftZoneBottom = 0.5f;

    /// <summary>Draw the camera zones for help with debugging and visualization.</summary>
    [ExportGroup("Editor")]
    [Export] public bool DrawZones = false;

    /// <summary>Draw camera limits for help with debugging and visualization.</summary>
    [ExportGroup("Editor")]
    [Export] public bool DrawLimits = false;

    /// <summary>Draw camera target points for help with debugging and visualization. Does not draw in the editor.</summary>
    [ExportGroup("Editor")]
    [Export] public bool DrawTargets = false;

    public Rect2 GetScreenRect()
    {
        if (Engine.IsEditorHint())
            return new(new Vector2(0, 0), new Vector2(ProjectSettings.GetSetting("display/window/size/viewport_width").As<int>(), ProjectSettings.GetSetting("display/window/size/viewport_height").As<int>())/Zoom);
        else
            return Camera.GetViewportRect();
    }

    /// <returns>The viewport rectangle projected onto the world.</returns>
    public Rect2 GetProjectedViewportRect() => Camera.GetCanvasTransform().AffineInverse()*GetScreenRect();

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
        if (DrawZones && !Engine.IsEditorHint())
            DrawCircle(Vector2.Zero, 3, Colors.Black);
        
        Rect2 bounds = GetTargetBounds();
        if (DrawLimits)
            DrawRect(bounds, Colors.Black, filled:false);

        Rect2 localDeadzone = GetZone(DeadZoneLeft, DeadZoneTop, DeadZoneRight, DeadZoneBottom);
        localDeadzone.Position -= Position;
        if (DrawZones)
            DrawRect(localDeadzone, Colors.Purple, filled:false);

        if (!Engine.IsEditorHint() && DrawTargets && Target != null)
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

    public override void _PhysicsProcess(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            Rect2 bounds = GetTargetBounds();
            Rect2 localDeadzone = GetZone(DeadZoneLeft, DeadZoneTop, DeadZoneRight, DeadZoneBottom);
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