using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace UI.Controls.Icons;

/// <summary>Resource mapping a specific gamepad's axes to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class IndividualGamepadAxisIconMap : Resource
{
    private readonly Dictionary<JoyAxis, Texture2D> _icons = Enum.GetValues<JoyAxis>().ToDictionary((k) => k, _ => (Texture2D)null);
    private readonly Dictionary<StringName, JoyAxis> _names = Enum.GetValues<JoyAxis>().ToDictionary((k) => new StringName(Enum.GetName(k)), (k) => k);

    public ICollection<JoyAxis> Keys => _icons.Keys;
    public ICollection<Texture2D> Values => _icons.Values;
    public int Count => _icons.Count;
    public Texture2D this[JoyAxis key] { get => _icons[key]; set => _icons[key] = value; }

    /// <summary>Generic icon to display for the left stick axis, not pressed in any direction.</summary>
    [Export] public Texture2D Left = null;

    /// <summary>Generic icon to display for the right stick axis, not pressed in any direction.</summary>
    [Export] public Texture2D Right = null;

    public bool ContainsKey(JoyAxis key) => _icons.ContainsKey(key);
}