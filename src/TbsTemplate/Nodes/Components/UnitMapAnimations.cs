using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Nodes.Components;

/// <summary><see cref="Unit"/> component containign a collection of animations available to display on the map.</summary>
public abstract partial class UnitMapAnimations : Node
{
    /// <summary>Begin the idle animation for when the unit is available to act but not selected.</summary>
    public abstract void Idle();

    /// <summary>Begin the selected animation for when the unit has been selected to act but its action has not been chosen.</summary>
    public abstract void Selected();

    /// <summary>Begin the animation for when the unit is moving in a direction.</summary>
    /// <param name="direction">Direction on the map the unit is moving.</param>
    public abstract void Move(Vector2 direction);

    /// <summary>Begin the animation for when the unit has finished acting and is no longer available.</summary>
    public abstract void Done();
}