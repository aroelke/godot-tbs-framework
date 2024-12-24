using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.AI;

/// <summary>Behavior for determining where a <see cref="Unit"/> will move if chosen to act.</summary>
[GlobalClass, Tool]
public abstract partial class MovementBehavior : Behavior
{
    /// <summary>Find the space a <see cref="Unit"/> will move to if chosen to act.</summary>
    /// <param name="unit">Acting unit.</param>
    /// <returns>The coordinates of the space <paramref name="unit"/> wants to move to.</returns>
    public abstract Vector2I Target(Unit unit);
}