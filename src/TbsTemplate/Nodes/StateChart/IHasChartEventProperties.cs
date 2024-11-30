using System;
using System.Linq;
using Godot;
using TbsTemplate.Extensions;

namespace TbsTemplate.Nodes.StateChart;

/// <summary>
/// Trait to mix into a <see cref="GodotObject"/> to give it methods to create, get, set, etc. properties whose values are based on
/// the events defined for a <see cref="Chart"/>.
/// </summary>
public interface IHasChartEventProperties
{
    /// <summary>Structure defining the name, a getter/setter, and revert value of a <see cref="Chart"/> event property.</summary>
    public class ChartEventProperty
    {
        /// <summary>Name of the property.</summary>
        public StringName Name = "";

        /// <summary>Getter of the property value.</summary>
        public Func<StringName> Get = null;

        /// <summary>Setter of the property value.</summary>
        public Action<StringName> Set = null;

        /// <summary>Revert value of the property.</summary>
        public StringName Default = "";

        /// <summary>Create a new property to store the name of a <see cref="Chart"/> event.</summary>
        /// <param name="this">Object containing the property.</param>
        /// <param name="name">Name of the property.</param>
        /// <param name="getter">Getter of the property value. Leave default to use reflection based on <paramref name="name"/>.</param>
        /// <param name="setter">Setter of the property value. Leave default to use reflection based on <paramref name="name"/></param>
        /// <param name="default">Revert value of the property.</param>
        public ChartEventProperty(object @this, StringName name, Func<StringName> getter=null, Action<StringName> setter=null, StringName @default=null)
        {
            System.Reflection.PropertyInfo info = @this.GetType().GetProperty(Name);

            Name = name;
            Get = getter ?? (() => info.GetValue(@this) as StringName);
            Set = setter ?? ((StringName v) => info.SetValue(@this, v));
            Default = @default ?? "";
        }
    }

    /// <summary>List of properties that contain <see cref="Chart"/> event names.</summary>
    public ChartEventProperty[] EventProperties { get; }

    /// <summary>State chart defining the events to store in properties.</summary>
    public Chart StateChart { get; }

    /// <returns>
    /// A property list containing all the properties in <see cref="EventProperties"/> that can be used in
    /// <see cref="GodotObject._GetPropertyList"/>.
    /// </returns>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetChartEventProperties()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = [];

        properties.Add(new ObjectProperty(
            "State Chart Events",
            Variant.Type.Nil,
            Usage:PropertyUsageFlags.Group
        ));
        properties.AddRange(EventProperties.Select((p) => StateChart.CreateEventProperty(p.Name).ToDictionary()));

        return properties;
    }

    /// <summary>Get the event stored in a <see cref="Chart"/> event property.</summary>
    /// <param name="property">Name of the event property.</param>
    /// <param name="value">Event stored in the property.</param>
    /// <returns><c>true</c> if <paramref name="property"/> is a valid event property, and <c>false</c> otherwise.</returns>
    public bool GetChartEventPropertyValue(StringName property, out StringName value)
    {
        foreach (ChartEventProperty p in EventProperties)
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

    /// <summary>Set the <see cref="Chart"/> event stored in an event property.</summary>
    /// <param name="property">Name of the property.</param>
    /// <param name="value">New event to store in the property.</param>
    /// <returns><c>true</c> if <paramref name="property"/> is a valid event property, and <c>false</c> otherwise.</returns>
    public bool SetChartEventPropertyValue(StringName property, StringName value)
    {
        foreach (ChartEventProperty p in EventProperties)
        {
            if (p.Name == property)
            {
                p.Set(value);
                return true;
            }
        }
        return false;
    }

    /// <summary>Determine if a property can revert to its default value.  All event properties can revert.</summary>
    /// <param name="property">Name of the property.</param>
    /// <param name="revert">Whether or not the property can revert. Only false if <paramref name="property"/> is not a valid property.</param>
    /// <returns><c>true</c> if <paramref name="property"/> is a valid event property, and <c>false</c> otherwise.</returns>
    public bool ChartEventPropertyCanRevert(StringName property, out bool revert)
    {
        revert = false;
        foreach (ChartEventProperty p in EventProperties)
            if (p.Name == property)
                revert = true;
        return revert;
    }

    /// <summary>Get the revert value of a <see cref="Chart"/> event property.</summary>
    /// <param name="property">Name of the property.</param>
    /// <param name="revert">
    /// Returns the revert value of the property if <paramref name="property"/> is a valid property,
    /// and <c>null</c> if it isn't.
    /// </param>
    /// <returns><c>true</c> if <paramref name="revert"/> is not null, and <c>false</c> otherwise.</returns>
    public bool ChartEventPropertyGetRevert(StringName property, out StringName revert)
    {
        foreach (ChartEventProperty p in EventProperties)
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