using System;
using System.Linq;
using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.Nodes.Components;

public class InputActionPropertyGenerator(params InputActionPropertyGenerator.InputActionProperty[] properties)
{
    public class InputActionProperty
    {
        /// <summary>Name of the property.</summary>
        public StringName Name = "";

        /// <summary>Getter of the property value.</summary>
        public Func<StringName> Get = null;

        /// <summary>Setter of the property value.</summary>
        public Action<StringName> Set = null;

        /// <summary>Revert value of the property.</summary>
        public StringName Default = "";

        public InputActionProperty(object @this, StringName name, Func<StringName> getter=null, Action<StringName> setter=null, StringName @default=null)
        {
            System.Reflection.PropertyInfo info = @this.GetType().GetProperty(Name);

            Name = name;
            Get = getter ?? (() => info.GetValue(@this) as StringName);
            Set = setter ?? ((StringName v) => info.SetValue(@this, v));
            Default = @default ?? "";
        }
    }

    /// <summary>Comma-separated list of input actions.</summary>
    public static string InputActionList => string.Join(",", InputManager.GetInputActions().Select(static (i) => i.ToString()));

    /// <summary>Create an input action property with the given name. It will present a dropdown menu with all of the defined input actions (even built-in ones).</summary>
    /// <param name="name">Name of the property.</param>
    public static Godot.Collections.Dictionary CreateInputActionProperty(StringName name) =>  new ObjectProperty(
        name,
        Variant.Type.StringName,
        PropertyHint.Enum,
        InputActionList
    );

    public InputActionPropertyGenerator(object @this, params StringName[] properties)
        : this(properties.Select((p) => new InputActionProperty(@this, p)).ToArray()) {}

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetInputActionProperties()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> list = [];

        list.Add(new ObjectProperty(
            "Input Action Events",
            Variant.Type.Nil,
            Usage:PropertyUsageFlags.Group
        ));
        list.AddRange(properties.Select((p) => CreateInputActionProperty(p.Name)));

        return list;
    }

    public bool GetInputActionPropertyValue(StringName property, out StringName value)
    {
        foreach (InputActionProperty p in properties)
        {
            if (p.Name == property)
            {
                value = p.Get();
                return true;
            }
        }
        value = null;
        return false;
    }

    public bool SetInputActionPropertyValue(StringName property, StringName value)
    {
        foreach (InputActionProperty p in properties)
        {
            if (p.Name == property)
            {
                p.Set(value);
                return true;
            }
        }
        return false;
    }

    public bool InputActionPropertyCanRevert(StringName property, out bool revert)
    {
        revert = false;
        foreach (InputActionProperty p in properties)
            if (p.Name == property)
                revert = true;
        return revert;
    }

    public bool InputActionPropertyGetRevert(StringName property, out StringName revert)
    {
        foreach (InputActionProperty p in properties)
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
}