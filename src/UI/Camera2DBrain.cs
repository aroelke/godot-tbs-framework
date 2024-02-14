using Godot;

namespace UI;

[Icon("res://icons/Camera2DBrain.svg"), Tool]
public partial class Camera2DBrain : Node2D
{
    private Camera2D _camera = null;
    private Camera2D Camera => _camera ??= GetNodeOrNull<Camera2D>("Camera2D");

    private Vector2 _zoom = Vector2.One;
    private Rect2I _limits = new(-1000000, -1000000, 2000000, 2000000);

    [ExportGroup("Camera")]
    [Export] public Node2D Target = null;

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
    [Export(PropertyHint.Range, "0, 1")] public float DeadZoneLeft = 0.5f;

    [Export(PropertyHint.Range, "0, 1")] public float DeadZoneTop = 0.5f;

    [Export(PropertyHint.Range, "0, 1")] public float DeadZoneRight = 0.5f;

    [Export(PropertyHint.Range, "0, 1")] public float DeadZoneBottom = 0.5f;

    [Export(PropertyHint.None, "suffix:px/s")] public double DeadZoneSmoothSpeed = 7.5f;

    /// <returns>The viewport rectangle projected onto the world.</returns>
    public Rect2 GetProjectedViewportRect() => Camera.GetCanvasTransform().AffineInverse()*Camera.GetViewportRect();

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
        DrawCircle(Vector2.Zero, 3, Colors.Black);

        Rect2 viewport = Camera.GetViewportRect();
        Rect2 projected = Camera.GetCanvasTransform().AffineInverse()*viewport;
        Rect2 bounds = (Rect2)Limits with { Position = Limits.Position + projected.Size/2 - Position, End = Limits.End - projected.Size/2 - Position };
        DrawRect(bounds, Colors.Red, filled:false);

        Vector2 deadzoneStart = new(DeadZoneLeft, DeadZoneTop), deadzoneEnd = new(DeadZoneRight, DeadZoneBottom);
        Rect2 viewportDeadzone = new Rect2() with { Position = viewport.Position + viewport.Size*(Vector2.One - deadzoneStart)/2, End = viewport.Position + viewport.Size - viewport.Size*(Vector2.One - deadzoneEnd)/2 };
        Rect2 localDeadzone = Camera.GetCanvasTransform().AffineInverse()*viewportDeadzone;
        localDeadzone.Position -= Position;
        DrawRect(localDeadzone, Colors.Black, filled:false);

        if (Target != null)
        {
            Vector2 targetRelative = Target.Position - Position;
            if (!localDeadzone.HasPoint(targetRelative))
            {
                DrawCircle(targetRelative, 3, Colors.Red);
                DrawCircle(targetRelative.Clamp(localDeadzone.Position, localDeadzone.End), 3, Colors.Purple);

                Rect2 moveBox = new Rect2().Expand((targetRelative - targetRelative.Clamp(localDeadzone.Position, localDeadzone.End)).Clamp(bounds.Position, bounds.End));
                DrawRect(moveBox, Colors.Blue, filled:false);

                Vector2 moveTarget = targetRelative.Clamp(moveBox.Position, moveBox.End);
                DrawCircle(moveTarget, 3, Colors.Blue);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            if (Target != null)
            {
                Rect2 viewport = Camera.GetViewportRect();
                Vector2 deadzoneStart = new(DeadZoneLeft, DeadZoneTop), deadzoneEnd = new(DeadZoneRight, DeadZoneBottom);
                Rect2 viewportDeadzone = new Rect2() with { Position = viewport.Position + viewport.Size*(Vector2.One - deadzoneStart)/2, End = viewport.Position + viewport.Size - viewport.Size*(Vector2.One - deadzoneEnd)/2 };
                Rect2 localDeadzone = Camera.GetCanvasTransform().AffineInverse()*viewportDeadzone;
                localDeadzone.Position -= Position;

                if (Target != null)
                {
                    Vector2 targetRelative = Target.Position - Position;
                    if (!localDeadzone.HasPoint(targetRelative))
                    {
                        Rect2 projected = Camera.GetCanvasTransform().AffineInverse()*viewport;
                        Rect2 bounds = (Rect2)Limits with { Position = Limits.Position + projected.Size/2 - Position, End = Limits.End - projected.Size/2 - Position };
                        Rect2 moveBox = new Rect2().Expand((targetRelative - targetRelative.Clamp(localDeadzone.Position, localDeadzone.End)).Clamp(bounds.Position, bounds.End));
                        Vector2 moveTarget = targetRelative.Clamp(moveBox.Position, moveBox.End);

                        Position = Position.Lerp(Position + moveTarget, (float)(DeadZoneSmoothSpeed*delta));
                    }
                }
            }
            QueueRedraw();
        }
    }
}