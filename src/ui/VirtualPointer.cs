using Godot;
using ui.input;

namespace ui;

/// <summary>A virtual mouse pointer that is controlled using an analog stick.</summary>
public partial class VirtualPointer : TextureRect
{
    /// <summary>Signals that the virtual pointer has moved in the canvas.</summary>
    /// <param name="previous">Previous position of the virtual pointer.</param>
    /// <param name="current">Next position of the virtual pointer.</param>
    [Signal] public delegate void PointerMovedEventHandler(Vector2 previous, Vector2 current);

    private InputManager _inputManager = null;
    private bool _accelerate = false;
    private Vector2 _projectionPosition = Vector2.Zero;

    private InputManager InputManager => _inputManager ??= GetNode<InputManager>("/root/InputManager");

    /// <summary>Speed in pixels/second the pointer moves when in analog mode.</summary>
    [Export] public double Speed = 600;

    /// <summary>Multiplier applied to the pointer speed when the accelerate button is held down in analog mode.</summary>
    [Export] public double Acceleration = 3;

    /// <summary>Move the virtual pointer to a position on the viewport and signal the move.</summary>
    /// <param name="position">Position to move to.</param>
    public void Warp(Vector2 position)
    {
        Vector2 old = Position;
        Position = position;
        if (old != Position)
            EmitSignal(SignalName.PointerMoved, old, Position);
    }

    /// <summary>When switching between mouse and non-mouse input, warp the mouse and virtual pointer around to appear seamless.</summary>
    /// <param name="previous">Previous input mode.</param>
    /// <param name="current">Current input mode.</param>
    public void OnInputModeChanged(input.InputMode previous, input.InputMode current)
    {
        if (current == input.InputMode.Mouse)
            Input.WarpMouse(Position);
        else
            Warp(GetViewport().GetMousePosition());
    }

    /// <summary>When the projection moves during digital input, move the virtual pointer to where it is on the screen.</summary>
    /// <param name="viewport">Projection's position in the viewport.</param>
    /// <param name="world">Projection's position in the world.</param>
    public void OnProjectionMoved(Vector2 viewport, Vector2 world)
    {
        if (InputManager.Mode == input.InputMode.Digital)
            Position = _projectionPosition = viewport;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        switch (InputManager.Mode)
        {
        case input.InputMode.Mouse:
            Warp(GetViewport().GetMousePosition());
            break;
        case input.InputMode.Analog:
            if (Input.IsActionJustPressed("cursor_analog_accelerate"))
            {
                _accelerate = true;
                GetViewport().SetInputAsHandled();
            }
            if (Input.IsActionJustReleased("cursor_analog_accelerate"))
            {
                _accelerate = false;
                GetViewport().SetInputAsHandled();
            }
            break;
        }
    }

    public override void _Ready()
    {
        base._Ready();
        InputManager.InputModeChanged += OnInputModeChanged;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        switch (InputManager.Mode)
        {
        case input.InputMode.Digital:
            Warp(_projectionPosition);
            break;
        case input.InputMode.Analog:
            double speed = _accelerate ? (Speed*Acceleration) : Speed;
            Warp((Position + (InputManager.GetAnalogVector()*(float)(speed*delta))).Clamp(GetViewportRect().Position, GetViewportRect().End));
            break;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        InputManager.InputModeChanged -= OnInputModeChanged;
    }
}