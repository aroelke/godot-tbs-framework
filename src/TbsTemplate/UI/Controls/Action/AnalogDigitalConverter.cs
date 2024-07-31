using Godot;
using TbsTemplate.Nodes.Components;

namespace TbsTemplate.UI.Controls.Action;

/// <summary>
/// Converts an analog action into a different, equivalent digital one (like movement on an axis to a dpad press). The digital
/// action is considered to be pressed when the analog action exits its dead zone, and released when it re-enters it.
/// </summary>
[Tool]
public partial class AnalogDigitalConverter : Node, IHasInputActionProperties
{
    /// <summary>Signals that the digital action has been pressed when the analog one exits its dead zone.</summary>
    /// <param name="event"><see cref="InputEvent"/> representing the digital action press.</param>
    [Signal] public delegate void ActionPressedEventHandler(InputEvent @event);

    /// <summary>Signals that the digital action has been released when the analog one enters its dead zone.</summary>
    /// <param name="event"><see cref="InputEvent"/> representing the digital action release.</param>
    [Signal] public delegate void ActionReleasedEventHandler(InputEvent @event);

    private IHasInputActionProperties.InputActionProperty AnalogAction = new("AnalogAction", "");
    private IHasInputActionProperties.InputActionProperty DigitalAction = new("DigitalAction", "");
    private bool active = false;

    public IHasInputActionProperties.InputActionProperty[] InputActions => [AnalogAction, DigitalAction];

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = base._GetPropertyList() ?? [];
        properties.AddRange(((IHasInputActionProperties)this).GetInputActionProperties());
        return properties;
    }

    public override Variant _Get(StringName property)
    {
        if (((IHasInputActionProperties)this).GetInputActionPropertyValue(property, out StringName value))
            return value;
        return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (value.VariantType == Variant.Type.StringName && ((IHasInputActionProperties)this).SetInputActionPropertyValue(property, value.AsStringName()))
            return true;
        return base._Set(property, value);
    }

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (((IHasInputActionProperties)this).InputActionPropertyGetRevert(property, out StringName revert))
            return revert;
        return base._PropertyGetRevert(property);
    }

    public override bool _PropertyCanRevert(StringName property)
    {
        if (((IHasInputActionProperties)this).InputActionPropertyCanRevert(property, out bool revert))
            return revert;
        return base._PropertyCanRevert(property);
    }

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
            else if (str < InputMap.ActionGetDeadzone(AnalogAction.Value) && active)
            {
                active = false;
                EmitSignal(SignalName.ActionReleased, new InputEventAction() { Action = DigitalAction, Pressed = false });
            }
        }
    }
}