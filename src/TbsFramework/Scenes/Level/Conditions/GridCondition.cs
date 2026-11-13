using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>
/// Represents a predicate on the state of the map. Provides an interface to periodically evaluate the state of a map and
/// determine if the predicate is satisfied or not. The owner of the resource for a particular scene is responsible for
/// either performing the evaluation manually or connecting/disconnecting the evaluation method to/from the
/// appropriate signals.
/// </summary>
[GlobalClass, Tool]
public abstract partial class GridCondition : Resource
{
    /// <returns>
    /// <c>true</c> if <paramref name="grid"/> satisfies the predicate this resource represents, or
    /// <c>false</c> otherwise.
    /// <returns>
    public abstract bool IsSatisfied(GridData grid);
}