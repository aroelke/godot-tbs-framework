using System.Threading.Tasks;
using Godot;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Nodes.Components;

/// <summary><see cref="Unit"/> component containign a collection of animations available to display on the map.</summary>
public abstract partial class UnitMapAnimations : Node2D
{
    [Signal] public delegate void AnimationFinishedEventHandler();

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
    public abstract void BeginAttack(Vector2I source, Vector2I target, bool hit);

    public abstract void FinishAttack();

    /// <summary>Begin an animation to support something in a target cell.</summary>
    /// <param name="source">Cell the support is being made from</param> 
    /// <param name="target">Cell to support.</param>
    public abstract void BeginSupport(Vector2I source, Vector2I target);

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