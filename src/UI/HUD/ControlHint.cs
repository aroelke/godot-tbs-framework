using System.Collections.Generic;
using Godot;
using UI.Controls.Icons;
using UI.Controls.Device;
using Nodes.Components;

namespace UI.HUD;

/// <summary>Icon and label showing the input for an action that doesn't have an analog or mouse option.</summary>
[Icon("res://icons/UIIcon.svg"), SceneTree, Tool]
public partial class ControlHint : HBoxContainer, IHasInputActionProperties
{
    private InputDevice _selected = default;
    private readonly Dictionary<InputDevice, IconMap> _maps = new()
    {
        { InputDevice.Mouse,    new MouseIconMap()         },
        { InputDevice.Keyboard, new KeyIconMap()           },
        { InputDevice.Gamepad,  new GamepadButtonIconMap() }
    };

    private void Update(InputDevice device, StringName action)
    {
        if (Icon is not null)
            Icon.Texture = _maps[device][action] ?? _maps[FallBackDevice][action];
    }

    public IHasInputActionProperties.InputActionProperty[] InputActions { get; set; } = [new("Action", "")];

    [Export] public string Description
    {
        get => Label?.Text[2..] ?? "";
        set
        {
            if (Label is not null)
                Label.Text = $": {value}";
        }
    }

    /// <summary>Whether or not to fall back to the keyboard icon when a mouse icon for an action doesn't exist.</summary>
    [Export] public InputDevice FallBackDevice = InputDevice.Keyboard;

    /// <summary>Switch the device to use for the icon to display.</summary>
    [Export] public InputDevice SelectedDevice
    {
        get => _selected;
        set
        {
            if (_selected != value)
            {
                _selected = value;
                Update(_selected, InputActions[0]);
            }
        }
    }

    /// <summary><see cref="MouseButton"/> map for the mouse input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public MouseIconMap MouseMap
    {
        get => _maps[InputDevice.Mouse] as MouseIconMap;
        set
        {
            _maps[InputDevice.Mouse] = value;
            Update(SelectedDevice, InputActions[0]);
        }
    }

    /// <summary><see cref="Key"/> map for the keyboard input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public KeyIconMap KeyMap
    {
        get => _maps[InputDevice.Keyboard] as KeyIconMap;
        set
        {
            _maps[InputDevice.Keyboard] = value;
            Update(SelectedDevice, InputActions[0]);
        }
    }

    /// <summary><see cref="JoyButton"/> map for the game pad input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public GamepadButtonIconMap GamepadButtonIconMap
    {
        get => _maps[InputDevice.Gamepad] as GamepadButtonIconMap;
        set
        {
            _maps[InputDevice.Gamepad] = value;
            Update(SelectedDevice, InputActions[0]);
        }
    }

    /// <summary>When the input device changes, also update the icon.</summary>
    /// <param name="device">New device being used for input.</param>
    public void OnInputDeviceChanged(InputDevice device, string name) => SelectedDevice = device;

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = base._GetPropertyList() ?? [];
        properties.AddRange(((IHasInputActionProperties)this).GetInputActionProperties());
        return properties;
    }

    public override Variant _Get(StringName property)
    {
        if (((IHasInputActionProperties)this).GetInputActionPropertyValue(property, out StringName value))
            return value;
        return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (value.VariantType == Variant.Type.StringName && ((IHasInputActionProperties)this).SetInputActionPropertyValue(property, value.AsStringName()))
        {
            Update(SelectedDevice, InputActions[0]);
            return true;
        }
        return base._Set(property, value);
    }

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (((IHasInputActionProperties)this).InputActionPropertyGetRevert(property, out StringName revert))
            return revert;
        return base._PropertyGetRevert(property);
    }

    public override bool _PropertyCanRevert(StringName property)
    {
        if (((IHasInputActionProperties)this).InputActionPropertyCanRevert(property, out bool revert))
            return revert;
        return base._PropertyCanRevert(property);
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        if (Engine.IsEditorHint())
            SelectedDevice = InputDevice.Mouse;
        else
        {
            SelectedDevice = DeviceManager.Device;
            DeviceManager.Singleton.InputDeviceChanged += OnInputDeviceChanged;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (!Engine.IsEditorHint())
            DeviceManager.Singleton.InputDeviceChanged -= OnInputDeviceChanged;
    }
}