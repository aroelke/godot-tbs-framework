using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UI.Controls.Action;
using UI.Controls.Device;

namespace UI.Controls.Icons;

[GlobalClass, Tool]
public partial class GamepadButtonIconMap : IconMap
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

    public override Texture2D this[InputActionReference action]
    {
        get => this[action.GamepadButton];
        set => throw new NotSupportedException();
    }

    public bool ContainsKey(JoyButton key)
    {
        if (Engine.IsEditorHint())
            return DefaultMap?.ContainsKey(key) ?? false;
        else
            return this[DeviceManager.DeviceName].ContainsKey(key);
    }

    public override bool ContainsKey(InputActionReference action) => ContainsKey(action.GamepadButton);
}