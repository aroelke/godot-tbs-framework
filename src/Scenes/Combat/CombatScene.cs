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

    private async void RunTurn(Queue<CombatAction> actions)
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
            void OnHit() => OnAttackStrike(true);
            void OnMiss() => OnAttackStrike(false);

            // Set up animation triggers
            if (action.Hit)
            {
                _animations[action.Actor].ZIndex = 1;
                _animations[action.Actor].AttackStrike += OnHit;
            }
            else
            {
                _animations[action.Target].ZIndex = 1;
                _animations[action.Actor].AttackDodged += _animations[action.Target].Dodge;
                _animations[action.Actor].AttackStrike += OnMiss;
                _animations[action.Actor].Returning += _animations[action.Target].DodgeReturn;
            }

            // Begin the animation sequence, then clean up triggers when it's done
            _animations[action.Actor].Attack();
            await ToSignal(_animations[action.Actor], CombatAnimation.SignalName.Returned);
            if (action.Hit)
                _animations[action.Actor].AttackStrike -= OnHit;
            else
            {
                _animations[action.Actor].AttackDodged -= _animations[action.Target].Dodge;
                _animations[action.Actor].AttackStrike -= OnMiss;
                _animations[action.Actor].Returning -= _animations[action.Target].DodgeReturn;
            }
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
    /// <exception cref="ArgumentException">If any <see cref="CombatAction"/> contains an _animations[action.Actor] who isn't participating in this combat.</exception>
    public void Start(Unit left, Unit right, IImmutableList<CombatAction> actions)
    {
        foreach (CombatAction action in actions)
            if (action.Actor != left && action.Actor != right)
                throw new ArgumentException($"CombatAction _animations[action.Actor] {action.Actor.Name} is not a participant in combat");

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