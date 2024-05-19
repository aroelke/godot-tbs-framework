using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Nodes;
using Scenes.Combat.Animations;
using Scenes.Level.Object;

namespace Scenes.Combat;

/// <summary>Scene used to display the results of combat in a cut scene.</summary>
public partial class CombatScene : Node
{
    private readonly NodeCache _cache;
    public CombatScene() : base() => _cache = new(this);

    private readonly Dictionary<Unit, CombatAnimation> _animations = new();

    private AudioStreamPlayer HitSound => _cache.GetNode<AudioStreamPlayer>("%HitSound");
    private AudioStreamPlayer MissSound => _cache.GetNode<AudioStreamPlayer>("%MissSound");

    private void RunTurn(Queue<CombatAction> actions)
    {
        // Reset all participants
        foreach ((_, CombatAnimation animation) in _animations)
        {
            animation.ZIndex = 0;
            animation.Idle();
        }

        // If there are turns left to run, run the animation for the next turn. Otherwise, end the combat sequence after a short delay.
        if (actions.Any())
        {
            CombatAction action = actions.Dequeue();
            CombatAnimation actor = _animations[action.Actor];
            CombatAnimation target = _animations[action.Target];

            if (action.Hit)
            {
                void OnHit()
                {
                    OnAttackStrike(true);
                    actor.AttackStrike -= OnHit;
                }
                actor.AttackStrike += OnHit;
            }
            else
            {
                void OnDodge()
                {
                    target.ZIndex = 2;
                    target.Dodge();
                    actor.AttackDodged -= OnDodge;
                }
                actor.AttackDodged += OnDodge;

                void OnMiss()
                {
                    OnAttackStrike(false);
                    actor.AttackStrike -= OnMiss;
                }
                actor.AttackStrike += OnMiss;

                void OnReturning()
                {
                    target.DodgeReturn();
                    actor.Returning -= OnReturning;
                }
                actor.Returning += OnReturning;
            }
            actor.ZIndex = 1;
            actor.Attack();
        }
        else
            GetNode<Timer>("CombatDelay").Start();
    }

    /// <summary>Position to display the left unit's sprite.</summary>
    [Export] public Vector2 Left  = new(44, 64);

    /// <summary>Position to display the right unit's sprite.</summary>
    [Export] public Vector2 Right = new(116, 64);

    public void OnTimerTimeout() => SceneManager.EndCombat();

    public void OnAttackStrike(bool hit)
    {
        if (hit)
            HitSound.Play();
        else
            MissSound.Play();
    }

    /// <summary>Set up the combat scene and then begin animation.</summary>
    /// <param name="left">Unit to display on the left side of the screen.</param>
    /// <param name="right">Unit to display on the right side of the screen.</param>
    /// <param name="actions">Action that will be performed each turn in combat. The length of the queue determines the number of turns.</param>
    /// <exception cref="ArgumentException">If any <see cref="CombatAction"/> contains an actor who isn't participating in this combat.</exception>
    public void Start(Unit left, Unit right, IImmutableList<CombatAction> actions)
    {
        foreach (CombatAction action in actions)
            if (action.Actor != left && action.Actor != right)
                throw new ArgumentException($"CombatAction actor {action.Actor.Name} is not a participant in combat");

        _animations[left] = left.Class.CombatAnimations.Instantiate<CombatAnimation>();
        _animations[left].Modulate = left.Affiliation.Color;
        _animations[left].Position = Left;
        _animations[left].Left = true;
        _animations[right] = right.Class.CombatAnimations.Instantiate<CombatAnimation>();
        _animations[right].Modulate = right.Affiliation.Color;
        _animations[right].Position = Right;
        _animations[right].Left = false;

        Queue<CombatAction> q = new(actions);
        foreach ((_, CombatAnimation animation) in _animations)
        {
            AddChild(animation);
            animation.Returned += () => RunTurn(q);
        }
        RunTurn(q);
    }
}