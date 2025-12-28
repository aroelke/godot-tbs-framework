using Godot;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Nodes.Components;

/// <summary><see cref="Unit"/> component containign a collection of animations available to display on the map.</summary>
public abstract partial class UnitMapAnimations : Node2D
{
    /// <summary>Begin the idle animation for when the unit is available to act but not selected.</summary>
    public abstract void PlayIdle();

    /// <summary>Begin the selected animation for when the unit has been selected to act but its action has not been chosen.</summary>
    public abstract void PlaySelected();

    /// <summary>Begin the animation for when the unit is moving in a direction.</summary>
    /// <param name="direction">Direction on the map the unit is moving.</param>
    public abstract void PlayMove(Vector2 direction);

    /// <summary>Begin the animation for when the unit has finished acting and is no longer available.</summary>
    public abstract void PlayDone();

    /// <summary>Set the unit's maximum health value to indicate on the map.</summary>
    /// <param name="value">New maximum health value.</param>
    public abstract void SetHealthValue(double value);

    /// <summary>Set the unit's current health value to indicate on the map.</summary>
    /// <param name="value">New current health value.</param>
    public abstract void SetHealthMax(double value);
}