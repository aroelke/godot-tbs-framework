using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ui.input;

namespace ui.Device.Icons;

[GlobalClass, Tool]
public partial class GamepadButtonIconMap : Resource
{
    private Dictionary<string, IndividualGamepadButtonIconMap> _maps = new();

    [Export] public GamepadButtonIconMapElement[] IconMaps = Array.Empty<GamepadButtonIconMapElement>();

    [Export] public IndividualGamepadButtonIconMap DefaultMap = null;

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

    public IndividualGamepadButtonIconMap this[string key]
    {
        get
        {
            _maps ??= IconMaps?.ToDictionary((e) => e.GamepadName, (e) => e.IconMap) ?? new();
            return _maps.ContainsKey(key) ? _maps[key] : DefaultMap;
        }
    }

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

    public bool ContainsKey(JoyButton key)
    {
        if (Engine.IsEditorHint())
            return DefaultMap?.ContainsKey(key) ?? false;
        else
            return this[DeviceManager.DeviceName].ContainsKey(key);
    }
}