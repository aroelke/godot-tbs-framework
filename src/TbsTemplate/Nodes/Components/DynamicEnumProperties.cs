using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;
using TbsTemplate.Extensions;

namespace TbsTemplate.Nodes.Components;

/// <summary>
/// Set of properties whose values are restricted to a set of options, but those values aren't determined at compile time. Allows access to
/// property values using a dictionary interface.
/// </summary>
/// <typeparam name="T">Type of data stored in the properties.</typeparam>
/// <param name="properties">List of data defining the properties.</param>
public class DynamicEnumProperties<[MustBeVariant] T>(params DynamicEnumProperties<T>.PropertyData[] properties) : IDictionary<StringName, T>
{
    /// <summary>Structure containing the name, value, and default value of a property.</summary>
    public class PropertyData
    {
        /// <summary>Read-only name of the property.</summary>
        public readonly StringName Name;
        /// <summary>Current value assigned to the property.  This is the only mutable field.</summary>
        public T Value;
        /// <summary>Default value of the property to use when not assigned or to revert to.</summary>
        public readonly T Default;

        /// <summary>Create a new property whose value is its default value.</summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="default">Initial and default value of the property.</param>
        public PropertyData(StringName name, T @default)
        {
            Name = name;
            Value = Default = @default;
        }
    }

    private readonly PropertyData[] _properties = properties;
    private readonly Dictionary<StringName, PropertyData> _dataDict = properties.ToDictionary((p) => p.Name, (p) => p);
    private Dictionary<StringName, T> ValueDict => _dataDict.ToDictionary((p) => p.Key, (p) => p.Value.Value);

    public ICollection<StringName> Keys => _dataDict.Keys;
    public ICollection<T> Values => (ICollection<T>)_properties.Select((p) => p.Value);
    public int Count => _properties.Length;
    public bool IsReadOnly => true;
    public T this[StringName key] { get => _dataDict[key].Value; set => throw new NotSupportedException(); }

    /// <summary>Convenience constructor for creating a set of properties that all have the same default value.</summary>
    /// <param name="names">Property names.</param>
    /// <param name="default">Default value of the properties.</param>
    public DynamicEnumProperties(IEnumerable<StringName> names, T @default=default) : this(names.Select((n) => new PropertyData(n, @default)).ToArray()) {}

    /// <summary>Create a property list that can be returned from <see cref="GodotObject._GetPropertyList"/> out of <see cref="_properties"/>.</summary>
    /// <param name="options">Allowed values for each of the properties.</param>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetPropertyList(IEnumerable<T> options) =>
        [.. _properties.Select((a) => ObjectProperty.CreateEnumProperty(a.Name, options).ToDictionary())];

    /// <summary>Get the value of a specific property.</summary>
    /// <param name="property">Name of the property.</param>
    /// <param name="value">Current value of the property, or <c>null</c> if there is no property with that name.</param>
    /// <returns><c>true</c> if there is a property with the given name, and <c>false</c> otherwise.</returns>
    public bool TryGetPropertyValue(StringName property, [MaybeNullWhen(false)] out T value)
    {
        if (_dataDict.TryGetValue(property, out PropertyData data))
        {
            value = data.Value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public T GetPropertyValue(StringName property) => _dataDict[property].Value;

    /// <summary>Set the value of a property.</summary>
    /// <param name="property">Name of the property.</param>
    /// <param name="value">New value for the property.</param>
    /// <returns><c>true</c> if there is a property with that name, and <c>false</c> otherwise.</returns>
    public bool SetPropertyValue(StringName property, T value)
    {
        if (_dataDict.TryGetValue(property, out PropertyData data))
        {
            data.Value = value;
            return true;
        }
        else
            return false;
    }

    /// <summary>Get the default value of a property that can be reverted to.</summary>
    /// <param name="property">Name of the property.</param>
    /// <param name="revert">Default value of the property, or <c>null</c> if there isn't one with that name.</param>
    /// <returns><c>true</c> if there is a property with that name, or <c>false</c> otherwise.</returns>
    public bool TryPropertyGetRevert(StringName property, out T revert)
    {
        if (_dataDict.TryGetValue(property, out PropertyData data))
        {
            revert = data.Default;
            return true;
        }
        else
        {
            revert = default;
            return false;
        }
    }

    /// <summary>Determine if a property can revert to its default value (for display in the editor).</summary>
    /// <param name="property">Name of the property.</param>
    /// <param name="revert"><c>true</c> if the property exists and can be reverted, and <c>false</c> otherwise.</param>
    /// <returns><c>true</c> if a property of that name exists, even if it can't be reverted, and <c>false</c> otherwise.</returns>
    public bool PropertyCanRevert(StringName property, out bool revert) => revert = _dataDict.ContainsKey(property);

    public bool TryGetValue(StringName key, [MaybeNullWhen(false)] out T value) => TryGetPropertyValue(key, out value);
    public bool ContainsKey(StringName key) => _dataDict.ContainsKey(key);
    public bool Contains(KeyValuePair<StringName, T> item) => _dataDict.ContainsKey(item.Key) && EqualityComparer<T>.Default.Equals(_dataDict[item.Key].Value, item.Value);
    public void CopyTo(KeyValuePair<StringName, T>[] array, int arrayIndex) => ((ICollection<KeyValuePair<StringName, T>>)ValueDict).CopyTo(array, arrayIndex);
    public IEnumerator<KeyValuePair<StringName, T>> GetEnumerator() => ValueDict.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(StringName key, T value) => throw new NotSupportedException();
    public bool Remove(StringName key) => throw new NotSupportedException();
    public void Add(KeyValuePair<StringName, T> item) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public bool Remove(KeyValuePair<StringName, T> item) => throw new NotSupportedException();
}