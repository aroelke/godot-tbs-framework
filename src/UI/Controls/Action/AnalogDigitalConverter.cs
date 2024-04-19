using Godot;

namespace UI.Controls.Action;

/// <summary>
/// Converts an analog action into a different, equivalent digital one (like movement on an axis to a dpad press). The digital
/// action is considered to be pressed when the analog action exits its dead zone, and released when it re-enters it.
/// </summary>
public partial class AnalogDigitalConverter : Node
{
    /// <summary>Signals that the digital action has been pressed when the analog one exits its dead zone.</summary>
    /// <param name="event"><see cref="InputEvent"/> representing the digital action press.</param>
    [Signal] public delegate void ActionPressedEventHandler(InputEvent @event);

    /// <summary>Signals that the digital action has been released when the analog one enters its dead zone.</summary>
    /// <param name="event"><see cref="InputEvent"/> representing the digital action release.</param>
    [Signal] public delegate void ActionReleasedEventHandler(InputEvent @event);

    private bool active = false;

    /// <summary>Analog action to convert to digital.</summary>
    [Export] public InputActionReference AnalogAction = new();

    /// <summary>Digital action that results from converting the analog one.</summary>
    [Export] public InputActionReference DigitalAction = new();

    public override void _Process(double delta)
    {
        base._Process(delta);

        float str = Input.GetActionRawStrength(AnalogAction);

        if (str >= AnalogAction.Deadzone && !active)
        {
            active = true;
            EmitSignal(SignalName.ActionPressed, new InputEventAction() { Action = DigitalAction, Pressed = true, Strength = 1 });
        }
        else if (str < AnalogAction.Deadzone && active)
        {
            active = false;
            EmitSignal(SignalName.ActionReleased, new InputEventAction() { Action = DigitalAction, Pressed = false });
        }
    }
}