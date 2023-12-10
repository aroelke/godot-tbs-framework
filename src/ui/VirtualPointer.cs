using Godot;
using ui.input;

namespace ui;

/// <summary>A virtual mouse pointer that is controlled using an analog stick.</summary>
public partial class VirtualPointer : TextureRect
{
    private InputManager _inputManager = null;
    private InputManager InputManager => _inputManager ??= GetNode<InputManager>("/root/InputManager");

    /// <summary>Speed in pixels/second the cursor moves when in analog mode.</summary>
    [Export] public double Speed = 600;

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (InputManager.Mode == input.InputMode.Analog)
        {
            Position = (Position + (InputManager.GetAnalogVector()*(float)(Speed*delta))).Clamp(GetViewportRect().Position, GetViewportRect().End);
        }
    }
}