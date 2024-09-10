using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.UI.Controls.Device;
using TbsTemplate.UI.Controls.Icons.Generic;

namespace TbsTemplate.UI.Controls.Icons;

/// <summary>Resource mapping mouse actions onto icons to display for them.</summary>
[GlobalClass, Tool]
public partial class MouseIconMap : IconMap, IIconMap<MouseButton>
{
    private readonly Dictionary<MouseButton, Texture2D> _icons = Enum.GetValues<MouseButton>().ToDictionary(static (k) => k, static _ => (Texture2D)null);
    private readonly Dictionary<StringName, MouseButton> _names = Enum.GetValues<MouseButton>().ToDictionary(static (k) => new StringName(Enum.GetName(k)), static (k) => k);

    public ICollection<MouseButton> Keys => _icons.Keys;
    public ICollection<Texture2D> Values => _icons.Values;
    public int Count => _icons.Count;
    public Texture2D this[MouseButton key] { get => _icons[key]; set => _icons[key] = value; }
    public override Texture2D this[StringName action] { get => this[InputManager.GetInputMouseButton(action)]; set => this[InputManager.GetInputMouseButton(action)] = value; }

    /// <summary>Icon to display for mouse motion.</summary>
    [Export] public Texture2D Motion = null;

    /// <summary>Left click icon.</summary>
    [Export] public Texture2D Left
    {
        get => _icons[MouseButton.Left];
        set => _icons[MouseButton.Left] = value;
    }

    /// <summary>Right click icon.</summary>
    [Export] public Texture2D Right
    {
        get => _icons[MouseButton.Right];
        set => _icons[MouseButton.Right] = value;
    }

    public bool ContainsKey(MouseButton key) => _icons.ContainsKey(key);
    public override bool ContainsKey(StringName action) => ContainsKey(InputManager.GetInputMouseButton(action));
}