using System.Linq;
using Godot;

namespace UI.Controls.Device;

/// <summary>Manages information about and changes in input actions.</summary>
public partial class InputManager : Node2D
{
    /// <summary>Signals that the mouse has entered the <see cref="Viewport"/>.</summary>
    /// <param name="position">Position the mouse entered the <see cref="Viewport"/> on (depending on the mouse speed, it might not be on the edge).</param>
    [Signal] public delegate void MouseEnteredEventHandler(Vector2 position);

    /// <summary>Signals that the mouse has exited the <see cref="Viewport"/>.</summary>
    /// <param name="position">Position on the edge of the <see cref="Viewport"/> the mouse exited.</param>
    [Signal] public delegate void MouseExitedEventHandler(Vector2 position);

    /// <summary>Last known position the mouse was on the screen if it's off the screen, or <c>null</c> if it's on the screen.</summary>
    private static Vector2? _lastKnownPointerPosition = Vector2.Zero;
    private static InputManager _singleton = null;

    /// <summary>Reference to the autoloaded <c>InputManager</c> node so its signals can be connected.</summary>
    public static InputManager Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<InputManager>("InputManager");

    /// <returns><c>true</c> if the mouse is in the <see cref="Viewport"/>, and <c>false</c> otherwise.</returns>
    public static bool IsMouseOnScreen() => _lastKnownPointerPosition is null;

    /// <returns>
    /// The position of the mouse on the <see cref="Viewport"/> or the position on the edge of the <see cref="Viewport"/> closest to where
    /// it was last if it's not on screen.
    /// </returns>
    public static Vector2 GetMousePosition() => _lastKnownPointerPosition ?? Singleton.GetViewport().GetMousePosition();

    /// <returns>The list of input actions.</returns>
    public static StringName[] GetInputActions() => ProjectSettings.Singleton.GetPropertyList().Select((p) => p["name"].As<string>().Split("/")).Where((p) => p[0] == "input").Select((i) => new StringName(i[1])).ToArray();

    /// <summary>
    /// Get an input event for an action of the desired type, if one is defined.  Assumes there is no more than one of each type of
    /// <see cref="InputEvent"/> for any given action.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="InputEvent"/> to get.</typeparam>
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

    /// <summary>
    /// Get the <see cref="MouseButton"/>, if any, for an input action.
    /// Assumes there's only one <see cref="MouseButton"/> mapped to the action.
    /// </summary>
    /// <param name="action">Name of the action to get the mouse button for.</param>
    /// <returns>The mouse button corresponding to the action, or <see cref="MouseButton.None"/> if there isn't one.</returns>
    public static MouseButton GetInputMouseButton(StringName action)
    {
        InputEventMouseButton button = GetInputEvent<InputEventMouseButton>(action);
        if (button == null)
            return MouseButton.None;
        else
            return button.ButtonIndex;
    }

    /// <summary>Get the physical <see cref="Key"/>, if any, for an input action.  Assumes there's only one <see cref="Key"/> mapped to the action.</summary>
    /// <param name="action">Name of the action to get the code for.</param>
    /// <returns>The physical <see cref="Key"/> corresponding to the action, or <see cref="Key.None"/> if there isn't one.</returns>
    public static Key GetInputKeycode(StringName action)
    {
        InputEventKey key = GetInputEvent<InputEventKey>(action);
        if (key == null)
            return Key.None;
        else
            return key.PhysicalKeycode;
    }

    /// <summary>Get the <see cref="JoyButton"/>, if any, for an input action.  Assumes there's only one <see cref="JoyButton"/> mapped to the action.</summary>
    /// <param name="action">Name of the action to get the <see cref="JoyButton"/> for.</param>
    /// <returns>The <see cref="JoyButton"/> corresponding to the action, or <see cref="JoyButton.Invalid"/> if there isn't one.</returns>
    public static JoyButton GetInputGamepadButton(StringName action)
    {
        InputEventJoypadButton button = GetInputEvent<InputEventJoypadButton>(action);
        if (button == null)
            return JoyButton.Invalid;
        else
            return button.ButtonIndex;
    }

    /// <summary>Get the <see cref="JoyAxis"/>, if any, for an input action. Assumes there's only one <see cref="JoyAxis"/> mapped to the action.</summary>
    /// <param name="action">Name of the action to get the <see cref="JoyAxis"/> for.</param>
    /// <returns>The <see cref="JoyAxis"/> corresponding to the action, or <see cref="JoyAxis"/> if there isn't one.</returns>
    public static JoyAxis GetInputGamepadAxis(StringName action)
    {
        InputEventJoypadMotion motion = GetInputEvent<InputEventJoypadMotion>(action);
        if (motion == null)
            return JoyAxis.Invalid;
        else
            return motion.Axis;
    }

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