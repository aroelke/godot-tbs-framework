using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Actions;

/// <summary>Determines whether or not a unit is allowed to perform an action regardless of its location or valid targets.</summary>
[GlobalClass]
public abstract partial class ActionPermission : Resource
{
    /// <returns>
    /// <c>true</c> if <paramref name="unit"/> is allowed to perform this action based on non-positional characteristics such as faction,
    /// and <c>false</c> otherwise.
    /// </returns>
    public abstract bool CanPerform(UnitData unit);

    /// <summary>Perform any initial setup at the beginning of the level.</summary>
    /// <param name="owner">
    /// Node calling <see cref="GenericUnitAction.Initialize(Events.LevelManager)">Initialize</see> of the parent <see cref="GenericUnitAction"/> for
    /// providing access to the scene tree.
    /// </param>
    public virtual void Initialize(Node owner) {}
}