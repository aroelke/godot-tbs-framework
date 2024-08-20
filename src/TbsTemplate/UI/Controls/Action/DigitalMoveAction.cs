using System;
using System.Collections.Generic;
using System.Linq;
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

    private static Vector2I ActionVector(Dictionary<StringName, bool> actions) => new(
        Convert.ToInt32(actions[InputActions.DigitalMoveRight]) - Convert.ToInt32(actions[InputActions.DigitalMoveLeft]),
        Convert.ToInt32(actions[InputActions.DigitalMoveDown]) - Convert.ToInt32(actions[InputActions.DigitalMoveUp])
    );

    private Dictionary<StringName, bool> _held = new() {
        { InputActions.DigitalMoveUp,    false },
        { InputActions.DigitalMoveLeft,  false },
        { InputActions.DigitalMoveDown,  false },
        { InputActions.DigitalMoveRight, false }
    };
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

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (@event.IsActionPressed(InputActions.Accelerate) && !IsEchoing())
            _skip = true;
        else if (@event.IsActionReleased(InputActions.Accelerate))
            _skip = false;

        Dictionary<StringName, bool> pressed = new() {
            { InputActions.DigitalMoveUp,    @event.IsActionPressed(InputActions.DigitalMoveUp) },
            { InputActions.DigitalMoveLeft,  @event.IsActionPressed(InputActions.DigitalMoveLeft) },
            { InputActions.DigitalMoveDown,  @event.IsActionPressed(InputActions.DigitalMoveDown) },
            { InputActions.DigitalMoveRight, @event.IsActionPressed(InputActions.DigitalMoveRight) }
        };
        Dictionary<StringName, bool> released = new() {
            { InputActions.DigitalMoveUp,    @event.IsActionReleased(InputActions.DigitalMoveUp) },
            { InputActions.DigitalMoveLeft,  @event.IsActionReleased(InputActions.DigitalMoveLeft) },
            { InputActions.DigitalMoveDown,  @event.IsActionReleased(InputActions.DigitalMoveDown) },
            { InputActions.DigitalMoveRight, @event.IsActionReleased(InputActions.DigitalMoveRight) }
        };

        if (pressed.Values.Any((v) => v) || released.Values.Any((v) => v))
        {
            foreach (StringName action in _held.Keys)
                _held[action] = (_held[action] || pressed[action]) && !released[action];
            _direction = ActionVector(_held);

            if (_skip)
            {
                if (_direction != Vector2I.Zero && !@event.IsEcho())
                    EmitSignal(SignalName.Skip, _direction);
            }
            else
            {
                if (pressed.Values.Any((v) => v))
                    EmitSignal(SignalName.DirectionPressed, ActionVector(pressed));
                if (released.Values.Any((v) => v))
                    EmitSignal(SignalName.DirectionReleased, ActionVector(released));

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