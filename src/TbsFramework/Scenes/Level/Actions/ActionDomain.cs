using Godot;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Scenes.Level.Actions;

/// <summary>
/// Describes the cells a unit could perform an action from across the entire map. This does not take into account the cells a unit can move
/// to from its current position. Can be combined with other instances of this class within <see cref="UnitAction"/> to construct complex
/// action domains.
/// </summary>
[GlobalClass]
public abstract partial class ActionDomain : Resource
{
    /// <returns><c>true</c> if <paramref name="cell"/> is a cell that the action can be performed from, and <c>false</c> otherwise.</returns>
    public abstract bool Contains(Vector2I cell);

    /// <summary>Perform any initial setup at the beginning of the level.</summary>
    /// <param name="manager">Node providing access to the scene tree in case any information needs to be extracted from it.</param>
    public virtual void Initialize(LevelManager manager) {}
}