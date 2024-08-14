using System;
using Godot;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI.Controls.Action;

/// <summary>Object component that enables the object to be controlled digitally (e.g. with keyboard keys or gamepad buttons).</summary>
[SceneTree]
public partial class DigitalMoveAction : Node
{
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

    private static Vector2I ActionVector(Predicate<StringName> pressed) => new(
        Convert.ToInt32(pressed(InputActions.DigitalMoveRight)) - Convert.ToInt32(pressed(InputActions.DigitalMoveLeft)),
        Convert.ToInt32(pressed(InputActions.DigitalMoveDown)) - Convert.ToInt32(pressed(InputActions.DigitalMoveUp))
    );

    private Vector2I _direction = Vector2I.Zero;
    private bool _process = false;
    private bool _echoing = false;
    private bool _reset = false;
    private bool _skip = false;

    private bool IsEchoing() => !_skip && _direction != Vector2I.Zero;

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
            EmitSignal(SignalName.DirectionEchoed, _direction);
            if (_process)
                _echoing = true;
            else
                EchoTimer.Start(EchoInterval);
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        _direction = ActionVector(static (n) => Input.IsActionPressed(n));
        if (_direction != Vector2I.Zero)
        {
            Callable.From<Vector2I>((d) => {
                EmitSignal(SignalName.DirectionPressed, d);
                EchoTimer.Start(EchoInterval);
            }).CallDeferred(_direction);
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        EchoTimer.Stop();
        _direction = Vector2I.Zero;
    }

    public override void _Ready()
    {
        base._Ready();
        _process = EchoInterval < 1.0/Engine.PhysicsTicksPerSecond;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionPressed(InputActions.Accelerate) && !IsEchoing())
            _skip = true;
        else if (@event.IsActionReleased(InputActions.Accelerate))
            _skip = false;

        Vector2I prev = _direction;

        Vector2I pressed = ActionVector((n) => @event.IsActionPressed(n));
        Vector2I released = ActionVector((n) => @event.IsActionReleased(n));
        _direction += pressed - released;

        if (_skip)
        {
            if (pressed != Vector2I.Zero && _direction != Vector2I.Zero && !@event.IsEcho())
                EmitSignal(SignalName.Skip, _direction);
        }
        else
        {
            if (pressed != Vector2I.Zero)
                EmitSignal(SignalName.DirectionPressed, pressed);
            if (released != Vector2I.Zero)
                EmitSignal(SignalName.DirectionReleased, released);

            if (prev != _direction)
            {
                EchoTimer.Stop();
                _echoing = false;

                if (_direction != Vector2I.Zero)
                    EchoTimer.Start(EchoDelay);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (DeviceManager.Mode == InputMode.Digital && _echoing)
            EmitSignal(SignalName.DirectionEchoed, _direction);
    }
}