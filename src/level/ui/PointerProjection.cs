using Godot;
using level.manager;
using ui.input;

namespace level.ui;

/// <summary>Projection of the pointer (virtual or real) onto the map, for controlling the cursor.</summary>
public partial class PointerProjection : Node2D, ILevelManaged
{
    /// <summary>Signals that the pointer projection has moved on the map.</summary>
    /// <param name="previous">Previous position on the map.</param>
    /// <param name="current">Current position on the map.</param>
    [Signal] public delegate void PointerMovedEventHandler(Vector2 previous, Vector2 current);

    private InputManager _inputManager = null;
    private LevelManager _levelManager = null;

    private InputManager InputManager => _inputManager ??= GetNode<InputManager>("/root/InputManager");

    public LevelManager LevelManager => _levelManager ??= GetParent<LevelManager>();

    public override void _Process(double delta)
    {
        base._Process(delta);

        Vector2 old = Position;
        if (InputManager.Mode == InputMode.Mouse)
        {
            Position = InputManager.LastKnownPointerPosition switch
            {
                Vector2 pos => (LevelManager.GetGlobalTransform()*LevelManager.GetCanvasTransform()).AffineInverse()*pos,
                _ => LevelManager.GetLocalMousePosition()
            };
        }
        if (Position != old)
            EmitSignal(SignalName.PointerMoved, old, Position);
    }
}