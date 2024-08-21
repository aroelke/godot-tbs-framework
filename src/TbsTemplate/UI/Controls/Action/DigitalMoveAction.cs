using System;
using Godot;

namespace TbsTemplate.UI.Controls.Action;

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

    /// <summary>Signals that a skip has been pressed.</summary>
    /// <param name="direction">Direction to skip in.</param>
    [Signal] public delegate void SkipEventHandler(Vector2I direction);

    private static bool IsActionPressed(StringName action) => Input.GetActionRawStrength(action) >= InputMap.ActionGetDeadzone(action);

    private double _remaining = 0;
    private Vector2I _previous = Vector2I.Zero;

    /// <summary>Initial delay after pressing a button to begin echoing the input.</summary>
    [Export] public double EchoDelay = 0.3;

    /// <summary>Delay between moves while holding an input down.</summary>
    [Export] public double EchoInterval = 0.03;

    /// <summary>Whether or not to use analog inputs for control (treated digitally) as well as digital ones.</summary>
    public bool EnableAnalog = false;

    /// <summary>Reset the echo timer so its next timeout is on the delay rather than the interval.</summary>
    public void ResetEcho() => _remaining = EchoDelay;

    public override void _EnterTree()
    {
        base._EnterTree();

        _remaining = 0;
        _previous = Vector2I.Zero;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Vector2I digital = new(
            Convert.ToInt32(Input.IsActionPressed(InputActions.DigitalMoveRight)) - Convert.ToInt32(Input.IsActionPressed(InputActions.DigitalMoveLeft)),
            Convert.ToInt32(Input.IsActionPressed(InputActions.DigitalMoveDown)) - Convert.ToInt32(Input.IsActionPressed(InputActions.DigitalMoveUp))
        );
        Vector2I analog = new(
            Convert.ToInt32(IsActionPressed(InputActions.AnalogMoveRight)) - Convert.ToInt32(IsActionPressed(InputActions.AnalogMoveLeft)),
            Convert.ToInt32(IsActionPressed(InputActions.AnalogMoveDown)) - Convert.ToInt32(IsActionPressed(InputActions.AnalogMoveUp))
        );
        Vector2I current = EnableAnalog ? (digital + analog).Clamp(-Vector2I.One, Vector2I.One) : digital;

        if (_previous != Vector2I.Zero && _previous == current)
        {
            _remaining -= delta;
            if (_remaining <= 0)
            {
                _remaining = EchoInterval;
                EmitSignal(SignalName.DirectionEchoed, _previous);
            }
        }
        else
        {
            _remaining = EchoDelay;
            Vector2I pressed = new(current.X != 0 && _previous.X == 0 ? current.X : 0, current.Y != 0 && _previous.Y == 0 ? current.Y : 0);
            if (pressed != Vector2I.Zero)
                EmitSignal(SignalName.DirectionPressed, pressed);
            Vector2I released = new(current.X != _previous.X && _previous.X != 0 ? _previous.X : 0, current.Y != _previous.Y && _previous.Y != 0 ? _previous.Y : 0);
            if (released != Vector2I.Zero)
                EmitSignal(SignalName.DirectionReleased, released);
        }
        _previous = current;
    }
}