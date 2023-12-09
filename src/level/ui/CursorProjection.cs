using Godot;
using level.manager;
using ui.input;

namespace level.ui;

/// <summary>Projection of the pointer (virtual or real) onto the map, for controlling the cursor.</summary>
public partial class CursorProjection : Node2D, ILevelManaged
{
    private InputManager _inputManager = null;
    private LevelManager _levelManager = null;

    private InputManager InputManager => _inputManager ??= GetNode<InputManager>("/root/InputManager");

    public LevelManager LevelManager => _levelManager ??= GetParent<LevelManager>();

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (InputManager.Mode == InputMode.Mouse)
            Position = LevelManager.GetLocalMousePosition();
    }
}