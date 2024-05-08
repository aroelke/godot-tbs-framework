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
            Accelerate = true;
        else if (@event.IsActionReleased(MoveAccelerateAction))
            Accelerate = false;
    }
}