using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Godot.Collections;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI.Controls.IconMaps;

/// <summary>
/// Icon map comprised of one or more other maps that are mapped to <c>string</c> names that correspond to input device names. Directly accessing icons (by action or input) selects
/// from the current input device.
/// </summary>
/// <typeparam name="T">Type of the input used for icons.</typeparam>
/// <typeparam name="M">Type of the constituent maps containing icons.</typeparam>
public abstract partial class CompositeIconMap<[MustBeVariant] T, [MustBeVariant] M> : GenericIconMap<T>, IReadOnlyDictionary<string, M> where T : struct, Enum where M : GenericIconMap<T>
{
    /// <summary>Icon map corresponding to the current input device.</summary>
    protected M CurrentIconMap => Engine.IsEditorHint() || !IconMaps.TryGetValue(DeviceManager.DeviceName, out M map) ? NoMappingMap : map;

    /// <summary>Display a warning that whatever operation is being attempted is not supported for this map and should be done directly on the consituent maps instead.</summary>
    protected void WarnUseConstituents() => GD.PushWarning("Composite icon maps can't set icon mappings. Set icon mappings in the constituent individual maps.");

    /// <summary>Mapping of map names to consituent device icon maps.</summary>
    public abstract Godot.Collections.Dictionary<string, M> IconMaps { get; set; }

    /// <summary>Default device icon map to use if a name isn't mapped to one.</summary>
    public abstract M NoMappingMap { get; set; }

    public M this[string key] => IconMaps[key];

    IEnumerable<string> IReadOnlyDictionary<string, M>.Keys => IconMaps.Keys;
    IEnumerable<M> IReadOnlyDictionary<string, M>.Values => IconMaps.Values;

    public override Godot.Collections.Dictionary<T, Texture2D> Icons
    {
        get => CurrentIconMap?.Icons ?? [];
        set => WarnUseConstituents();
    }

    public override Texture2D NoMappedActionIcon
    {
        get => CurrentIconMap?.NoMappedActionIcon;
        set => WarnUseConstituents();
    }

    public override Texture2D NoMappedInputIcon
    {
        get => CurrentIconMap?.NoMappedInputIcon;
        set => WarnUseConstituents();
    }

    public override T GetInput(StringName action) => CurrentIconMap.GetInput(action);
    public override bool InputIsInvalid(T input) => CurrentIconMap.InputIsInvalid(input);
    public bool ContainsKey(string key) => IconMaps.ContainsKey(key);
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out M value) => IconMaps.TryGetValue(key, out value);
    IEnumerator<KeyValuePair<string, M>> IEnumerable<KeyValuePair<string, M>>.GetEnumerator() => IconMaps.GetEnumerator();

    public override void _ValidateProperty(Dictionary property)
    {
        base._ValidateProperty(property);
        if (property["name"].AsStringName() == PropertyName.NoMappedActionIcon || property["name"].AsStringName() == PropertyName.NoMappedInputIcon)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
    }
}