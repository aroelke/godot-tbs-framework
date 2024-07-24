using System;
using System.Linq;
using Godot;
using UI.Controls.Device;

namespace Nodes.Components;

public interface IHasInputActionProperties
{
    private class PropertyException(Type type, StringName property) : ArgumentException($"{type} does not have an input action property named {property}") {}

    public class InputActionProperty
    {
        public static implicit operator StringName(InputActionProperty property) => property.Value;

        public readonly StringName Name;
        public StringName Value;
        public readonly StringName Default;

        public InputActionProperty(StringName name, StringName @default)
        {
            Name = name;
            Value = Default = @default;
        }
    }

    public InputActionProperty[] InputActions { get; }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetInputActionProperties() => new(InputActions.Select((a) => new Godot.Collections.Dictionary()
    {
        { "name", a.Name },
        { "type", Variant.From(Variant.Type.StringName) },
        { "hint", Variant.From(PropertyHint.Enum) },
        { "hint_string", string.Join(",", InputManager.GetInputActions().Select(static (i) => i.ToString())) }
    }));

    public bool GetInputActionPropertyValue(StringName property, out StringName value)
    {
        foreach (InputActionProperty p in InputActions)
        {
            if (p.Name == property)
            {
                value = p.Value;
                return true;
            }
        }
        value = null;
        return false;
    }

    public bool SetInputActionPropertyValue(StringName property, StringName value)
    {
        for (int i = 0; i < InputActions.Length; i++)
        {
            if (InputActions[i].Name == property)
            {
                InputActions[i].Value = value;
                return true;
            }
        }
        return false;
    }

    public bool InputActionPropertyGetRevert(StringName property, out StringName revert)
    {
        foreach (InputActionProperty p in InputActions)
        {
            if (p.Name == property)
            {
                revert = p.Default;
                return true;
            }
        }
        revert = null;
        return false;
    }

    public bool InputActionPropertyCanRevert(StringName property, out bool revert)
    {
        foreach (InputActionProperty data in InputActions)
        {
            if (data.Name == property)
            {
                revert = data.Value != data.Default;
                return true;
            }
        }
        revert = false;
        return false;
    }
}