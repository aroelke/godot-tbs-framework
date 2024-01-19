using System.Linq;
using Godot;
using UI.Device;

namespace UI.Action;

/// <summary>Provides a single place to define the name of an action and get its associated controls.</summary>
[GlobalClass, Tool]
public partial class InputActionReference : Resource
{
    /// <summary>Name of the property containing the input action.</summary>
    public static readonly StringName InputActionProperty = "Input Action";

    /// <summary>Default value of the input action property.</summary>
    public static readonly StringName InputActionDefault = "";

    /// <summary><c>InputActionReference</c>s can be used as though they were the string name of the action they represent.</summary>
    public static implicit operator StringName(InputActionReference reference) => reference.InputAction;

    /// <summary>Property containing the input action name.</summary>
    public StringName InputAction = InputActionDefault;

    /// <summary>Convenience property allowing access to any mouse button that has been mapped to the action.</summary>
    public MouseButton MouseButton => InputManager.GetInputMouseButton(InputAction);

    /// <summary>Convenience property allowing access to any keyboard key that has been mapped to the action.</summary>
    public Key Key => InputManager.GetInputKeycode(InputAction);

    /// <summary>Convenience property allowing access to any gamepad button that has been mapped to the action.</summary>
    public JoyButton GamepadButton => InputManager.GetInputGamepadButton(InputAction);

    /// <summary>Convenience property allowing access to any gamepad axis that has been mapped to the action.</summary>
    public JoyAxis GamepadAxis => InputManager.GetInputGamepadAxis(InputAction);

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        return new()
        {
            new()
            {
                { "name", "InputActionReference" },
                { "type", Variant.From(Variant.Type.Nil) },
                { "usage", Variant.From(PropertyUsageFlags.Category) }
            },
            new()
            {
                { "name", InputActionProperty },
                { "type", Variant.From(Variant.Type.StringName) },
                { "hint", Variant.From(PropertyHint.Enum) },
                { "hint_string", string.Join(",", InputManager.GetInputActions().Select((i) => i.ToString())) }
            }
        };
    }

    public override Variant _Get(StringName property) => property == InputActionProperty ? InputAction : base._Get(property);

    public override bool _Set(StringName property, Variant value)
    {
        if (property == InputActionProperty)
        {
            InputAction = value.As<StringName>();
            return true;
        }
        else
            return base._Set(property, value);
    }

    public override bool _PropertyCanRevert(StringName property) => (property == InputActionProperty && InputAction != InputActionDefault) || base._PropertyCanRevert(property);

    public override Variant _PropertyGetRevert(StringName property) => property == InputActionProperty ? InputActionDefault : base._PropertyGetRevert(property);
}