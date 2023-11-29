using battle;
using Godot;
using Godot.NativeInterop;

namespace ui;

/// <summary>Virtual cursor that can be moved via mouse movement, digitally, or via analog.</summary>
public abstract partial class VirtualMouse : Sprite2D
{
    /// <summary>Signals that the cursor has moved.</summary>
    /// <param name="previous">Previous position of the cursor.</param>
    /// <param name="current">Current position of the cursor.</param>
    [Signal] public delegate void MovedEventHandler(Vector2 previous, Vector2 current);

    /// <summary>Signals that the input mode of the cursor has changed.</summary>
    /// <param name="mode">New input mode.</param>
    [Signal] public delegate void InputModeChangedEventHandler(InputMode mode);

    private CanvasItem _parent = null;
    private Timer _echoTimer;
    private bool _echoing = false;
    private InputMode _mode = InputMode.Digital;
    private Vector2? _lastKnownPointerPosition = null;
    private Vector2 _previous = Vector2.Zero;
    private Vector2I _direction = Vector2I.Zero;
    private bool _accelerate = false;

    private CanvasItem Parent => _parent ??= GetParent<CanvasItem>();

    private Vector2 MousePosition() => GetParent<CanvasItem>()?.GetLocalMousePosition() ?? GetGlobalMousePosition();

    /// <summary>Speed in pixels/second the cursor moves when in analog mode.</summary>
    [Export] public double CursorSpeed = 400;

    /// <summary>Multiplier applied to the cursor speed when the accelerate button is held down in analog mode.</summary>
    [Export] public double Acceleration = 3;

    /// <summary>Initial delay after pressing a digital movement key/button to start echoing the movement.</summary>
    [Export] public double EchoDelay = 0.3;

    /// <summary>Delay between movement echoes while holding a digital movement key/button down.</summary>
    [Export] public double EchoInterval = 0.03;

    /// <summary>Jump the cursor in a direction. Where it actually goes is implementation-defined.</summary>
    /// <param name="direction">Direction to jump, which will be either 1, 0, or -1 in the X- or Y-axes.</param>
    public abstract void Jump(Vector2I direction);

    /// <summary>Skip to the edge of the world in a direction.</summary>
    /// <param name="direction">Edge to jump to. On each axis, 1 represents the edge in the positive direction and -1 represents the one in the negative direction.</param>
    public abstract void Skip(Vector2I direction);

    /// <summary>Clamp a position within an implementation-defined bounding box.</summary>
    /// <param name="position">Position to clamp.</param>
    /// <returns>A new position computed by clamping the given one within the bounding box.</returns>
    public abstract Vector2 Clamp(Vector2 position);

    /// <summary>Current input mode used for moving the virtual mouse.</summary>
    public InputMode InputMode
    {
        get => _mode;
        private set
        {
            InputMode old = _mode;
            _mode = value;
            if (old != _mode)
                EmitSignal(SignalName.InputModeChanged, Variant.CreateTakingOwnershipOfDisposableValue(VariantUtils.CreateFrom(_mode)));
            Visible = _mode != InputMode.Digital;
        }
    }

    /// <summary>Start/continue echo movement of the cursor.</summary>
    public void OnEchoTimeout()
    {
        Jump(_direction);
        if (EchoInterval > GetProcessDeltaTime())
        {
            _echoTimer.WaitTime = EchoInterval;
            _echoTimer.Start();
        }
        else
            _echoing = true;
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        switch ((long)what)
        {
        case NotificationWMMouseEnter: case NotificationVpMouseEnter:
            Position = MousePosition();
            _lastKnownPointerPosition = null;
            break;
        case NotificationWMMouseExit: case NotificationVpMouseExit:
            _lastKnownPointerPosition = GetViewport().GetMousePosition().Clamp(Vector2.Zero, GetViewportRect().Size);
            break;
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event is InputEventMouseMotion mm)
        {
            if (mm.Velocity.Length() > 0 && _lastKnownPointerPosition == null)
            {
                // This is matrix math, so the order of operations matters!
                if (InputMode != InputMode.Mouse && Position != MousePosition())
                    Input.WarpMouse(Parent.GetGlobalTransform()*Parent.GetCanvasTransform()*Position);
                Position = MousePosition();
                InputMode = InputMode.Mouse;
            }
        }
        else
        {
            Vector2I skip = (Vector2I)Input.GetVector("cursor_skip_left", "cursor_skip_right", "cursor_skip_up", "cursor_skip_down").Round();
            if (skip != Vector2.Zero)
            {
                Skip(skip);
                InputMode = InputMode.Digital;
            }
            else if (Input.GetVector("cursor_analog_left", "cursor_analog_right", "cursor_analog_up", "cursor_analog_down") != Vector2.Zero)
            {
                InputMode = InputMode.Analog;
            }
            else
            {
                Vector2I dir = (Vector2I)Input.GetVector("cursor_digital_left", "cursor_digital_right", "cursor_digital_up", "cursor_digital_down").Round();
                if (dir != _direction)
                {
                    _echoTimer.Stop();
                    _echoing = false;

                    if (dir != Vector2I.Zero)
                    {
                        if (dir.Abs().X + dir.Abs().Y > _direction.Abs().X + _direction.Abs().Y)
                            Jump(dir - _direction);
                        else
                            Jump(dir);
                        _direction = dir;
                        InputMode = InputMode.Digital;

                        _echoTimer.WaitTime = EchoDelay;
                        _echoTimer.Start();
                    }
                    else
                        _direction = Vector2I.Zero;
                }
            }

            if (InputMode == InputMode.Analog)
                _accelerate = Input.IsActionPressed("cursor_analog_accelerate");
        }
    }

    public override void _Ready()
    {
        base._Ready();

        AddChild(_echoTimer = new Timer());
        _echoTimer.Timeout += OnEchoTimeout;
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        switch (InputMode)
        {
        case InputMode.Mouse:
            Position = _lastKnownPointerPosition switch {
                Vector2 pos => Clamp((Parent.GetGlobalTransform()*Parent.GetCanvasTransform()).AffineInverse()*pos),
                null => MousePosition()
            };
            break;
        case InputMode.Digital:
            if (_echoing)
                Jump(_direction);
            break;
        case InputMode.Analog:
            double speed = _accelerate ? (CursorSpeed*Acceleration) : CursorSpeed;
            Position = Clamp(Position + Input.GetVector("cursor_analog_left", "cursor_analog_right", "cursor_analog_up", "cursor_analog_down")*(float)(speed*delta));
            break;
        default:
            break;
        }

        if (Position != _previous)
        {
            EmitSignal(SignalName.Moved, _previous, Position);
            _previous = Position;
        }
    }
}