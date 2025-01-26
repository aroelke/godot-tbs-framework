using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>
/// A <see cref="Unit"/> resource that provides information about how the AI uses it in a
/// specific situation.
/// </summary>
[GlobalClass, Tool]
public abstract partial class UnitBehavior : Resource
{
    /// <summary>Find the cell a unit will move to if chosen to act.</summary>
    /// <param name="unit">Acting unit.</param>
    public abstract Vector2I DesiredMoveTarget(Unit unit);

    /// <summary>Determine the action that the unit will perform if moved to <see cref="DesiredMoveTarget"/>.</summary>
    public abstract StringName DesiredAction();
}