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
    private InputManager InputManager => _inputManager ??= GetNode<InputManager>("/root/InputManager");

    /// <summary>Speed in pixels/second the cursor moves when in analog mode.</summary>
    [Export] public double Speed = 600;

    public override void _Process(double delta)
    {
        base._Process(delta);

        Vector2 old = Position;
        if (InputManager.Mode == input.InputMode.Analog)
        {
            Position = (Position + (InputManager.GetAnalogVector()*(float)(Speed*delta))).Clamp(GetViewportRect().Position, GetViewportRect().End);
        }
        if (Position != old)
            EmitSignal(SignalName.PointerMoved, old, Position);
    }
}