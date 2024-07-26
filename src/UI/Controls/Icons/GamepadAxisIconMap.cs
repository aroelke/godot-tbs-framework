using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UI.Controls.Device;

namespace UI.Controls.Icons;

/// <summary>Resource that maps input actions onto game pad axis icons for the current game pad.</summary>
[GlobalClass, Tool]
public partial class GamepadAxisIconMap : IconMap
{
    private Dictionary<string, IndividualGamepadAxisIconMap> _maps = null;

    /// <summary>Mappings of input actions onto game pad axis icons.</summary>
    [Export] public GamepadAxisIconMapElement[] IconMaps = Array.Empty<GamepadAxisIconMapElement>();

    /// <summary>Default icon map to use when a gamepad doesn't have its own icon map.</summary>
    [Export] public IndividualGamepadAxisIconMap DefaultMap = null;

    /// <summary>Icon to use for the left joystick not pressed in a direction.</summary>
    public Texture2D Left
    {
        get
        {
            if (Engine.IsEditorHint())
                return DefaultMap?.Left;
            else
                return this[DeviceManager.DeviceName].Left;
        }
    }

    /// <summary>Icon to use for the right joystick not pressed in a direction.</summary>
    public Texture2D Right
    {
        get
        {
            if (Engine.IsEditorHint())
                return DefaultMap?.Right;
            else
                return this[DeviceManager.DeviceName].Right;
        }
    }

    /// <summary>Get the axis icon map for a particular game pad.</summary>
    /// <param name="key">Name of the game pad.</param>
    public IndividualGamepadAxisIconMap this[string key]
    {
        get
        {
            _maps ??= IconMaps?.ToDictionary(static (e) => e.GamepadName, static (e) => e.IconMap) ?? new();
            return _maps.ContainsKey(key) ? _maps[key] : DefaultMap;
        }
    }

    /// <summary>Get an axis icon for the current game pad.</summary>
    /// <param name="key">Axis to get the icon of.</param>
    public Texture2D this[JoyAxis key]
    {
        get
        {
            if (Engine.IsEditorHint())
                return DefaultMap?[key];
            else
                return this[DeviceManager.DeviceName][key];
        }
    }

    /// <summary>Check if an axis has an icon mapped to it for the current game pad.</summary>
    /// <param name="key">Game pad axis to check.</param>
    /// <returns><c>true</c> if the current game pad has an icon for the axis, and <c>false</c> if it doesn't (even if others do).</returns>
    public bool ContainsKey(JoyAxis key)
    {
        if (Engine.IsEditorHint())
            return DefaultMap?.ContainsKey(key) ?? false;
        else
            return this[DeviceManager.DeviceName].ContainsKey(key);
    }

    public override Texture2D this[StringName action]
    {
        get => this[InputManager.GetInputGamepadAxis(action)];
        set => throw new NotSupportedException();
    }

    public override bool ContainsKey(StringName action) => ContainsKey(InputManager.GetInputGamepadAxis(action));
}