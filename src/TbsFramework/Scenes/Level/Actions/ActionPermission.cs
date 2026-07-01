using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Events;

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
    /// <param name="manager">Node providing access to the scene tree in case any information needs to be extracted from it.</param>
    public virtual void Initialize(LevelManager manager) {}
}