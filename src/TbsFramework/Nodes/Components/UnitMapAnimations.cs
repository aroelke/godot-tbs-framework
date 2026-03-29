using Godot;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Nodes.Components;

/// <summary><see cref="Unit"/> component containing a collection of animations available to display on the map.</summary>
public abstract partial class UnitMapAnimations : Node2D
{
    /// <summary>Indicates that an animation has finished.</summary>
    [Signal] public delegate void AnimationFinishedEventHandler();

    /// <summary>Map renderer that helps with translating cells into world locations for animations.</summary>
    public Grid Grid = null;

    /// <summary>Begin the idle animation for when the unit is available to act but not selected.</summary>
    public abstract void PlayIdle();

    /// <summary>Begin the selected animation for when the unit has been selected to act but its action has not been chosen.</summary>
    public abstract void PlaySelected();

    /// <summary>Begin the animation for when the unit is moving in a direction.</summary>
    /// <param name="direction">Direction on the map the unit is moving.</param>
    public abstract void PlayMove(Vector2 direction);

    /// <summary>Begin the animation for when the unit has finished acting and is no longer available.</summary>
    public abstract void PlayDone();

    /// <summary>Begin an animation to attack something in a target cell.</summary>
    /// <param name="source">Cell the attack is being made from</param> 
    /// <param name="target">Cell to attack.</param>
    /// <param name="hit">Whether or not the attack hit its target.</param>
    public abstract void BeginAttack(Vector2I source, Vector2I target, bool hit);

    /// <summary>Play the animation to finish an attack and return to a neutral pose.</summary>
    public abstract void FinishAttack();

    /// <summary>Begin an animation to support something in a target cell.</summary>
    /// <param name="source">Cell the support is being made from</param> 
    /// <param name="target">Cell to support.</param>
    public abstract void BeginSupport(Vector2I source, Vector2I target);

    /// <summary>Play the animation to finish a support action and return to a neutral pose.</summary>
    public abstract void FinishSupport();

    /// <summary>Begin an animation to be defeated.</summary>
    public abstract void PlayDie();

    /// <summary>Set the unit's maximum health value to indicate on the map.</summary>
    /// <param name="value">New maximum health value.</param>
    public abstract void SetHealthValue(double value);

    /// <summary>Set the unit's current health value to indicate on the map.</summary>
    /// <param name="value">New current health value.</param>
    public abstract void SetHealthMax(double value);
}