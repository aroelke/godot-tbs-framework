using System;
using System.Collections.Generic;
using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes;
using TbsTemplate.UI.Controls.Action;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI;

/// <summary>
/// Virtual mouse pointer that lives in the game world but has a projection onto the UI canvas and is controlled via analog stick.
/// Is only visible during analog control; during digital control, it and the main mouse become invisible in favor of a
/// cursor; during mouse control the system mouse is visible.
/// </summary>
[SceneTree, Tool]
public partial class Pointer : BoundedNode2D
{
    /// <summary>Signals that the virtual pointer has moved in the canvas.</summary>
    /// <param name="position">Position of the virtual pointer.</param>
    [Signal] public delegate void PointerMovedEventHandler(Vector2 position);

    /// <summary>Signals that the pointer has stopped moving on a point.</summary>
    /// <param name="position">Position of the pointer when it stopped moving.</param>
    [Signal] public delegate void PointerStoppedEventHandler(Vector2 position);

    /// <summary>Signals that the pointer has begun moving to a new position over time. During flight, the cursor doesn't respond to input.</summary>
    /// <param name="target">Position the cursor is moving to.</param>
    [Signal] public delegate void FlightStartedEventHandler(Vector2 target);

    /// <summary>Signals that the pointer has finished moving to its target position.</summary>
    [Signal] public delegate void FlightCompletedEventHandler();

    private static readonly StringName ModeProperty = "mode";
    private static readonly StringName FlyEvent = "fly";
    private static readonly StringName WaitEvent = "wait";
    private static readonly StringName DoneEvent = "done";

    private Vector2[] _positions = [];
    private bool _accelerate = false;
    private bool _tracking = true;
    private Tween _flyer = null;

    /// <summary>Convert a position in the <see cref="World"/> to a position in the <see cref="Viewport"/>.</summary>
    /// <param name="world"><see cref="World"/> position.</param>
    /// <returns>Position in the <see cref="Viewport"/> that's at the same place as the one in the <see cref="World"/>.</returns>
    private Vector2 WorldToViewport(Vector2 world) => World.GetGlobalTransformWithCanvas()*world;

    /// <summary>Convert a position in the <see cref="Viewport"/> to a position in the <see cref="World"/>.</summary>
    /// <param name="viewport"><see cref="Viewport"/> position.</param>
    /// <returns>Position in the <see cref="World"/> that's at the same place as the one in the <see cref="Viewport"/>.</returns>
    private Vector2 ViewportToWorld(Vector2 viewport) => World.GetGlobalTransformWithCanvas().AffineInverse()*viewport;

    /// <summary>Bounding rectangle where the pointer is allowed to move.</summary>
    [Export] public Rect2I Bounds = new(0, 0, 0, 0);

    /// <summary>Scene containing the pointer and things it can point to.</summary>
    [Export] public CanvasItem World = null;

    /// <summary>Speed in screen pixels/second the pointer moves when in analog mode.</summary>
    [ExportGroup("Movement")]
    [Export(PropertyHint.None, "suffix:px/s")] public double Speed = 1200;

    /// <summary>Multiplier applied to the pointer speed when the accelerate button is held down in analog mode.</summary>
    [ExportGroup("Movement")]
    [Export] public double Acceleration = 3;

    /// <summary>Default time to warp during mouse control.</summary>
    [ExportGroup("Movement")]
    [Export(PropertyHint.None, "suffix:s")] public double DefaultFlightTime = 0.25;

    /// <summary>Position of the pointer relative to the <see cref="Viewport"/>.</summary>
    public Vector2 ViewportPosition
    {
        get => Engine.IsEditorHint() ? GlobalPosition : WorldToViewport(GlobalPosition);
        set => GlobalPosition = Engine.IsEditorHint() ? value : ViewportToWorld(value);
    }

    /// <summary>Whether or not the pointer should be visible and move during analog control.</summary>
    public bool AnalogTracking
    {
        get => _tracking;
        set
        {
            _tracking = value;
            if (AnalogState?.Active ?? false)
                Mouse.Visible = _tracking;
        }
    }

