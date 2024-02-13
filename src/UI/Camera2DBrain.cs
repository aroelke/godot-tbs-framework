using Godot;

namespace UI;

[Icon("res://icons/Camera2DBrain.svg"), Tool]
public partial class Camera2DBrain : Node2D
{
    private Camera2D _camera = null;
    private Camera2D Camera => _camera ??= GetNodeOrNull<Camera2D>("Camera2D");

    private Vector2 _zoom = Vector2.One;
    private int _limitLeft = -10000000, _limitTop = -10000000, _limitRight = 10000000, _limitBottom = 10000000;

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

    [ExportGroup("Limit")]
    [Export] public int LimitLeft
    {
        get => _limitLeft;
        set
        {
            _limitLeft = value;
            if (Camera != null)
                Camera.LimitLeft = value;
        }
    }

    [ExportGroup("Limit")]
    [Export] public int LimitTop
    {
        get => _limitTop;
        set
        {
            _limitTop = value;
            if (Camera != null)
                Camera.LimitTop = value;
        }
    }

    [ExportGroup("Limit")]
    [Export] public int LimitRight
    {
        get => _limitRight;
        set
        {
            _limitRight = value;
            if (Camera != null)
                Camera.LimitRight = value;
        }
    }

    [ExportGroup("Limit")]
    [Export] public int LimitBottom
    {
        get => _limitBottom;
        set
        {
            _limitBottom = value;
            if (Camera != null)
                Camera.LimitBottom = value;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        Camera.Zoom = _zoom;
        Camera.LimitLeft     = _limitLeft;
        Camera.LimitTop      = _limitTop;
        Camera.LimitRight    = _limitRight;
        Camera.LimitBottom   = _limitBottom;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Target != null)
            Position = Target.Position;
    }
}