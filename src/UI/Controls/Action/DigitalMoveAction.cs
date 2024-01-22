using Godot;
using UI.Controls.Device;

namespace UI.Controls.Action;

/// <summary>Object component that enables the object to be controlled digitally (e.g. with keyboard keys or gamepad buttons).</summary>
public partial class DigitalMoveAction : Node
{
    /// <summary>Signals that a new direction has been pressed.</summary>
    /// <param name="direction">Direction that was pressed.</param>
    [Signal] public delegate void DirectionPressedEventHandler(Vector2I direction);

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
            Vector2I dir = InputManager.GetDigitalVector();
            if (dir != _direction)
            {
                EchoTimer.Stop();
                _echoing = false;

                if (dir != Vector2I.Zero)
                {
                    if (dir.Abs().X + dir.Abs().Y > _direction.Abs().X + _direction.Abs().Y)
                        EmitSignal(SignalName.DirectionPressed, dir - _direction);
                    else
                        EmitSignal(SignalName.DirectionPressed, dir);
                    _direction = dir;

                    EchoTimer.WaitTime = EchoDelay;
                    EchoTimer.Start();
                }
                else
                    _direction = Vector2I.Zero;
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