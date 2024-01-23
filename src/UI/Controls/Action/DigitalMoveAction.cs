using System;
using Godot;
using UI.Controls.Device;

namespace UI.Controls.Action;

/// <summary>Object component that enables the object to be controlled digitally (e.g. with keyboard keys or gamepad buttons).</summary>
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

    private Vector2I _direction = Vector2I.Zero;
    private Timer _timer = null;
    private bool _echoing = false;

    private Timer EchoTimer => _timer = GetNode<Timer>("EchoTimer");

    /// <summary>Move up action.</summary>
    [ExportGroup("Input Actions")]
    [Export] public InputActionReference UpAction = new();

    /// <summary>Move left action.</summary>
    [ExportGroup("Input Actions")]
    [Export] public InputActionReference LeftAction = new();

    /// <summary>Move down action.</summary>
    [ExportGroup("Input Actions")]
    [Export] public InputActionReference DownAction = new();

    /// <summary>Move right action.</summary>
    [ExportGroup("Input Actions")]
    [Export] public InputActionReference RightAction = new();

    /// <summary>Initial delay after pressing a button to begin echoing the input.</summary>
    [ExportGroup("Echo Control")]
    [Export] public double EchoDelay = 0.3;

    /// <summary>Delay between moves while holding an input down.</summary>
    [ExportGroup("Echo Control")]
    [Export] public double EchoInterval = 0.03;

    /// <summary>Start/continue echo movement.</summary>
    public void OnEchoTimeout()
    {
        EmitSignal(SignalName.DirectionEchoed, _direction);
        if (EchoInterval > GetProcessDeltaTime())
        {
            EchoTimer.WaitTime = EchoInterval;
            EchoTimer.Start();
        }
        else
            _echoing = true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (DeviceManager.Mode == InputMode.Digital)
        {
            Vector2I prev = _direction;

            Vector2I pressed = new(
                Convert.ToInt32(@event.IsActionPressed(RightAction)) - Convert.ToInt32(@event.IsActionPressed(LeftAction)),
                Convert.ToInt32(@event.IsActionPressed(DownAction)) - Convert.ToInt32(@event.IsActionPressed(UpAction))
            );
            Vector2I released = new(
                Convert.ToInt32(@event.IsActionReleased(RightAction)) - Convert.ToInt32(@event.IsActionReleased(LeftAction)),
                Convert.ToInt32(@event.IsActionReleased(DownAction)) - Convert.ToInt32(@event.IsActionReleased(UpAction))
            );
            _direction += pressed - released;

            if (pressed != Vector2I.Zero)
                EmitSignal(SignalName.DirectionPressed, pressed);
            if (released != Vector2I.Zero)
                EmitSignal(SignalName.DirectionReleased, released);

            if (prev != _direction)
            {
                EchoTimer.Stop();
                _echoing = false;

                if (_direction != Vector2I.Zero)
                {
                    EchoTimer.WaitTime = EchoDelay;
                    EchoTimer.Start();
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (DeviceManager.Mode == InputMode.Digital && _echoing)
            EmitSignal(SignalName.DirectionEchoed, _direction);
    }
}