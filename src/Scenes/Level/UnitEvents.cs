using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level;

/// <summary>
/// Singleton that handles events related to <see cref="Unit"/>s that can't be easily done with references or could move across scenes (such as
/// holding down the accelerate button between map and combat scenes).
/// </summary>
public partial class UnitEvents : Node
{
    /// <summary>Signals that a unit has been defeated.</summary>
    /// <param name="defeated">The unit that was defeated.</param>
    [Signal] public delegate void UnitDefeatedEventHandler(Unit defeated);

    private static UnitEvents _singleton = null;

    /// <summary>Reference to the autoloaded <see cref="Unit"/> event bus.</summary>
    public static UnitEvents Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<UnitEvents>("UnitEvents");
}