using battle;
using Godot;
using Godot.NativeInterop;

namespace ui;

/// <summary>Virtual cursor that can be moved via mouse movement, digitally, or via analog.</summary>
public partial class VirtualMouse : Sprite2D
{
    /// <summary>
    /// Method used for moving the virtual mouse cursor: the real mouse, using digital input (e.g. keyboard keys, controller dpad), or
    /// using analog inputs (e.g. controller analog sticks).
    /// </summary>
    public enum Mode { Mouse, Digital, Analog }

    /// <summary>Signals that the cursor has moved.</summary>
    /// <param name="previous">Previous position of the cursor.</param>
    /// <param name="current">Current position of the cursor.</param>
    [Signal] public delegate void MovedEventHandler(Vector2 previous, Vector2 current);

    /// <summary>Signals that the input mode of the cursor has changed.</summary>
    /// <param name="mode">New input mode.</param>
    [Signal] public delegate void ModeChangedEventHandler(Mode mode);

    private BattleMap _map = null;
    private Timer _echo = null;
    private bool _echoing = false;
    private Mode _mode = Mode.Mouse;
    private bool _tracking = false;
    private Vector2 _previous = Vector2.Zero;
    private Vector2I _direction = Vector2I.Zero;

    private BattleMap Map => _map ??= GetParent<BattleMap>();
    private Timer EchoTimer => _echo ??= GetNode<Timer>("EchoTimer");

    [Export] public double EchoDelay = 0.3;
    [Export] public double EchoInterval = 0.03;

    /// <summary>Current mode used for moving the virtual mouse.</summary>
    public Mode MoveMode
    {
        get => _mode;
        private set
        {
            Mode old = _mode;
            _mode = value;
            if (old != _mode)
                EmitSignal(SignalName.ModeChanged, Variant.CreateTakingOwnershipOfDisposableValue(VariantUtils.CreateFrom(_mode)));
            Visible = _mode != Mode.Digital;
        }
    }

    private Vector2 MousePosition() => GetParent<CanvasItem>()?.GetLocalMousePosition() ?? GetGlobalMousePosition();

    public override void _Notification(int what)
    {
        base._Notification(what);
        switch ((long)what)
        {
            case NotificationWMMouseEnter: case NotificationVpMouseEnter:
                Position = MousePosition();
                _tracking = true;
                break;
            case NotificationWMMouseExit: case NotificationVpMouseExit:
                _tracking = false;
                break;
        }
    }

    public void Jump(Vector2I cell)
    {
        Position = Map.PositionOf(Map.Clamp(cell)) + Map.CellSize/2;
    }

    public void OnEchoTimeout()
    {
        Jump(Map.CellOf(Position) + _direction);
        if (EchoInterval > GetProcessDeltaTime())
        {
            EchoTimer.WaitTime = EchoInterval;
            EchoTimer.Start();
        }
        else
            _echoing = true;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event is InputEventMouseMotion mm)
        {
            if (mm.Velocity.Length() > 0)
            {
                // This is matrix math, so the order of operations matters!
                if (MoveMode != Mode.Mouse && Position != MousePosition())
                    Input.WarpMouse(Map.GetGlobalTransform()*Map.GetCanvasTransform()*Position);
                if (_tracking)
                {
                    Position = MousePosition();
                    MoveMode = Mode.Mouse;
                }
            }
        }
        else
        {
            Vector2I current = Map.CellOf(Position);

            Vector2I skip = (Vector2I)Input.GetVector("cursor_skip_left", "cursor_skip_right", "cursor_skip_up", "cursor_skip_down").Round();
            if (skip != Vector2.Zero)
            {
                Jump(new(
                    skip.X < 0 ? 0 : skip.X > 0 ? Map.Size.X - 1 : current.X,
                    skip.Y < 0 ? 0 : skip.Y > 0 ? Map.Size.Y - 1 : current.Y
                ));
                MoveMode = Mode.Digital;
            }
            else
            {
                Vector2I dir = (Vector2I)Input.GetVector("cursor_left", "cursor_right", "cursor_up", "cursor_down").Round();
                if (dir != _direction)
                {
                    EchoTimer.Stop();
                    _echoing = false;

                    if (dir != Vector2I.Zero)
                    {
                        if (dir.Abs().X + dir.Abs().Y > _direction.Abs().X + _direction.Abs().Y)
                            Jump(current + (dir - _direction));
                        else
                            Jump(current + dir);
                        _direction = dir;
                        MoveMode = Mode.Digital;

                        EchoTimer.WaitTime = EchoDelay;
                        EchoTimer.Start();
                    }
                    else
                        _direction = Vector2I.Zero;
                }
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        switch (MoveMode)
        {
          case Mode.Mouse:
            if (_tracking && Position != MousePosition())
                Position = MousePosition();
            break;
          case Mode.Digital:
            if (_echoing)
                Jump(Map.CellOf(Position) + _direction);
            break;
          default:
            break;
        }

        if (MoveMode == Mode.Mouse && _tracking && Position != MousePosition())
            Position = MousePosition();

        if (Position != _previous)
        {
            EmitSignal(SignalName.Moved, _previous, Position);
            _previous = Position;
        }
    }
}