using Godot;

namespace ui.input.map;

/// <summary>Resource representing a mapping from some set of values onto a set of <c>Texture2D</c> icons.</summary>
[GlobalClass, Tool]
public partial class IconMap : Resource
{
    private string GetPath(string img) => $"{IconPath}/{img}{IconExt}";

    /// <summary>File system path containing the icons in the map.</summary>
    [Export(PropertyHint.Dir)] public string IconPath = null;

    /// <summary>Extension of the icons in the path (they must all be the same).</summary>
    [Export(PropertyHint.EnumSuggestion, ".bmp,.dds,.ktx,.exr,.hdr,.jpg,.jpeg,.png,.tga,.svg,.webp")]
    public string IconExt = ".png";

    /// <summary>Load an icon by name.</summary>
    /// <param name="k">Name of the icon to load.</param>
    /// <returns>A <c>Texture2D</c> containing the loaded icon.</returns>
    public Texture2D this[string k] => ResourceLoader.Load<Texture2D>(GetPath(k));

    /// <summary>Determine if an icon exists.</summary>
    /// <param name="k">Name of the icon to look for.</param>
    /// <returns><c>true</c> if there is an icon with the given name, and <c>false</c> otherwise.</returns>
    public bool Contains(string k) => ResourceLoader.Exists(GetPath(k));
}