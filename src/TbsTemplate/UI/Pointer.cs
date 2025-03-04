using System;
using System.Collections.Generic;
using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes;
using TbsTemplate.Nodes.Components;
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

    private static readonly StringName WaitEvent = "Wait";
    private static readonly StringName DoneEvent = "Done";
    private static readonly StringName ModeProperty = "mode";

    private readonly DynamicEnumProperties<StringName> _events = new([WaitEvent, DoneEvent], @default:"");
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

    /// <summary>Move the pointer to a new location that's not bounded by <see cref="Bounds"/>, and update listeners that the move occurred.</summary>
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
    /// <param name="target">Location to move to.</param>
    /// <param name="duration">Time to take to move there in seconds.</param>
    public void Fly(Vector2 target, double duration)
    {
        Mouse.Visible = true;
        DeviceManager.EnableSystemMouse = false;
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
            ControlState.SendEvent(_events[DoneEvent]);
            EmitSignal(SignalName.PointerMoved, Position);
            EmitSignal(SignalName.FlightCompleted);
        };

        ControlState.SendEvent(_events[WaitEvent]);
    }

    /// <summary>Disable input and wait for an event to complete.</summary>
    /// <param name="hide">Whether or not to hide the mouse while waiting.</param>
    public void StartWaiting(bool hide=true)
    {
        DeviceManager.EnableSystemMouse = !hide;
        ControlState.SendEvent(_events[WaitEvent]);
        Mouse.Visible = false;
    }

    /// <summary>Re-enable input.</summary>
    public void StopWaiting() => ControlState.SendEvent(_events[DoneEvent]);

    /// <summary>Update the state whenever input mode changes.</summary>
    /// <param name="mode">New input mode.</param>
    public void OnInputModeChanged(InputMode mode) => ControlState.ExpressionProperties = ControlState.ExpressionProperties.SetItem(ModeProperty, Enum.GetName(mode));

    /// <summary>When entering an active state, enable the system mouse during mouse control.</summary>
    public void OnActiveEntered() => DeviceManager.EnableSystemMouse = true;

    /// <summary>When entering digital state, hide the virtual pointer, as the pointer is not used for control in that state.</summary>
    public void OnDigitalStateEntered() => Mouse.Visible = false;

    /// <summary>When changing input mode from mouse to analog, warp the pointer to where the mouse is.</summary>
    public void OnMouseToAnalogTaken() => Warp(ViewportToWorld(InputManager.GetMousePosition()));

    /// <summary>When entering analog state, make sure the virtual pointer is visible (unless analog input should be treated as digital).</summary>
    public void OnAnalogStateEntered() => Mouse.Visible = _tracking;

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

    /// <summary>When transitioning to the mouse state from other control states (not waiting ones), warp the mouse to the pointer's position.</summary>
    public void OnToMouseStateTaken() => GetViewport().WarpMouse(WorldToViewport(Position));

    /// <summary>
    /// When changing to mouse input, move the mouse to the pointer's location to ensure overall motion is contiguous and make the virtual pointer
    /// invisible and system pointer visible.
    /// </summary>
    public void OnMouseStateEntered()
    {
        if (ViewportPosition != GetViewport().GetMousePosition())
            ViewportPosition = GetViewport().GetMousePosition();
        Mouse.Visible = false;
    }

    /// <summary>During mouse control, move to the mouse position every step.</summary>
    public void OnMouseStateProcess(double delta)
    {
        if (Position != ViewportToWorld(InputManager.GetMousePosition()))
        {
            ControlState.SendEvent(_events[DoneEvent]);
            Warp(ViewportToWorld(InputManager.GetMousePosition()));
        }
    }

    /// <summary>During mouse or analog control, signal that the cursor has stopped when it stops.</summary>
    public void OnNotDigitalStatePhysicsProcess(double delta)
    {
        if (!Engine.IsEditorHint())
        {
            Vector2 velocity = Position - _positions[0];
            Vector2 acceleration = velocity - (_positions[0] - _positions[1]);

            if (velocity.IsZeroApprox() && !acceleration.IsZeroApprox())
                Callable.From<Vector2>((p) => EmitSignal(SignalName.PointerStopped, p)).CallDeferred(Position);

            _positions[1] = _positions[0];
            _positions[0] = Position;
        }
    }

    /// <summary>When done waiting, kill the tween controlling it in case the pointer is flying and flight is ended before it has completed movement.</summary>
    public void OnWaitingExited()
    {
        if (_flyer.IsValid())
            _flyer.Kill();
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
            if (Waiting.Active && DeviceManager.Mode == InputMode.Mouse)
            {
                Warp(region.GetCenter());
                GetViewport().WarpMouse(WorldToViewport(region.GetCenter()));
            }
            else if (MouseState.Active)
                Fly(region.GetCenter(), DefaultFlightTime);
            else
                Warp(region.GetCenter());
        }
    }

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = base._GetPropertyList() ?? [];
        properties.AddRange(_events.GetPropertyList(ControlState.Events));
        return properties;
    }

    public override Variant _Get(StringName property)
    {
        if (_events.TryGetPropertyValue(property, out StringName value))
            return value;
        else
            return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (value.VariantType == Variant.Type.StringName && _events.SetPropertyValue(property, value.AsStringName()))
            return true;
        else
            return base._Set(property, value);
    }

    public override bool _PropertyCanRevert(StringName property)
    {
        if (_events.PropertyCanRevert(property, out bool revert))
            return revert;
        else
            return base._PropertyCanRevert(property);
    }

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (_events.TryPropertyGetRevert(property, out StringName revert))
            return revert;
        else
            return base._PropertyGetRevert(property);
    }

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        if (property["name"].As<StringName>() == PropertyName.Size)
            property["usage"] = property["usage"].As<uint>() | (uint)PropertyUsageFlags.ReadOnly;
        base._ValidateProperty(property);
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (World is null)
            warnings.Add("The pointer won't be able to convert screen and world coordinates without knowing what the world is.");

        if (_events[WaitEvent].IsEmpty)
            warnings.Add("The \"wait\" state chart event is not set. The pointer will not respond to input.");
        if (_events[DoneEvent].IsEmpty)
            warnings.Add("The \"done\" state chart event is not set. The pointer cannot be paused.");

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
}