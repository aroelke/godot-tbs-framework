using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI.Controls.Icons;

/// <summary>Mapping of input actions into icons for the current game pad.</summary>
[GlobalClass, Tool]
public partial class GamepadButtonIconMap : IconMap
{
    private Dictionary<string, IndividualGamepadButtonIconMap> _maps = [];

    /// <summary>Mappings of actions onto game pad button icons for various game pads.</summary>
    [Export] public GamepadButtonIconMapElement[] IconMaps = [];

    /// <summary>Default game pad button icon mapping to use for unknown game pads.</summary>
    [Export] public IndividualGamepadButtonIconMap DefaultMap = null;

    /// <summary>Icon to display for the directional pad not pressed in any direction.</summary>
    public Texture2D Dpad
    {
        get
        {
            if (Engine.IsEditorHint())
                return DefaultMap?.Dpad;
            else
                return this[DeviceManager.DeviceName].Dpad;
        }
    }

    /// <summary>Get the button icon map for a particular game pad.</summary>
    /// <param name="key">Name of the game pad.</param>
    public IndividualGamepadButtonIconMap this[string key]
    {
        get
        {
            _maps ??= IconMaps?.ToDictionary(static (e) => e.GamepadName, static (e) => e.IconMap) ?? [];
            return _maps.TryGetValue(key, out IndividualGamepadButtonIconMap map) ? map : DefaultMap;
        }
    }

    /// <summary>Get the the icon for the current game pad of a particular button.</summary>
    /// <param name="key">Button to get the icon for.</param>
    public Texture2D this[JoyButton key]
    {
        get
        {
            if (Engine.IsEditorHint())
                return DefaultMap?[key];
            else
                return this[DeviceManager.DeviceName][key];
        }
    }

    /// <summary>Check if the current game pad has an icon for a button.</summary>
    /// <param name="key">Button to check.</param>
    /// <returns><c>true</c> if the current game pad has an icon for the button, and <c>false</c> otherwise, even if other game pads do.</returns>
    public bool ContainsKey(JoyButton key)
    {
        if (Engine.IsEditorHint())
            return DefaultMap?.ContainsKey(key) ?? false;
        else
            return this[DeviceManager.DeviceName].ContainsKey(key);
    }

    public override Texture2D this[StringName action]
    {
        get => this[InputManager.GetInputGamepadButton(action)];
        set => throw new NotSupportedException();
    }

    public override bool ContainsKey(StringName action) => ContainsKey(InputManager.GetInputGamepadButton(action));
}