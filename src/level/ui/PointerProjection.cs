using Godot;
using level.manager;
using ui.input;

namespace level.ui;

/// <summary>Projection of the pointer (virtual or real) onto the map, for controlling the cursor and the camera.</summary>
public partial class PointerProjection : Node2D, ILevelManaged
{
    /// <summary>Signals that the pointer projection has moved on the map.</summary>
    /// <param name="previous">Previous position on the map.</param>
    /// <param name="current">Current position on the map.</param>
    [Signal] public delegate void PointerMovedEventHandler(Vector2 previous, Vector2 current);

    private InputManager _inputManager = null;
    private Camera2D _camera = null;
    private LevelManager _levelManager = null;
    private Vector2 _virtualPosition = Vector2.Zero;

    private InputManager InputManager => _inputManager ??= GetNode<InputManager>("/root/InputManager");
    private Camera2D Camera => _camera ??= GetNode<Camera2D>("Camera");

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

    /// <summary>When the cursor moves during digital control, move the projection to the center of the cell.</summary>
    /// <param name="cell">Cell to jump to.</param>
    public void OnCursorMoved(Vector2I cell)
    {
        if (InputManager.Mode == InputMode.Digital)
            Warp(LevelManager.PositionOf(cell) + LevelManager.CellSize/2);
    }

    /// <summary>Only smooth the camera when the cursor is controlled by the mouse.</summary>
    /// <param name="previous">Previous input mode.</param>
    /// <param name="current">Current input mode.</param>
    public void OnInputModeChanged(InputMode previous, InputMode current) => Camera.PositionSmoothingEnabled = current != InputMode.Digital;

    /// <summary>When the virtual pointer moves, move to its location projected onto the map.</summary>
    /// <param name="previous">Previous location of the virtual pointer on the viewport.</param>
    /// <param name="current">Next location of the virtual pointer on the viewport.</param>
    public void OnVirtualPointerMoved(Vector2 previous, Vector2 current) => _virtualPosition = current;

    public override void _Ready()
    {
        base._Ready();

        (Camera.LimitTop, Camera.LimitLeft) = Vector2I.Zero;
        (Camera.LimitRight, Camera.LimitBottom) = (Vector2I)(LevelManager.GridSize*LevelManager.CellSize);
        InputManager.InputModeChanged += OnInputModeChanged;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        switch (InputManager.Mode)
        {
        case InputMode.Mouse:
            Warp(InputManager.LastKnownPointerPosition switch
            {
                Vector2 pos => (LevelManager.GetGlobalTransform()*LevelManager.GetCanvasTransform()).AffineInverse()*pos,
                _ => LevelManager.GetLocalMousePosition()
            });
            break;
        case InputMode.Analog:
            Warp(InputManager.GetGlobalTransformWithCanvas().AffineInverse()*_virtualPosition);
            break;
        }

        if (InputManager.Mode == InputMode.Mouse)
        {
            Warp(InputManager.LastKnownPointerPosition switch
            {
                Vector2 pos => (LevelManager.GetGlobalTransform()*LevelManager.GetCanvasTransform()).AffineInverse()*pos,
                _ => LevelManager.GetLocalMousePosition()
            });
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        InputManager.InputModeChanged -= OnInputModeChanged;
    }
}