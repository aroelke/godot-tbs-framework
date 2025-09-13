using Godot;
using TbsTemplate.Nodes.Components;
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

    private static readonly StringName AnalogAction = "Analog Action";
    private static readonly StringName DigitalAction = "Digital Action";

    private readonly DynamicEnumProperties<StringName> _actions = new([AnalogAction, DigitalAction], @default:"");
    private bool active = false;

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = base._GetPropertyList() ?? [];
        properties.AddRange(_actions.GetPropertyList(InputManager.GetInputActions()));
        return properties;
    }

    public override Variant _Get(StringName property)
    {
        if (_actions.TryGetPropertyValue(property, out StringName value))
            return value;
        else
            return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (value.VariantType == Variant.Type.StringName && _actions.SetPropertyValue(property, value.AsStringName()))
            return true;
        else
            return base._Set(property, value);
    }

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (_actions.TryPropertyGetRevert(property, out StringName revert))
            return revert;
        else
            return base._PropertyGetRevert(property);
    }

    public override bool _PropertyCanRevert(StringName property)
    {
        if (_actions.PropertyCanRevert(property, out bool revert))
            return revert;
        else
            return base._PropertyCanRevert(property);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            float str = Input.GetActionRawStrength(_actions[AnalogAction]);

            if (str >= InputMap.ActionGetDeadzone(_actions[AnalogAction]) && !active)
            {
                active = true;
                EmitSignal(SignalName.ActionPressed, new InputEventAction() { Action = _actions[DigitalAction], Pressed = true, Strength = 1 });
            }
            else if (str < InputMap.ActionGetDeadzone(_actions[AnalogAction]) && active)
            {
                active = false;
                EmitSignal(SignalName.ActionReleased, new InputEventAction() { Action = _actions[DigitalAction], Pressed = false });
            }
        }
    }
}