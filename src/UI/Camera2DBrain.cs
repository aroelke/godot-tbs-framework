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
    [Export] public Rect2I Limits
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

    public override void _Ready()
    {
        base._Ready();

        Camera.Zoom = _zoom;
        (Camera.LimitLeft, Camera.LimitTop) = _limits.Position;
        (Camera.LimitRight, Camera.LimitBottom) = _limits.End;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Target != null)
            Position = Target.Position;
    }
}