    /// <inheritdoc cref="BoundedNode2D.Size"/>
    /// <remarks>The pointer is just a point, but it has to have a zero area so the camera can focus on it.</remarks>
    public override Vector2 Size { get => Vector2.Zero; set {}}

    /// <summary>
    /// Move the pointer to a new location that's not bounded by <see cref="Bounds"/>, and update listeners that the move occurred. If the pointer is currently
    /// on its way to a new position due to <see cref="Fly"/>, cancel the movement.
    /// </summary>
    /// <param name="position">Position to warp to.</param>
    public void Warp(Vector2 position)
    {
        if (Position != position)
        {
            Position = position;
            EmitSignal(SignalName.PointerMoved, Position);
        }
    }

    /// <summary>Move the pointer to a new position over time. Update listeners that the move ocurred afterward.</summary>
    /// <param name="target"></param>
    /// <param name="duration"></param>
    public void Fly(Vector2 target, double duration)
    {
        ControlState.SendEvent(FlyEvent);
        EmitSignal(SignalName.FlightStarted, target);

        if (_flyer.IsValid())
            _flyer.Kill();
        _flyer = CreateTween();

        _flyer.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out).TweenMethod(
            Callable.From((Vector2 position) => {
                Position = position;
                GetViewport().WarpMouse(ViewportPosition);
            }),
            Position,
            target,
            duration
        ).Finished += () => {
            ControlState.SendEvent(DoneEvent);
            EmitSignal(SignalName.PointerMoved, Position);
            EmitSignal(SignalName.FlightCompleted);
        };
    }

    /// <summary>Disable input and wait for an event to complete.</summary>
    /// <param name="hide">Whether or not to hide the mouse while waiting.</param>
    public void StartWaiting(bool hide=true)
    {
        if (hide)
            Input.MouseMode = Input.MouseModeEnum.Hidden;
        ControlState.SendEvent(WaitEvent);
    }

    /// <summary>Re-enable input.</summary>
    public void StopWaiting() => ControlState.SendEvent(DoneEvent);

    /// <summary>Update the state whenever input mode changes.</summary>
    /// <param name="mode">New input mode.</param>
    public void OnInputModeChanged(InputMode mode) => ControlState.ExpressionProperties = ControlState.ExpressionProperties.SetItem(ModeProperty, Enum.GetName(mode));

    /// <summary>When entering digital state, hide both the virtual and system pointers, as the pointer is not used for control in that state.</summary>
    public void OnDigitalStateEntered()
    {
        Mouse.Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    /// <summary>When changing input mode from mouse to analog, warp the pointer to where the mouse is.</summary>
    public void OnMouseToAnalogTaken() => Warp(ViewportToWorld(InputManager.GetMousePosition()));

    /// <summary>When entering analog state, make sure the virtual pointer is visible (unless analog input should be treated as digital) and the system pointer is invisible.</summary>
    public void OnAnalogStateEntered()
    {
        Mouse.Visible = _tracking;
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    /// <summary>While the accelerate button is held down, move the pointer faster.</summary>
    /// <param name="event">Input event describing the input.</param>
    public void OnAnalogStateUnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputActions.Accelerate))
            _accelerate = true;
        if (@event.IsActionReleased(InputActions.Accelerate))
            _accelerate = false;
    }

    /// <summary>During analog input state, move the pointer according to the movement axis.</summary>
    /// <param name="delta">Time since the last process step.</param>
    public void OnAnalogStatePhysicsProcess(double delta)
    {
        if (_tracking)
        {
            Vector2 direction = Input.GetVector(InputActions.AnalogMoveLeft, InputActions.AnalogMoveRight, InputActions.AnalogMoveUp, InputActions.AnalogMoveDown);
            if (direction != Vector2.Zero)
            {
                double speed = _accelerate ? (Speed*Acceleration) : Speed;
                Warp((Position + direction*(float)(speed*delta)).Clamp(Bounds.Position, Bounds.End));
            }
        }
    }

    /// <summary>
    /// When changing to mouse input, move the mouse to the pointer's location to ensure overall motion is contiguous and make the virtual pointer
    /// invisible and system pointer visible.
    /// </summary>
    public void OnMouseStateEntered()
    {
        GetViewport().WarpMouse(WorldToViewport(Position));
        Mouse.Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    /// <summary>During mouse control, move to the mouse position every step.</summary>
    /// <param name="delta">Time since last process step.</param>
    public void OnMouseStateProcess(double delta)
    {
        if (Position != ViewportToWorld(InputManager.GetMousePosition()))
        {
            ControlState.SendEvent(DoneEvent);
            Warp(ViewportToWorld(InputManager.GetMousePosition()));
        }
    }

    /// <summary>
    /// When starting to fly to a location, make the virtual pointer visible and system mouse invisible so it doesn't jitter if moved while flying. Re-entering
    /// the appropriate state after flying is complete will automatically update the pointers to the correct visibilities.
    /// </summary>
    public void OnFlyingEntered()
    {
        Mouse.Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    /// <summary>When done flying, kill the tween controlling it in case flight is ended before it has completed movement.</summary>
    public void OnFlyingExited()
    {
        if (_flyer.IsValid())
            _flyer.Kill();
    }

    /// <summary>
    /// When entering the waiting state, hide the mouse for the duration of the wait. Its visibility will be restored automatically by the state it
    /// returns to.
    /// </summary>
    public void OnWaitingEntered() => Mouse.Visible = false;

    /// <summary>When the mouse enters the <see cref="Viewport"/>, warp to its entry position.</summary>
    /// <param name="position">Position the mouse entered the <see cref="Viewport"/> on.</param>
    public void OnMouseEntered(Vector2 position) => Warp(ViewportToWorld(position));

    /// <summary>When the mouse exits the <see cref="Viewport"/> , warp to edge of the <see cref="Viewport"/> near where it exited.</summary>
    /// <param name="position">Position on <see cref="Viewport"/> close to where the mouse exited.</param>
    public void OnMouseExited(Vector2 position) => Warp(ViewportToWorld(position));

    /// <summary>When the cursor moves during digital input, warp to its location.</summary>
    /// <param name="region">New region enclosed by the cursor.</param>
    public void OnCursorMoved(Rect2 region)
    {
        if (!region.Contains(Position, perimeter:true))
        {
            if (MouseState.Active)
                Fly(region.GetCenter(), DefaultFlightTime);
            else
                Warp(region.GetCenter());
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (World is null)
            warnings.Add("The pointer won't be able to convert screen and world coordinates without knowing what the world is.");

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            _positions = [Position, Position];

            ControlState.ExpressionProperties = ControlState.ExpressionProperties.SetItem(ModeProperty, Enum.GetName(DeviceManager.Mode));

            _flyer = CreateTween();
            _flyer.Kill();

            Mouse.Texture = ResourceLoader.Load<Texture2D>(ProjectSettings.GetSetting("display/mouse_cursor/custom_image").As<string>());
            Callable.From(() => Mouse.Position = WorldToViewport(Position)).CallDeferred();
            OnInputModeChanged(DeviceManager.Mode);

            GetViewport().SizeChanged += () => Mouse.Scale = (GetViewport().GetScreenTransform() with { Origin = Vector2.Zero }).AffineInverse()*Vector2.One;
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        if (!Engine.IsEditorHint())
        {
            DeviceManager.Singleton.InputModeChanged += OnInputModeChanged;
            InputManager.Singleton.MouseEntered += OnMouseEntered;
            InputManager.Singleton.MouseExited += OnMouseExited;
        }
    }

    public override void _ExitTree()
    {
        if (!Engine.IsEditorHint())
        {
            DeviceManager.Singleton.InputModeChanged -= OnInputModeChanged;
            InputManager.Singleton.MouseEntered -= OnMouseEntered;
            InputManager.Singleton.MouseExited -= OnMouseExited;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            Mouse.Position = WorldToViewport(Position);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (!Engine.IsEditorHint())
        {
            Vector2 velocity = Position - _positions[0];
            Vector2 acceleration = velocity - (_positions[0] - _positions[1]);

            if (velocity.IsZeroApprox() && !acceleration.IsZeroApprox())
                EmitSignal(SignalName.PointerStopped, Position);

            _positions[1] = _positions[0];
            _positions[0] = Position;
        }
    }
}