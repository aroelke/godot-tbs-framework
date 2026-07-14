using Godot;

namespace TbsFramework.Scenes.Level.Actions;

/// <summary>
/// Describes the cells a unit could perform an action from across the entire map. This does not take into account the cells a unit can move
/// to from its current position. Can be combined with other instances of this class within <see cref="GenericUnitAction"/> to construct complex
/// action domains.
/// </summary>
[GlobalClass]
public abstract partial class ActionDomain : Resource
{
    /// <returns><c>true</c> if <paramref name="cell"/> is a cell that the action can be performed from, and <c>false</c> otherwise.</returns>
    public abstract bool Contains(Vector2I cell);

    /// <summary>Perform any initial setup at the beginning of the level.</summary>
    /// <param name="owner">
    /// Node calling <see cref="GenericUnitAction.Initialize(Events.LevelManager)">Initialize</see> of the parent <see cref="GenericUnitAction"/> for
    /// providing access to the scene tree.
    /// </param>
    public virtual void Initialize(Node owner) {}
}