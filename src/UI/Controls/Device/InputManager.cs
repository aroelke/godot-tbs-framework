using System.Linq;
using Godot;
using UI.Controls.Action;

namespace UI.Controls.Device;

/// <summary>Manages information about and changes in input actions.</summary>
public partial class InputManager : Node2D
{
    /// <summary>Signals that the mouse has entered the screen.</summary>
    /// <param name="position">Position the mouse entered the screen on (depending on the mouse speed, it might not be on the edge).</param>
    [Signal] public delegate void MouseEnteredEventHandler(Vector2 position);

    /// <summary>Signals that the mouse has exited the screen.</summary>
    /// <param name="position">Position on the edge of the screen the mouse exited.</param>
    [Signal] public delegate void MouseExitedEventHandler(Vector2 position);

    /// <summary>Last known position the mouse was on the screen if it's off the screen, or <c>null</c> if it's on the screen.</summary>
    private static Vector2? _lastKnownPointerPosition = Vector2.Zero;
    private static InputManager _singleton = null;

    /// <summary>Reference to the autoloaded <c>InputManager</c> node so its signals can be connected.</summary>
    public static InputManager Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<InputManager>("InputManager");

    /// <returns><c>true</c> if the mouse is in the window, and <c>false</c> otherwise.</returns>
    public static bool IsMouseOnScreen() => _lastKnownPointerPosition is null;

    ///<returns>The position of the mouse on the screen or the position on the edge of the screen closest to where it was last if it's not on screen.</returns>
    public static Vector2 GetMousePosition() => _lastKnownPointerPosition ?? Singleton.GetViewport().GetMousePosition();

    /// <returns>The list of input actions.</returns>
    public static StringName[] GetInputActions() => ProjectSettings.Singleton.GetPropertyList().Select((p) => p["name"].As<string>().Split("/")).Where((p) => p[0] == "input").Select((i) => new StringName(i[1])).ToArray();

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
    public static MouseButton GetInputMouseButton(StringName action)
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
    public static Key GetInputKeycode(StringName action)
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
    public static JoyButton GetInputGamepadButton(StringName action)
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
    public static JoyAxis GetInputGamepadAxis(StringName action)
    {
        InputEventJoypadMotion motion = GetInputEvent<InputEventJoypadMotion>(action);
        if (motion == null)
            return JoyAxis.Invalid;
        else
            return motion.Axis;
    }


    /// <returns>A vector representing the movement of the left control stick of the game pad.</returns>
    public static Vector2 GetAnalogVector() => Input.GetVector(Singleton.AnalogLeftAction, Singleton.AnalogRightAction, Singleton.AnalogUpAction, Singleton.AnalogDownAction);

    [Export] public InputActionReference AnalogUpAction { get; private set; }

    [Export] public InputActionReference AnalogLeftAction { get; private set; }

    [Export] public InputActionReference AnalogDownAction { get; private set; }

    [Export] public InputActionReference AnalogRightAction { get; private set; }

    public override void _Notification(int what)
    {
        base._Notification(what);
        switch ((long)what)
        {
        case NotificationWMMouseEnter or NotificationVpMouseEnter:
            _lastKnownPointerPosition = null;
            EmitSignal(SignalName.MouseEntered, GetViewport().GetMousePosition());
            break;
        case NotificationWMMouseExit or NotificationVpMouseExit:
            _lastKnownPointerPosition = GetViewport().GetMousePosition().Clamp(Vector2.Zero, GetViewportRect().Size);
            EmitSignal(SignalName.MouseExited, _lastKnownPointerPosition.Value);
            break;
        }
    }
}