using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Nodes;
using Scenes.Combat.Animations;
using Scenes.Combat.Data;
using Scenes.Combat.UI;
using Scenes.Level.Object;

namespace Scenes.Combat;

/// <summary>Scene used to display the results of combat in a cut scene.</summary>
public partial class CombatScene : Node
{
    private readonly NodeCache _cache;
    public CombatScene() : base() => _cache = new(this);

    private readonly Dictionary<Unit, CombatAnimation> _animations = new();
    private readonly Dictionary<Unit, ParticipantInfo> _infos = new();

    private AudioStreamPlayer HitSound => _cache.GetNode<AudioStreamPlayer>("%HitSound");
    private AudioStreamPlayer MissSound => _cache.GetNode<AudioStreamPlayer>("%MissSound");
    private ParticipantInfo LeftInfo => _cache.GetNode<ParticipantInfo>("%LeftInfo");
    private ParticipantInfo RightInfo => _cache.GetNode<ParticipantInfo>("%RightInfo");

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
            void OnHit()
            {
                OnAttackStrike(true);
                _infos[action.Target].TransitionHealth(_infos[action.Target].CurrentHealth - action.Damage, 0.3);
            }
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
    [Export] public Vector2 LeftPosition  = new(44, 80);

    /// <summary>Position to display the right unit's sprite.</summary>
    [Export] public Vector2 RightPosition = new(116, 80);

    public void OnTimerTimeout() => SceneManager.EndCombat();

    public void OnAttackStrike(bool hit)
    {
        if (hit)
            HitSound.Play();
        else
            MissSound.Play();
    }

    /// <summary>Set up the combat scene and then begin animation.</summary>
    /// <param name="left">Constant information about the unit on the left side of the screen.</param>
    /// <param name="right">Constant information about the unit on the right side of the screen.</param>
    /// <param name="actions">Action that will be performed each turn in combat. The length of the queue determines the number of turns.</param>
    /// <exception cref="ArgumentException">If any <see cref="CombatAction"/> contains an _animations[action.Actor] who isn't participating in this combat.</exception>
    public void Start(ParticipantData left, ParticipantData right, IImmutableList<CombatAction> actions)
    {
        foreach (CombatAction action in actions)
            if (action.Actor != left.Unit && action.Actor != right.Unit)
                throw new ArgumentException($"CombatAction {action.Actor.Name} is not a participant in combat");

        _animations[left.Unit] = left.Unit.Class.CombatAnimations.Instantiate<CombatAnimation>();
        _animations[left.Unit].Modulate = left.Unit.Affiliation.Color;
        _animations[left.Unit].Position = LeftPosition;
        _animations[left.Unit].Left = true;
        _infos[left.Unit] = LeftInfo;
        LeftInfo.MaxHealth = left.Unit.Health.Maximum;
        LeftInfo.CurrentHealth = left.Unit.Health.Value;
        LeftInfo.Damage = actions.Where((a) => a.Actor == left.Unit).Select((a) => a.Damage).ToArray();
        LeftInfo.HitChance = left.HitChance;

        _animations[right.Unit] = right.Unit.Class.CombatAnimations.Instantiate<CombatAnimation>();
        _animations[right.Unit].Modulate = right.Unit.Affiliation.Color;
        _animations[right.Unit].Position = RightPosition;
        _animations[right.Unit].Left = false;
        _infos[right.Unit] = RightInfo;
        RightInfo.MaxHealth = right.Unit.Health.Maximum;
        RightInfo.CurrentHealth = right.Unit.Health.Value;
        RightInfo.Damage = actions.Where((a) => a.Actor == right.Unit).Select((a) => a.Damage).ToArray();
        RightInfo.HitChance = right.HitChance;

        Queue<CombatAction> q = new(actions);
        foreach ((_, CombatAnimation animation) in _animations)
        {
            AddChild(animation);
            animation.Returned += () => RunTurn(q);
        }
        RunTurn(q);
    }
}