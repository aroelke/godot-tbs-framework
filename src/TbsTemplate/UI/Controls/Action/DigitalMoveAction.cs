using System;
using Godot;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI.Controls.Action;

/// <summary>Object component that enables the object to be controlled digitally (e.g. with keyboard keys or gamepad buttons).</summary>
[SceneTree]
public partial class DigitalMoveAction : Node
{
    private readonly record struct DirectionBits(bool Up=false, bool Left=false, bool Down=false, bool Right=false)
    {
        public static DirectionBits operator~(DirectionBits a) => new(!a.Up, !a.Left, !a.Down, !a.Right);
        public static DirectionBits operator|(DirectionBits a, DirectionBits b) => new(a.Up || b.Up, a.Left || b.Left, a.Down || b.Down, a.Right || b.Right);
        public static DirectionBits operator&(DirectionBits a, DirectionBits b) => new(a.Up && b.Up, a.Left && b.Left, a.Down && b.Down, a.Right && b.Right);

        public static DirectionBits FromInputEvent(InputEvent @event, bool pressed=true) => new(
            pressed ? @event.IsActionPressed(InputActions.DigitalMoveUp)    : @event.IsActionReleased(InputActions.DigitalMoveUp),
            pressed ? @event.IsActionPressed(InputActions.DigitalMoveLeft)  : @event.IsActionReleased(InputActions.DigitalMoveLeft),
            pressed ? @event.IsActionPressed(InputActions.DigitalMoveDown)  : @event.IsActionReleased(InputActions.DigitalMoveDown),
            pressed ? @event.IsActionPressed(InputActions.DigitalMoveRight) : @event.IsActionReleased(InputActions.DigitalMoveRight)
        );

        public static DirectionBits FromInput(Func<StringName, bool, bool> @event, bool exactMatch=false) => new(
            @event(InputActions.DigitalMoveUp, exactMatch),
            @event(InputActions.DigitalMoveLeft, exactMatch),
            @event(InputActions.DigitalMoveDown, exactMatch),
            @event(InputActions.DigitalMoveRight, exactMatch)
        );

        public bool Any() => Up || Left || Down || Right;

        public Vector2I GetVector() => new(Convert.ToInt32(Right) - Convert.ToInt32(Left), Convert.ToInt32(Down) - Convert.ToInt32(Up));
    }

    /// <summary>Signals that a new direction has been pressed.</summary>
    /// <param name="direction">Direction that was pressed.</param>
    [Signal] public delegate void DirectionPressedEventHandler(Vector2I direction);

    /// <summary>Signals that a direction has been released.</summary>
    /// <param name="direction">Direction that was released.</param>
    [Signal] public delegate void DirectionReleasedEventHandler(Vector2I direction);

    /// <summary>Signals that a direction is being echoed.</summary>
    /// <param name="direction">Direction that was echoed.</param>
    [Signal] public delegate void DirectionEchoedEventHandler(Vector2I direction);

    /// <summary>Signals that a skip has been pressed.</summary>
    /// <param name="direction">Direction to skip in.</param>
    [Signal] public delegate void SkipEventHandler(Vector2I direction);

    private DirectionBits _held = new();
    private bool _process = false;
    private bool _echoing = false;
    private bool _reset = false;
    private bool _skip = false;

    private bool IsEchoing() => !_skip && _held.Any();

    /// <summary>Initial delay after pressing a button to begin echoing the input.</summary>
    [Export] public double EchoDelay = 0.3;

    /// <summary>Delay between moves while holding an input down.</summary>
    [Export] public double EchoInterval = 0.03;

    /// <summary>Reset the echo timer so its next timeout is on the delay rather than the interval.</summary>
    public void ResetEcho()
    {
        if (IsEchoing())
        {
            _echoing = false;
            EchoTimer.Start(EchoDelay);
            _reset = true;
        }
    }

    /// <summary>Start/continue echo movement.</summary>
    public void OnEchoTimeout()
    {
        if (_reset)
        {
            EchoTimer.Start(EchoDelay);
            _reset = false;
        }
        else
        {
            EmitSignal(SignalName.DirectionEchoed, _held.GetVector());
            if (_process)
                _echoing = true;
            else
                EchoTimer.Start(EchoInterval);
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        _held = DirectionBits.FromInput(Input.IsActionPressed);
        if (_held.Any())
        {
            Callable.From<Vector2I>((d) => {
                EmitSignal(SignalName.DirectionPressed, d);
                EchoTimer.Start(EchoInterval);
            }).CallDeferred(_held.GetVector());
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        EchoTimer.Stop();
        _held = new();
    }

    public override void _Ready()
    {
        base._Ready();
        _process = EchoInterval < 1.0/Engine.PhysicsTicksPerSecond;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (@event.IsActionPressed(InputActions.Accelerate) && !IsEchoing())
            _skip = true;
        else if (@event.IsActionReleased(InputActions.Accelerate))
            _skip = false;

        DirectionBits pressed = DirectionBits.FromInputEvent(@event, true);
        DirectionBits released = DirectionBits.FromInputEvent(@event, false);

        if (pressed.Any() || released.Any())
        {
            _held = (_held | pressed) & ~released;

            if (_skip)
            {
                if (_held.Any() && !@event.IsEcho())
                    EmitSignal(SignalName.Skip, _held.GetVector());
            }
            else
            {
                if (pressed.Any())
                    EmitSignal(SignalName.DirectionPressed, pressed.GetVector());
                if (released.Any())
                    EmitSignal(SignalName.DirectionReleased, released.GetVector());

                EchoTimer.Stop();
                _echoing = false;
                if (_held.Any())
                    EchoTimer.Start(EchoDelay);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (DeviceManager.Mode == InputMode.Digital && _echoing)
            EmitSignal(SignalName.DirectionEchoed, _held.GetVector());
    }
}