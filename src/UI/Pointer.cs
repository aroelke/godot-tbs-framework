using System;
using System.Collections.Generic;
using Extensions;
using Godot;
using Nodes;
using Nodes.StateChart;
using Nodes.StateChart.States;
using UI.Controls.Action;
using UI.Controls.Device;

namespace UI;

/// <summary>
/// Virtual mouse pointer that lives in the game world but has a projection onto the UI canvas and is controlled via analog stick.
/// Is only visible during analog control; during digital control, it and the main mouse become invisible in favor of a
/// cursor; during mouse control the system mouse is visible.
/// </summary>
[Tool]
public partial class Pointer : BoundedNode2D
{
    private readonly NodeCache _cache;
    public Pointer() : base() => _cache = new(this);

    /// <summary>Signals that the virtual pointer has moved in the canvas.</summary>
    /// <param name="position">Position of the virtual pointer.</param>
    [Signal] public delegate void PointerMovedEventHandler(Vector2 position);

    [Signal] public delegate void FlightCompletedEventHandler();

    private static readonly StringName ModeProperty = "mode";
    private static readonly StringName FlyEvent = "fly";
    private static readonly StringName WaitEvent = "wait";
    private static readonly StringName DoneEvent = "done";

    private bool _accelerate = false;
    private bool _tracking = true;
    private Tween _flyer = null;

    private Chart ControlState => _cache.GetNode<Chart>("ControlState");
    private TextureRect Mouse => _cache.GetNode<TextureRect>("Canvas/Mouse");
    private State DigitalState => _cache.GetNodeOrNull<State>("%Digital");
    private State AnalogState => _cache.GetNodeOrNull<State>("%Analog");
    private State MouseState => _cache.GetNodeOrNull<State>("%Mouse");

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
            ControlState.SendEvent(DoneEvent);
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

    public void StartWaiting() => ControlState.SendEvent(WaitEvent);

    public void StopWaiting() => ControlState.SendEvent(DoneEvent);

    public void OnInputModeChanged(InputMode mode) => ControlState.ExpressionProperties = ControlState.ExpressionProperties.SetItem(ModeProperty, Enum.GetName(mode));

    public void OnDigitalStateEntered()
    {
        Mouse.Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    public void OnMouseToAnalogTaken() => Warp(ViewportToWorld(InputManager.GetMousePosition()));

    public void OnAnalogStateEntered()
    {
        Mouse.Visible = _tracking;
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    public void OnAnalogStateUnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(AccelerateAction))
            _accelerate = true;
        if (@event.IsActionReleased(AccelerateAction))
            _accelerate = false;
    }

    public void OnAnalogStateProcess(double delta)
    {
        if (_tracking)
        {
            Vector2 direction = Input.GetVector(LeftAction, RightAction, UpAction, DownAction);
            if (direction != Vector2.Zero)
            {
                double speed = _accelerate ? (Speed*Acceleration) : Speed;
                Warp((Position + direction*(float)(speed*delta)).Clamp(Bounds.Position, Bounds.End));
            }
        }
    }

    public void OnTransitionToMouseTaken() => GetViewport().WarpMouse(WorldToViewport(Position));

    public void OnMouseStateEntered()
    {
        GetViewport().WarpMouse(WorldToViewport(Position));
        Mouse.Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public void OnMouseStateProcess(double delta)
    {
        if (Position != ViewportToWorld(InputManager.GetMousePosition()))
            Warp(ViewportToWorld(InputManager.GetMousePosition()));
    }

    public void OnFlyingEntered()
    {
        Mouse.Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    public void OnFlyingExited()
    {
        if (_flyer.IsValid())
            _flyer.Kill();
    }

    public void OnWaitingEntered()
    {
        Mouse.Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

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
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        if (World is null)
            warnings.Add("The pointer won't be able to convert screen and world coordinates without knowing what the world is.");

        return warnings.ToArray();
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
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
            Mouse.Position = WorldToViewport(Position);
    }
}