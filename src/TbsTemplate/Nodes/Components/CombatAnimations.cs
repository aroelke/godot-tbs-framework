using System.Threading.Tasks;
using Godot;

namespace TbsTemplate.Nodes.Components;

/// <summary>Collection of animations to play during combat for a class or character.</summary>
[GlobalClass, Tool]
public abstract partial class CombatAnimations : BoundedNode2D
{
    /// <summary>Signals that the current animation has completed.</summary>
    [Signal] public delegate void AnimationFinishedEventHandler();

    [Signal] public delegate void StepTakenEventHandler();

    /// <summary>Signals that the camera should begin to shake for an animation frame.</summary>
    [Signal] public delegate void ShakeCameraEventHandler();

    /// <summary>Signals that the attack is about to land, so the opponent's dodge animation should begin now.</summary>
    /// <remarks>Currently, all dodge animations are expected to be 0.1 seconds long, so this fires 0.1 seconds before the attack lands.</remarks>
    [Signal] public delegate void AttackDodgedEventHandler();

    /// <summary>Signals the frame in which the attack animation connects (or misses) with the opponent.</summary>
    [Signal] public delegate void AttackStrikeEventHandler();

    /// <summary>Signals the frame at which the animation for a spell that is cast should begin.</summary>
    [Signal] public delegate void SpellCastEventHandler();

    /// <summary>Offset on the animation where an attack made by it will contact its target.</summary>
    public abstract Vector2 ContactPoint { get; }

    /// <summary>
    /// Animation speed scale ratio. A value of 1 means normal speed, between 0 and 1 means slower, and higher than 1 means faster. Negative numbers
    /// mean to play the animation backwards.
    /// </summary>
    public abstract float AnimationSpeedScale { get; set; }

    /// <summary>Set the direction the animation should be facing. The meaning of that is left to the implementation.</summary>
    /// <param name="direction">Direction the animation should face.</param>
    public abstract void SetFacing(Vector2 direction);

    /// <summary>Play the idle animation.</summary>
    public abstract void Idle();

    /// <summary>Play the animation to begin an attack up until the attack connects.</summary>
    /// <param name="target">Target of the attack to help with motion.</param>
    public abstract void BeginAttack(CombatAnimations target);

    /// <summary>Complete the attack animation and return to the idle pose.</summary>
    public abstract void FinishAttack();

    /// <summary>Play the animation used to indicate that a hit has been taken.</summary>
    /// <param name="attacker">Attacker dealing the hit to help with motion.</param>
    public abstract void TakeHit(CombatAnimations attacker);

    /// <summary>Play the animation to begin a dodge of an incoming attack.</summary>
    /// <param name="attacker">Attacker to help with motion.</param>
    public abstract void BeginDodge(CombatAnimations attacker);

    /// <summary>Complete the dodge animation and return to the idle pose.</summary>
    public abstract void FinishDodge();

    /// <summary>Play the animation to begin a support action up until the support effect begins.</summary>
    /// <param name="target">Recipient of the support to help with motion.</param>
    public abstract void BeginSupport(CombatAnimations target);

    /// <summary>Play the animation to finish support and return to the idle pose.</summary>
    public abstract void FinishSupport();

    /// <summary>Play the death animation.</summary>
    public abstract void Die();

    /// <summary>Create a task that can be awaited until an action animation has completed.</summary>
    public abstract Task ActionCompleted();
}