using System;
using Godot;
using Level.Map;
using Object;
using UI.Controls.Action;
using UI.Controls.Device;

namespace Level.UI;

/// <summary>
/// Virtual mouse pointer that lives in the game world but has a projection onto the UI canvas and is controlled via analog stick.
/// Is only visible during analog control; during digital control, it and the main mouse become invisible in favor of the cursor,
/// and during mouse control, the system mouse is visible.
/// </summary>
[Tool]
public partial class Pointer : BoundedNode2D
{
    /// <summary>Signals that the virtual pointer has moved in the canvas.</summary>
    /// <param name="position">Position of the virtual pointer.</param>
    [Signal] public delegate void PointerMovedEventHandler(Vector2 position);

    private InputMode _prevMode = default;
    private Rect2I _bounds = new(0, 0, 0, 0);
    private bool _accelerate = false;
    private TextureRect _mouse = null;
    private CanvasItem _parent = null;

    private CanvasItem Parent => _parent ??= GetParent<CanvasItem>();

    /// <summary>Convert a position in the level to a position in the viewport.</summary>
    /// <param name="world">Level position.</param>
    /// <returns>Position in the viewport that's at the same place as the one in the level.</returns>
    private Vector2 WorldToViewport(Vector2 world) => Parent.GetGlobalTransformWithCanvas()*world;

    /// <summary>Convert a position in the viewport to a position in the level.</summary>
    /// <param name="viewport">Viewport position.</param>
    /// <returns>Position in the level that's at the same place as the one in the viewport.</returns>
    private Vector2 ViewportToWorld(Vector2 viewport) => Parent.GetGlobalTransformWithCanvas().AffineInverse()*viewport;

    /// <summary>Bounding rectangle where the cursor is allowed to move.</summary>
    [Export] public Rect2I Bounds
    {
        get => _bounds;
        set
        {
            if (_bounds != value)
            {
                _bounds = value;
            }
        }
    }

    /// <summary>Speed in screen pixels/second the pointer moves when in analog mode.</summary>
    [ExportGroup("Movement")]
    [Export] public double Speed = 1200;

    /// <summary>Multiplier applied to the pointer speed when the accelerate button is held down in analog mode.</summary>
    [ExportGroup("Movement")]
    [Export] public double Acceleration = 3;

    /// <summary>Action to move the pointer up.</summary>
    [ExportGroup("Input Actions/Movement")]
    [Export] public InputActionReference UpAction = new();

    /// <summary>Action to move the pointer left.</summary>
    [ExportGroup("Input Actions/Movement")]
    [Export] public InputActionReference LeftAction = new();

    /// <summary>Action to move the pointer down.</summary>
    [ExportGroup("Input Actions/Movement")]
    [Export] public InputActionReference DownAction = new();

    /// <summary>Action to move the pointer right.</summary>
    [ExportGroup("Input Actions/Movement")]
    [Export] public InputActionReference RightAction = new();

    /// <summary>Action to accelerate the speed of the cursor.</summary>
    [ExportGroup("Input Actions/Movement")]
    [Export] public InputActionReference AccelerateAction = new();

    /// <summary>The pointer is just a point, but it has to have a zero area so the camera can focus on it.</summary>
    public override Vector2 Size { get => Vector2.Zero; set {}}

    /// <summary>Move the cursor to a new location that's not bounded by <c>Bounds</c>, and update listeners that the move occurred.</summary>
    /// <param name="position">Position to warp to.</param>
    public void Warp(Vector2 position)
    {
        if (Position != position)
        {
            Position = position;
            EmitSignal(SignalName.PointerMoved, Position);
        }
    }

    /// <summary>
    /// Move the cursor to a new location that's not bounded by <c>Bounds</c>, and update listeners that the move occurred. If input is controlled by the mouse,
    /// this will also warp the mouse to the pointer's position, moving the camera if necessary (the camera moves instantly with no smoothing in this case).
    /// </summary>
    /// <param name="position">Position to warp to.</param>
    public void WarpMouse(Vector2 position)
    {
        if (DeviceManager.Mode != InputMode.Mouse)
            GD.PushWarning("Warping the mouse when not controlling via mouse.");

        if (Position != position)
        {
            Warp(position);
        }
    }

    /// <summary>When the input mode changes, update visibility and move things around to make sure real/virtual mouse positions are consistent.</summary>
    /// <param name="mode">New input mode.</param>
    public void OnInputModeChanged(InputMode mode)
    {
        switch (mode)
        {
        case InputMode.Mouse:
            _mouse.Visible = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
            Input.WarpMouse(GetViewport().GetScreenTransform()*WorldToViewport(Position));
            break;
        case InputMode.Analog:
            _mouse.Visible = true;
            Input.MouseMode = Input.MouseModeEnum.Hidden;
            if (_prevMode == InputMode.Mouse)
                Warp(ViewportToWorld(InputManager.GetMousePosition()));
            break;
        }
        _prevMode = mode;
    }

    /// <summary>When the mouse enters the screen, warp to its entry position.</summary>
    /// <param name="position">Position the mouse entered the screen on.</param>
    public void OnMouseEntered(Vector2 position) => Warp(ViewportToWorld(position));

    /// <summary>When the mouse exits the screen, warp to edge of the screen near where it exited.</summary>
    /// <param name="position">Position on screen close to where the mouse exited.</param>
    public void OnMouseExited(Vector2 position) => Warp(ViewportToWorld(position));

    /// <summary>When the cursor moves during digital input, warp to its location.</summary>
    /// <param name="position">New cursor position.</param>
    public void OnCursorMoved(Vector2 position)
    {
        if (DeviceManager.Mode == InputMode.Digital)
        {
            _mouse.Visible = false;
            Input.MouseMode = Input.MouseModeEnum.Hidden;
            Warp(position);
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            _mouse = GetNode<TextureRect>("Canvas/Mouse");
            _mouse.Texture = ResourceLoader.Load<Texture2D>(ProjectSettings.GetSetting("display/mouse_cursor/custom_image").As<string>());
            _mouse.Position = WorldToViewport(Position);
            _mouse.Visible = DeviceManager.Mode == InputMode.Analog;

            _prevMode = DeviceManager.Mode;
            Input.MouseMode = DeviceManager.Mode == InputMode.Mouse ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Hidden;

            DeviceManager.Singleton.InputModeChanged += OnInputModeChanged;
            InputManager.Singleton.MouseEntered += OnMouseEntered;
            InputManager.Singleton.MouseExited += OnMouseExited;

            GetViewport().SizeChanged += () => _mouse.Scale = (GetViewport().GetScreenTransform() with { Origin = Vector2.Zero }).AffineInverse()*Vector2.One;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (DeviceManager.Mode == InputMode.Analog)
        {
            if (@event.IsActionPressed(AccelerateAction))
                _accelerate = true;
            if (@event.IsActionReleased(AccelerateAction))
                _accelerate = false;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            switch (DeviceManager.Mode)
            {
            case InputMode.Mouse when Position != ViewportToWorld(InputManager.GetMousePosition()):
                Warp(ViewportToWorld(InputManager.GetMousePosition()));
                break;
            case InputMode.Analog:
                Vector2 direction = Input.GetVector(LeftAction, RightAction, UpAction, DownAction);
                if (direction != Vector2.Zero)
                {
                    double speed = _accelerate ? (Speed*Acceleration) : Speed;
                    Warp((Position + direction*(float)(speed*delta)).Clamp(Bounds.Position, Bounds.End));
                }
                break;
            }
            _mouse.Position = WorldToViewport(Position);
        }
    }
}