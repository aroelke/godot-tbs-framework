using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ui.input.map;

/// <summary>Resource representing a mapping from some set of values onto a set of <c>Texture2D</c> icons.</summary>
[GlobalClass, Tool]
public partial class IconMap : Resource
{
    private string GetPath(string img) => $"{IconPath}/{img}{IconExt}";

    [Export(PropertyHint.Dir)] public string IconPath = null;

    [Export(PropertyHint.EnumSuggestion, ".bmp,.dds,.ktx,.exr,.hdr,.jpg,.jpeg,.png,.tga,.svg,.webp")]
    public string IconExt = ".png";

    public Texture2D this[string k] => ResourceLoader.Load<Texture2D>(GetPath(k));

    public bool Contains(string k) => ResourceLoader.Exists(GetPath(k));
}