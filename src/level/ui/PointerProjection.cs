using Godot;
using level.manager;
using ui.input;

namespace level.ui;

/// <summary>Projection of the pointer onto the map, for controlling the the camera and converting world/viewport positions.</summary>
public partial class PointerProjection : Node2D, ILevelManaged
{
    /// <summary>Signals that the pointer projection has moved on the map.</summary>
    /// <param name="viewport">Current position on the viewport.</param>
    /// <param name="world">Current position on the map.</param>
    [Signal] public delegate void PointerMovedEventHandler(Vector2 viewport, Vector2 world);

    /// <summary>Signals that the pointer has been clicked.</summary>
    /// <param name="viewport">Position in the viewport of the click.</param>
    /// <param name="world">Position on the map of the click.</param>
    [Signal] public delegate void PointerClickedEventHandler(Vector2 viewport, Vector2 world);

    private InputManager _inputManager = null;
    private Camera2D _camera = null;
    private LevelManager _levelManager = null;
    private Vector2 _viewportPosition = Vector2.Zero;

    private InputManager InputManager => _inputManager ??= GetNode<InputManager>(InputManager.NodePath);
    private Camera2D Camera => _camera ??= GetNode<Camera2D>("Camera");

    /// <summary>
    /// Move the pointer projection to a new position. This does not affect the actual mouse pointer or any virtual pointers
    /// the projection is subscribed to, but does update things that are subscribed to it.
    /// </summary>
    /// <param name="position">Position to jump to.</param>
    private void Warp(Vector2 position)
    {
        if (Position != position)
        {
            Position = position;
            EmitSignal(SignalName.PointerMoved, WorldToViewport(Position), Position);
        }
    }

    /// <summary>Position of the pointer projection in the viewport.</summary>
    public Vector2 ViewportPosition
    {
        get => LevelManager.GetGlobalTransform()*LevelManager.GetCanvasTransform()*Position;
        set => Warp(ViewportToWorld(value));
    }

    /// <summary>Position of the pointer projection in the level.</summary>
    public Vector2 LevelPosition
    {
        get => Position;
        set => Warp(value);
    }

    /// <summary>Convert a position in the viewport to a position in the level.</summary>
    /// <param name="viewport">Viewport position.</param>
    /// <returns>Position in the level that's at the same place as the one in the viewport.</returns>
    public Vector2 ViewportToWorld(Vector2 viewport) => (LevelManager.GetGlobalTransform()*LevelManager.GetCanvasTransform()).AffineInverse()*viewport;

    /// <summary>Convert a position in the level to a position in the viewport.</summary>
    /// <param name="world">Level position.</param>
    /// <returns>Position in the viewport that's at the same place as the one in the level.</returns>
    public Vector2 WorldToViewport(Vector2 world) => LevelManager.GetGlobalTransform()*LevelManager.GetCanvasTransform()*world;

    /// <summary>Only smooth the camera when the cursor is controlled by the mouse.</summary>
    /// <param name="mode">Current input mode.</param>
    public void OnInputModeChanged(InputMode mode) => Camera.PositionSmoothingEnabled = mode != InputMode.Digital;

    /// <summary>When the cursor moves during digital control, move the projection to the center of the cell.</summary>
    /// <param name="position">Position of the center of the cell to jump to.</param>
    public void OnCursorMoved(Vector2 position)
    {
        if (InputManager.Mode == InputMode.Digital)
            Warp(position);
    }

    /// <summary>When the virtual pointer moves using analog input, move to its location projected onto the map.</summary>
    /// <param name="viewport">Current location of the virtual pointer on the viewport.</param>
    public void OnPointerMoved(Vector2 viewport)
    {
        _viewportPosition = viewport;
        if (InputManager.Mode != InputMode.Digital)
            Warp(ViewportToWorld(_viewportPosition));
    }

    /// <summary>When the viewport pointer is clicked, emit a signal converting it to a world position.</summary>
    /// <param name="viewport">Position in the viewport of the click.</param>
    public void OnPointerClicked(Vector2 viewport) => EmitSignal(SignalName.PointerClicked, viewport, ViewportToWorld(viewport));

    public LevelManager LevelManager => _levelManager ??= GetParent<LevelManager>();

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
                Vector2 pos => ViewportToWorld(pos),
                null => LevelManager.GetLocalMousePosition()
            });
            break;
        case InputMode.Analog:
            Warp(ViewportToWorld(_viewportPosition));
            break;
        }
    }
}