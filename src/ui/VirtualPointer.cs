using Godot;
using level.ui;
using ui.input;

namespace ui;

/// <summary>A virtual mouse pointer that is controlled using an analog stick.</summary>
public partial class VirtualPointer : TextureRect
{
    /// <summary>Signals that the virtual pointer has moved in the canvas.</summary>
    /// <param name="position">Position of the virtual pointer.</param>
    [Signal] public delegate void PointerMovedEventHandler(Vector2 position);

    /// <summary>Signals that the pointer has been clicked.  Also used to signal a real mouse click.</summary>
    /// <param name="position">Position of the click.</param>
    [Signal] public delegate void PointerClickedEventHandler(Vector2 position);

    private InputManager _inputManager = null;
    private bool _accelerate = false;

    private InputManager InputManager => _inputManager ??= GetNode<InputManager>("/root/InputManager");

    /// <summary>Projection of the pointer's screen position onto the world.</summary>
    [Export] public PointerProjection Projection = null;

    /// <summary>Speed in pixels/second the pointer moves when in analog mode.</summary>
    [ExportGroup("Analog Movement")]
    [Export] public double Speed = 600;

    /// <summary>Multiplier applied to the pointer speed when the accelerate button is held down in analog mode.</summary>
    [ExportGroup("Analog Movement")]
    [Export] public double Acceleration = 3;

    /// <summary>Move the virtual pointer to a position on the viewport and signal the move.</summary>
    /// <param name="target">Position to move to.</param>
    public void Warp(Vector2 target)
    {
        if (Position != target)
        {
            Position = target;
            EmitSignal(SignalName.PointerMoved, Position);
        }
    }

    /// <summary>When switching between mouse and non-mouse input, warp the mouse and virtual pointer around to appear seamless.</summary>
    /// <param name="mode">Current input mode.</param>
    public void OnInputModeChanged(input.InputMode mode)
    {
        if (mode == input.InputMode.Mouse)
            Input.WarpMouse(Position);
    }

    /// <summary>When the mouse enters the screen, warp to its entry position.</summary>
    /// <param name="position">Position the mouse entered the screen on.</param>
    public void OnMouseEntered(Vector2 position) => Warp(position);

    /// <summary>When the mouse exits the screen, warp to edge of the screen near where it exited.</summary>
    /// <param name="position">Position on screen close to where the mouse exited.</param>
    public void OnMouseExited(Vector2 position) => Warp(position);

    /// <summary>When the projection moves during digital input, move the virtual pointer to where it is on the screen.</summary>
    /// <param name="viewport">Projection's position in the viewport.</param>
    /// <param name="world">Projection's position in the world.</param>
    public void OnProjectionMoved(Vector2 viewport, Vector2 world)
    {
        if (InputManager.Mode == input.InputMode.Digital)
            Warp(viewport);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        switch (InputManager.Mode)
        {
        case input.InputMode.Mouse when @event is InputEventMouseMotion:
            Warp(GetViewport().GetMousePosition());
            return;
        case input.InputMode.Analog:
            if (Input.IsActionJustPressed("cursor_analog_accelerate"))
            {
                GetViewport().SetInputAsHandled();
                _accelerate = true;
                return;
            }
            if (Input.IsActionJustReleased("cursor_analog_accelerate"))
            {
                GetViewport().SetInputAsHandled();
                _accelerate = false;
                return;
            }
            break;
        }
        if (Input.IsActionJustReleased("cursor_select") && (InputManager.Mode == input.InputMode.Mouse || InputManager.Mode == input.InputMode.Analog))
        {
            GetViewport().SetInputAsHandled();
            EmitSignal(SignalName.PointerClicked, Position);
            return;
        }
    }

    public override void _Ready()
    {
        base._Ready();
        InputManager.InputModeChanged += OnInputModeChanged;
        InputManager.MouseEntered += OnMouseEntered;
        InputManager.MouseExited += OnMouseExited;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        switch (InputManager.Mode)
        {
        case input.InputMode.Digital:
            if (Projection != null)
                Warp(Projection.ViewportPosition);
            break;
        case input.InputMode.Analog:
            double speed = _accelerate ? (Speed*Acceleration) : Speed;
            Warp((Position + (InputManager.GetAnalogVector()*(float)(speed*delta))).Clamp(GetViewportRect().Position, GetViewportRect().End));
            break;
        }
    }
}