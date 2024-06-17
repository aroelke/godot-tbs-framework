using Godot;
using Scenes.Level.Object;
using UI.Controls.Action;

namespace Scenes.Level;

/// <summary>
/// Singleton that handles events related to <see cref="Unit"/>s that can't be easily done with references or could move across scenes (such as
/// holding down the accelerate button between map and combat scenes).
/// </summary>
public partial class UnitEvents : Node
{
    /// <summary>Signals that units should begin to move faster.</summary>
    [Signal] public delegate void UnitAccelerateEventHandler();

    /// <summary>Signals that units should stop moving faster.</summary>
    [Signal] public delegate void UnitDecelerateEventHandler();

    /// <summary>Signals that a unit has been defeated.</summary>
    /// <param name="defeated">The unit that was defeated.</param>
    [Signal] public delegate void UnitDefeatedEventHandler(Unit defeated);

    private static UnitEvents _singleton = null;

    /// <summary>Reference to the autoloaded <see cref="Unit"/> event bus.</summary>
    public static UnitEvents Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<UnitEvents>("UnitEvents");

    /// <summary>Whether or not a moving <see cref="Unit"/> should move faster.</summary>
    public static bool Accelerate { get; private set; } = false;

    /// <summary>Action to use to accelerate the unit while it's moving. </summary>
    [Export] public InputActionReference MoveAccelerateAction = new();

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (@event.IsActionPressed(MoveAccelerateAction))
        {
            Accelerate = true;
            EmitSignal(SignalName.UnitAccelerate);
        }
        else if (@event.IsActionReleased(MoveAccelerateAction))
        {
            Accelerate = false;
            EmitSignal(SignalName.UnitDecelerate);
        }
    }
}