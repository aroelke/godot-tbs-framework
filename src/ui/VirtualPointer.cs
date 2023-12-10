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

    private InputManager InputManager => _inputManager ??= GetNode<InputManager>("/root/InputManager");

    /// <summary>Speed in pixels/second the pointer moves when in analog mode.</summary>
    [Export] public double Speed = 600;

    /// <summary>Multiplier applied to the pointer speed when the accelerate button is held down in analog mode.</summary>
    [Export] public double Acceleration = 3;

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (InputManager.Mode == input.InputMode.Analog)
        {
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
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Vector2 old = Position;
        if (InputManager.Mode == input.InputMode.Analog)
        {
            double speed = _accelerate ? (Speed*Acceleration) : Speed;
            Position = (Position + (InputManager.GetAnalogVector()*(float)(speed*delta))).Clamp(GetViewportRect().Position, GetViewportRect().End);
        }
        if (Position != old)
            EmitSignal(SignalName.PointerMoved, old, Position);
    }
}