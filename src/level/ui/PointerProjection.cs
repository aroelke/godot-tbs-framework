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

    /// <summary>
    /// Move the pointer projection to a new position. This does not affect the actual mouse pointer or any virtual pointers
    /// the projection is subscribed to, but does update things that are subscribed to it.
    /// </summary>
    /// <param name="position">Position to jump to.</param>
    public void Warp(Vector2 position)
    {
        if (Position != position)
        {
            Vector2 old = Position;
            Position = position;
            EmitSignal(SignalName.PointerMoved, old, Position);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (InputManager.Mode == InputMode.Mouse)
        {
            Warp(InputManager.LastKnownPointerPosition switch
            {
                Vector2 pos => (LevelManager.GetGlobalTransform()*LevelManager.GetCanvasTransform()).AffineInverse()*pos,
                _ => LevelManager.GetLocalMousePosition()
            });
        }
    }
}