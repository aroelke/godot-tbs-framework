using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ui.input;

namespace ui.Device.Icons;

[GlobalClass, Tool]
public partial class GamepadAxisIconMap : Resource
{
    private Dictionary<string, IndividualGamepadAxisIconMap> _maps = null;

    [Export] public GamepadAxisIconMapElement[] IconMaps = Array.Empty<GamepadAxisIconMapElement>();

    [Export] public IndividualGamepadAxisIconMap DefaultMap = null;

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

    public IndividualGamepadAxisIconMap this[string key]
    {
        get
        {
            _maps ??= IconMaps?.ToDictionary((e) => e.GamepadName, (e) => e.IconMap) ?? new();
            return _maps.ContainsKey(key) ? _maps[key] : DefaultMap;
        }
    }

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

    public bool ContainsKey(JoyAxis key)
    {
        if (Engine.IsEditorHint())
            return DefaultMap?.ContainsKey(key) ?? false;
        else
            return this[DeviceManager.DeviceName].ContainsKey(key);
    }
}