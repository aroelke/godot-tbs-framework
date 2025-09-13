using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI.Controls.Action;

/// <summary>
/// Converts an analog action into a different, equivalent digital one (like movement on an axis to a dpad press). The digital
/// action is considered to be pressed when the analog action exits its dead zone, and released when it re-enters it.
/// </summary>
[Tool]
public partial class AnalogDigitalConverter : Node
{
    /// <summary>Signals that the digital action has been pressed when the analog one exits its dead zone.</summary>
    /// <param name="event"><see cref="InputEvent"/> representing the digital action press.</param>
    [Signal] public delegate void ActionPressedEventHandler(InputEvent @event);

    /// <summary>Signals that the digital action has been released when the analog one enters its dead zone.</summary>
    /// <param name="event"><see cref="InputEvent"/> representing the digital action release.</param>
    [Signal] public delegate void ActionReleasedEventHandler(InputEvent @event);

    private bool active = false;
    private StringName AnalogAction = "";
    private StringName DigitalAction = "";

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = base._GetPropertyList() ?? [];
        properties.Add(ObjectProperty.CreateEnumProperty(PropertyName.AnalogAction, InputManager.GetInputActions()));
        properties.Add(ObjectProperty.CreateEnumProperty(PropertyName.DigitalAction, InputManager.GetInputActions()));
        return properties;
    }

    public override Variant _Get(StringName property)
    {
        if (property == PropertyName.AnalogAction)
            return AnalogAction;
        else if (property == PropertyName.DigitalAction)
            return DigitalAction;
        else
            return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (property == PropertyName.AnalogAction)
        {
            AnalogAction = value.AsStringName();
            return true;
        }
        else if (property == PropertyName.DigitalAction)
        {
            DigitalAction = value.AsStringName();
            return true;
        }
        else
            return base._Set(property, value);
    }

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (property == PropertyName.AnalogAction ||  property == PropertyName.DigitalAction)
            return "";
        else
            return base._PropertyGetRevert(property);
    }

    public override bool _PropertyCanRevert(StringName property) => property == PropertyName.AnalogAction || property == PropertyName.DigitalAction || base._PropertyCanRevert(property);

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            float str = Input.GetActionRawStrength(AnalogAction);

            if (str >= InputMap.ActionGetDeadzone(AnalogAction) && !active)
            {
                active = true;
                EmitSignal(SignalName.ActionPressed, new InputEventAction() { Action = DigitalAction, Pressed = true, Strength = 1 });
            }
            else if (str < InputMap.ActionGetDeadzone(AnalogAction) && active)
            {
                active = false;
                EmitSignal(SignalName.ActionReleased, new InputEventAction() { Action = DigitalAction, Pressed = false });
            }
        }
    }
}