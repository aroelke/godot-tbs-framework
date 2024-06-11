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
using UI;

namespace Scenes.Combat;

/// <summary>Scene used to display the results of combat in a cut scene.</summary>
public partial class CombatScene : Node
{
    private readonly NodeCache _cache;
    public CombatScene() : base() => _cache = new(this);

    private readonly Dictionary<Unit, CombatAnimation> _animations = new();
    private readonly Dictionary<Unit, ParticipantInfo> _infos = new();
    private IImmutableList<CombatAction> _actions = null;

    private Camera2DBrain Camera => _cache.GetNode<Camera2DBrain>("Camera");
    private AudioStreamPlayer HitSound => _cache.GetNode<AudioStreamPlayer>("%HitSound");
    private AudioStreamPlayer MissSound => _cache.GetNode<AudioStreamPlayer>("%MissSound");
    private AudioStreamPlayer DeathSound => _cache.GetNode<AudioStreamPlayer>("DeathSound");
    private ParticipantInfo LeftInfo => _cache.GetNode<ParticipantInfo>("%LeftInfo");
    private ParticipantInfo RightInfo => _cache.GetNode<ParticipantInfo>("%RightInfo");
    private Timer HitDelay => _cache.GetNode<Timer>("%HitDelay");
    private Timer TurnDelay => _cache.GetNode<Timer>("%TurnDelay");

    /// <summary>Position to display the left unit's sprite.</summary>
    [Export] public Vector2 LeftPosition  = new(44, 80);

    /// <summary>Position to display the right unit's sprite.</summary>
    [Export] public Vector2 RightPosition = new(116, 80);

    public void OnTimerTimeout() => SceneManager.EndCombat();

    /// <summary>Set up the combat scene.</summary>
    /// <param name="left">Unit on the left side of the screen.</param>
    /// <param name="right">Unit on the right side of the screen.</param>
    /// <param name="actions">List of actions that will be performed each turn in combat. The length of the list determines the number of turns.</param>
    /// <exception cref="ArgumentException">If any <see cref="CombatAction"/> contains an _animations[action.Actor] who isn't participating in this combat.</exception>
    public void Initialize(Unit left, Unit right, IImmutableList<CombatAction> actions)
    {
        foreach (CombatAction action in actions)
            if (action.Actor != left && action.Actor != right)
                throw new ArgumentException($"CombatAction {action.Actor.Name} is not a participant in combat");

        _actions = actions;

        _animations[left] = left.Class.CombatAnimations.Instantiate<CombatAnimation>();
        _animations[left].Modulate = left.Affiliation.Color;
        _animations[left].Position = LeftPosition;
        _animations[left].Left = true;
        _infos[left] = LeftInfo;
        LeftInfo.Health = left.Health;
        LeftInfo.Damage = _actions.Where((a) => a.Actor == left).Select((a) => a.Damage).ToArray();
        LeftInfo.HitChance = Mathf.Clamp(CombatCalculations.HitChance(left, right), 0, 100);

        _animations[right] = right.Class.CombatAnimations.Instantiate<CombatAnimation>();
        _animations[right].Modulate = right.Affiliation.Color;
        _animations[right].Position = RightPosition;
        _animations[right].Left = false;
        _infos[right] = RightInfo;
        RightInfo.Health = right.Health;
        RightInfo.Damage = _actions.Where((a) => a.Actor == right).Select((a) => a.Damage).ToArray();
        RightInfo.HitChance = Mathf.Clamp(CombatCalculations.HitChance(right, left), 0, 100);

        foreach ((_, CombatAnimation animation) in _animations)
            AddChild(animation);
    }

    /// <summary>Run the full combat animation.</summary>
    public async void Start()
    {
        // Play the combat sequence
        foreach (CombatAction action in _actions)
        {
            void OnDodge() =>  _animations[action.Target].PlayAnimation(CombatAnimation.DodgeAnimation);
            void OnHit()
            {
                HitSound.Play();
                Camera.Trauma += 0.2f;
                _infos[action.Target].TransitionHealth(_infos[action.Target].Health.Value - action.Damage, 0.3);
            }
            void OnMiss() => MissSound.Play();

            // Reset all participants
            foreach ((_, CombatAnimation animation) in _animations)
            {
                animation.ZIndex = 0;
                animation.PlayAnimation(CombatAnimation.IdleAnimation);
            }

            // Set up animation triggers
            if (action.Hit)
            {
                _animations[action.Actor].ZIndex = 1;
                _animations[action.Actor].AttackStrike += OnHit;
            }
            else
            {
                _animations[action.Target].ZIndex = 1;
                _animations[action.Actor].AttackDodged += OnDodge;
                _animations[action.Actor].AttackStrike += OnMiss;
            }

            // Play the animation sequence for the turn
            _animations[action.Actor].PlayAnimation(CombatAnimation.AttackAnimation);
            await ToSignal(_animations[action.Actor], CombatAnimation.SignalName.AnimationFinished);
            HitDelay.Start();
            await ToSignal(HitDelay, Timer.SignalName.Timeout);
            _animations[action.Actor].PlayAnimation(CombatAnimation.AttackReturnAnimation);
            if (!action.Hit)
                _animations[action.Target].PlayAnimation(CombatAnimation.DodgeReturnAnimation);
            await ToSignal(_animations[action.Actor], CombatAnimation.SignalName.AnimationFinished);

            // Clean up any triggers
            if (action.Hit)
            {
                _animations[action.Actor].AttackStrike -= OnHit;
                if (_infos[action.Target].Health.Value == 0)
                {
                    _animations[action.Target].PlayAnimation(CombatAnimation.DieAnimation);
                    DeathSound.Play();
                    await ToSignal(_animations[action.Target], CombatAnimation.SignalName.AnimationFinished);
                }
            }
            else
            {
                _animations[action.Actor].AttackDodged -= OnDodge;
                _animations[action.Actor].AttackStrike -= OnMiss;
            }

            TurnDelay.Start();
            await ToSignal(TurnDelay, Timer.SignalName.Timeout);
        }
        GetNode<Timer>("CombatDelay").Start();
    }
}