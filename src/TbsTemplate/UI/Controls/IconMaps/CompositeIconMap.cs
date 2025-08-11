using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Godot.Collections;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI.Controls.IconMaps;

public abstract partial class CompositeIconMap<[MustBeVariant] T, [MustBeVariant] M> : GenericIconMap<T>, IReadOnlyDictionary<string, M> where T : struct, Enum where M : GenericIconMap<T>
{
    protected M CurrentIconMap => Engine.IsEditorHint() || !IconMaps.TryGetValue(DeviceManager.DeviceName, out M map) ? NoMappingMap : map;
    protected void WarnUseConstituents() => GD.PushWarning("Composite icon maps can't set icon mappings. Set icon mappings in the constituent individual maps.");

    public M this[string key] => IconMaps[key];

    public override Godot.Collections.Dictionary<T, Texture2D> Icons
    {
        get => CurrentIconMap?.Icons ?? [];
        set => WarnUseConstituents();
    }

    public abstract Godot.Collections.Dictionary<string, M> IconMaps { get; set; }

    public abstract M NoMappingMap { get; set; }

    IEnumerable<string> IReadOnlyDictionary<string, M>.Keys => IconMaps.Keys;

    IEnumerable<M> IReadOnlyDictionary<string, M>.Values => IconMaps.Values;

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