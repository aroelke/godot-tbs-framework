using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ui.input;

public partial class InputManager : Node2D
{
    public const string NodePath = "/root/InputManager";

    /// <summary>Signals that the input method has changed.</summary>
    /// <param name="device">New method of input.</param>
    [Signal] public delegate void InputDeviceChangedEventHandler(InputDevice device);

    /// <summary>Signals that the input mode has changed.</summary>
    /// <param name="mode">New input mode</param>
    [Signal] public delegate void InputModeChangedEventHandler(InputMode mode);

    /// <summary>Signals that the mouse has entered the screen.</summary>
    /// <param name="position">Position the mouse entered the screen on (depending on the mouse speed, it might not be on the edge).</param>
    [Signal] public delegate void MouseEnteredEventHandler(Vector2 position);

    /// <summary>Signals that the mouse has exited the screen.</summary>
    /// <param name="position">Position on the edge of the screen the mouse exited.</param>
    [Signal] public delegate void MouseExitedEventHandler(Vector2 position);

    /// <summary>
    /// Get an input event for an action of the desired type, if one is defined.  Assumes there is no more than one of each type of
    /// <c>InputEvent</c> for any given action.
    /// </summary>
    /// <typeparam name="T">Type of <c>InputEvent</c> to get.</typeparam>
    /// <param name="action">Name of the input action to get the event for.</param>
    /// <returns>The input event of the given type for the action.</returns>
    public static T GetInputEvent<T>(string action) where T : InputEvent
    {
        if (Engine.IsEditorHint())
        {
            string setting = $"input/{action}";
            if (ProjectSettings.HasSetting(setting))
            {
                Godot.Collections.Array<InputEvent> events = ProjectSettings.GetSetting(setting).As<Godot.Collections.Dictionary>()["events"].As<Godot.Collections.Array<InputEvent>>();
                return events.Select((e) => e as T).Where((e) => e is not null).FirstOrDefault();
            }
            else
                return default;
        }
        else
            return InputMap.ActionGetEvents(action).Select((e) => e as T).Where((e) => e is not null).FirstOrDefault();
    }

    /// <summary>Get the mouse button, if any, for an input action.  Assumes there's only one mouse button mapped to the action.</summary>
    /// <param name="action">Name of the action to get the mouse button for.</param>
    /// <returns>The mouse button corresponding to the action, or <c>MouseButton.None</c> if there isn't one.</returns>
    public static MouseButton GetInputMouseButton(string action)
    {
        InputEventMouseButton button = GetInputEvent<InputEventMouseButton>(action);
        if (button == null)
            return MouseButton.None;
        else
            return button.ButtonIndex;
    }

    /// <summary>Get the physical key code, if any, for an input action.  Assumes there's only one key mapped to the action.</summary>
    /// <param name="action">Name of the action to get the code for.</param>
    /// <returns>The physical key code corresponding to the action, or <c>Key.None</c> if there isn't one.</returns>
    public static Key GetInputKeycode(string action)
    {
        InputEventKey key = GetInputEvent<InputEventKey>(action);
        if (key == null)
            return Key.None;
        else
            return key.PhysicalKeycode;
    }

    /// <summary>Get the game pad button index, if any, for an input action.  Assumes there's only one game pad button mapped to the action.</summary>
    /// <param name="action">Name of the action to get the game pad button index for.</param>
    /// <returns>The game pad button index corresponding to the action, or <c>JoyButton.Invalid</c> if there isn't one.</returns>
    public static JoyButton GetInputGamepadButton(string action)
    {
        InputEventJoypadButton button = GetInputEvent<InputEventJoypadButton>(action);
        if (button == null)
            return JoyButton.Invalid;
        else
            return button.ButtonIndex;
    }

    /// <summary>Get the game pad axis, if any, for an input action. Assumes there's only one axis mapped to the action.</summary>
    /// <param name="action">Name of the action to get the game pad axis for.</param>
    /// <returns>The game pad axis corresponding to the action, or <c>JoyAxis.Invalid</c> if there isn't one.</returns>
    public static JoyAxis GetInputGamepadAxis(string action)
    {
        InputEventJoypadMotion motion = GetInputEvent<InputEventJoypadMotion>(action);
        if (motion == null)
            return JoyAxis.Invalid;
        else
            return motion.Axis;
    }

    /// <returns>A vector representing the digital direction(s) being held down. Elements have values 0, 1, or -1.</returns>
    public static Vector2I GetDigitalVector() => (Vector2I)Input.GetVector("cursor_digital_left", "cursor_digital_right", "cursor_digital_up", "cursor_digital_down").Round();

    /// <returns>A vector representing the movement of the left control stick of the game pad.</returns>
    public static Vector2 GetAnalogVector() => Input.GetVector("cursor_analog_left", "cursor_analog_right", "cursor_analog_up", "cursor_analog_down");

    private InputDevice _device = InputDevice.Keyboard;
    private InputMode _mode = InputMode.Digital;

    /// <summary>The current input device.</summary>
    [Export] public InputDevice Device
    {
        get => _device;
        set
        {
            InputDevice old = _device;
            _device = value;
            if (_device != old)
                EmitSignal(SignalName.InputDeviceChanged, Variant.From(_device));
        }
    }

    /// <summary>The current input mode.</summary>
    [Export] public InputMode Mode
    {
        get => _mode;
        set
        {
            InputMode old = _mode;
            _mode = value;
            if (_mode != old)
                EmitSignal(SignalName.InputModeChanged, Variant.From(_mode));
        }
    }

    /// <summary>Last known position the mouse was on the screen if it's off the screen, or <c>null</c> if it's on the screen.</summary>
    public Vector2? LastKnownPointerPosition { get; private set; } = Vector2.Zero;

    public override void _Notification(int what)
    {
        base._Notification(what);
        switch ((long)what)
        {
        case NotificationWMMouseEnter or NotificationVpMouseEnter:
            LastKnownPointerPosition = null;
            EmitSignal(SignalName.MouseEntered, GetViewport().GetMousePosition());
            break;
        case NotificationWMMouseExit or NotificationVpMouseExit:
            LastKnownPointerPosition = GetViewport().GetMousePosition().Clamp(Vector2.Zero, GetViewportRect().Size);
            EmitSignal(SignalName.MouseExited, LastKnownPointerPosition.Value);
            break;
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        switch (@event)
        {
        case InputEventMouse:
            Device = InputDevice.Mouse;
            Mode = InputMode.Mouse;
            break;
        case InputEventKey:
            Device = InputDevice.Keyboard;
            Mode = InputMode.Digital;
            break;
        case InputEventJoypadButton:
            Device = InputDevice.Playstation;
            Mode = InputMode.Digital;
            break;
        case InputEventJoypadMotion when GetAnalogVector() != Vector2.Zero:
            Device = InputDevice.Playstation;
            Mode = InputMode.Analog;
            break;
        }
    }

    public override void _Ready()
    {
        base._Ready();
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }
}