using System;
using System.Linq;
using Godot;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.Nodes.Components;

/// <summary>Trait to mix into a <see cref="GodotObject"/> that gives it methods to create, get, set, etc. properties that contain input action names.</summary>
public interface IHasInputActionProperties
{
    private class PropertyException(Type type, StringName property) : ArgumentException($"{type} does not have an input action property named {property}") {}

    /// <summary>Structure containing the name, value, and default value of an input action property.</summary>
    public class InputActionProperty
    {
        /// <summary>Input action properties can be passed directly into Godot's input action functions instead of accessing their <see cref="Value"/> fields.</summary>
        public static implicit operator StringName(InputActionProperty property) => property.Value;

        /// <summary>Read-only name of the property.</summary>
        public readonly StringName Name;
        /// <summary>Current input action assigned to the property.  This is the only mutable field.</summary>
        public StringName Value;
        /// <summary>Default value of the property to use when not assigned or to revert to.</summary>
        public readonly StringName Default;

        /// <summary>Create a new input action property whose value is its default value.</summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="default">Initial and default value of the property.</param>
        public InputActionProperty(StringName name, StringName @default)
        {
            Name = name;
            Value = Default = @default;
        }
    }

    /// <summary>Comma-separated list of input actions.</summary>
    public static string InputActionList => string.Join(",", InputManager.GetInputActions().Select(static (i) => i.ToString()));

    /// <summary>Create an input action property with the given name. It will present a dropdown menu with all of the defined input actions (even built-in ones).</summary>
    /// <param name="name">Name of the property.</param>
    public static Godot.Collections.Dictionary CreateInputActionProperty(StringName name) => new()
    {
        { "name", name },
        { "type", Variant.From(Variant.Type.StringName) },
        { "hint", Variant.From(PropertyHint.Enum) },
        { "hint_string", InputActionList }
    };

    /// <summary>List of input action properties to provide to the <see cref="GodotObject"/>.</summary>
    public InputActionProperty[] InputActions { get; }

    /// <summary>Create a property list that can be returned from <see cref="GodotObject._GetPropertyList"/> out of <see cref="InputActions"/>.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetInputActionProperties() => new(InputActions.Select((a) => CreateInputActionProperty(a.Name)));

    /// <summary>Get the value of a specific input action property.</summary>
    /// <param name="property">Name of the property.</param>
    /// <param name="value">Current value of the property, or <c>null</c> if there is no property with that name.</param>
    /// <returns><c>true</c> if there is a property with the given name, and <c>false</c> otherwise.</returns>
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

    /// <summary>Set the value of an input action property.</summary>
    /// <param name="property">Name of the property.</param>
    /// <param name="value">New value for the property.</param>
    /// <returns><c>true</c> if there is a property with that name, and <c>false</c> otherwise.</returns>
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

    /// <summary>Get the default value of an input action property that can be reverted to.</summary>
    /// <param name="property">Name of the property.</param>
    /// <param name="revert">Default value of the property, or <c>null</c> if there isn't one with that name.</param>
    /// <returns><c>true</c> if there is a property with that name, or <c>false</c> otherwise.</returns>
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

    /// <summary>Determine if a property can revert to its default value (for display in the editor).</summary>
    /// <param name="property">Name of the property.</param>
    /// <param name="revert"><c>true</c> if the property exists and can be reverted, and <c>false</c> otherwise.</param>
    /// <returns><c>true</c> if a property of that name exists, even if it can't be reverted, and <c>false</c> otherwise.</returns>
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