using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsTemplate.Scenes.Combat.Animations;
using TbsTemplate.Scenes.Combat.Data;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.UI;
using TbsTemplate.UI.Combat;
using TbsTemplate.UI.Controls.Action;

namespace TbsTemplate.Scenes.Combat;

/// <summary>Scene used to display the results of combat in a cut scene.</summary>
[SceneTree]
public partial class CombatScene : Node
{
    [Signal] public delegate void TimeExpiredEventHandler();

    private readonly Dictionary<Unit, CombatAnimation> _animations = [];
    private readonly Dictionary<Unit, ParticipantInfo> _infos = [];
    private IImmutableList<CombatAction> _actions = null;
    private double _remaining = 0;
    private bool _canceled = false;

    /// <summary>Wait for all actors in an action to complete their current animation, if they are acting.</summary>
    /// <param name="action">Action whose actors could be acting.</param>
    private async Task ActionCompleted(CombatAction action)
    {
        await _animations[action.Actor].ActionFinished();
        await _animations[action.Target].ActionFinished();
    }

    /// <summary>Wait for a specified amount of time.</summary>
    /// <param name="time">Amount of time to wait, in seconds.</param>
    private async Task Delay(double time)
    {
        _remaining = time;
        await ToSignal(this, SignalName.TimeExpired);
    }

    /// <summary>Background music to play during the combat scene.</summary>
    [Export] public AudioStream BackgroundMusic = null;

    /// <summary>Position to display the left unit's sprite.</summary>
    [Export] public Vector2 LeftPosition  = new(44, 80);

    /// <summary>Position to display the right unit's sprite.</summary>
    [Export] public Vector2 RightPosition = new(116, 80);

    /// <summary>Duration for the health bar to change when HP changes.</summary>
    [Export(PropertyHint.None, "suffix:s")] public double HealthBarTransitionDuration = 0.3;

    /// <summary>Delay after striking (hit or miss) before returning to idle.</summary>
    [Export(PropertyHint.None, "suffix:s")] public double HitDelay = 0.3;

    /// <summary>Delay between combat turns.</summary>
    [Export(PropertyHint.None, "suffix:s")] public double TurnDelay = 0.1;

    /// <summary>Amount to speed up the animation while the accelerate button is held down.</summary>
    [Export] public float AccelerationFactor = 2;

    /// <summary>Amount of camera shake trauma for a normal hit.</summary>
    [Export] public double CameraShakeHitTrauma = 0.2;

    /// <summary>Set up the combat scene.</summary>
    /// <param name="left">Unit on the left side of the screen.</param>
    /// <param name="right">Unit on the right side of the screen.</param>
    /// <param name="actions">List of actions that will be performed each turn in combat. The length of the list determines the number of turns.</param>
    /// <exception cref="ArgumentException">If any <see cref="CombatAction"/> contains an _animations[action.Actor] who isn't participating in this combat.</exception>
    [OnInstantiate]
    public void Initialize(Unit left, Unit right, IImmutableList<CombatAction> actions)
    {
        foreach (CombatAction action in actions)
            if (action.Actor != left && action.Actor != right)
                throw new ArgumentException($"CombatAction {action.Actor.Name} is not a participant in combat");

        _actions = actions;

        _animations[left] = left.Class.CombatAnimations.Instantiate<CombatAnimation>();
        _animations[left].Modulate = left.Faction.Color;
        _animations[left].Position = LeftPosition;
        _animations[left].Left = true;
        _animations[left].StepTaken += () => StepSound.Play();
        _infos[left] = LeftInfo;
        LeftInfo.Health = left.Health;
        LeftInfo.Damage = _actions.Where((a) => a.Actor == left).Select(static (a) => a.Damage).ToArray();
        LeftInfo.HitChance = Mathf.Clamp(CombatCalculations.HitChance(left, right), 0, 100);
        LeftInfo.TransitionDuration = HitDelay;

        _animations[right] = right.Class.CombatAnimations.Instantiate<CombatAnimation>();
        _animations[right].Modulate = right.Faction.Color;
        _animations[right].Position = RightPosition;
        _animations[right].Left = false;
        _animations[right].StepTaken += () => StepSound.Play();
        _infos[right] = RightInfo;
        RightInfo.Health = right.Health;
        RightInfo.Damage = _actions.Where((a) => a.Actor == right).Select(static (a) => a.Damage).ToArray();
        RightInfo.HitChance = Mathf.Clamp(CombatCalculations.HitChance(right, left), 0, 100);
        RightInfo.TransitionDuration = HitDelay;

        foreach ((var _, CombatAnimation animation) in _animations)
            AddChild(animation);
    }

    /// <summary>Run the full combat animation.</summary>
    public async void Start()
    {
        // Play the combat sequence
        foreach (CombatAction action in _actions)
        {
            // Reset all participants
            foreach ((var _, CombatAnimation animation) in _animations)
            {
                animation.ZIndex = 0;
                animation.PlayAnimation(CombatAnimation.IdleAnimation);
            }

            // Set up animation triggers
            if (action.Hit)
            {
                _animations[action.Actor].ZIndex = 1;
                _animations[action.Actor].Connect(CombatAnimation.SignalName.AttackStrike, Callable.From(() => {
                    if (action.Damage > 0)
                    {
                        HitSound.Play();
                        HitSparks.Position = _animations[action.Actor].Position + _animations[action.Actor].ContactPoint;
                        HitSparks.ZIndex = _animations[action.Actor].ZIndex + 1;
                        HitSparks.Emitting = true;
                        Camera.Trauma += CameraShakeHitTrauma;
                        _infos[action.Target].Health.Value = _infos[action.Target].Health.Value - action.Damage;
                    }
                    else
                        BlockSound.Play();
                }), (uint)ConnectFlags.OneShot);
            }
            else
            {
                _animations[action.Target].ZIndex = 1;
                _animations[action.Actor].Connect(CombatAnimation.SignalName.AttackDodged, Callable.From(() => _animations[action.Target].PlayAnimation(CombatAnimation.DodgeAnimation)), (uint)ConnectFlags.OneShot);
                _animations[action.Actor].Connect(CombatAnimation.SignalName.AttackStrike, Callable.From(() => MissSound.Play()), (uint)ConnectFlags.OneShot);
            }

            // Play the animation sequence for the turn
            _animations[action.Actor].PlayAnimation(CombatAnimation.AttackAnimation);
            await ActionCompleted(action);
            await Delay(HitDelay);
            _animations[action.Actor].PlayAnimation(CombatAnimation.AttackReturnAnimation);
            if (!action.Hit)
                _animations[action.Target].PlayAnimation(CombatAnimation.DodgeReturnAnimation);
            await ActionCompleted(action);

            // Clean up any triggers
            if (action.Hit && _infos[action.Target].Health.Value == 0)
            {
                _animations[action.Target].PlayAnimation(CombatAnimation.DieAnimation);
                DeathSound.Play();
                await ToSignal(_animations[action.Target], CombatAnimation.SignalName.AnimationFinished);
            }

            if (LeftInfo.Health.Value == 0 || RightInfo.Health.Value == 0)
                break;
            else
                await Delay(TurnDelay);
        }

        if (!_canceled)
            TransitionDelay.Start();
    }

    public void OnAccelerate()
    {
        foreach ((var _, CombatAnimation animation) in _animations)
            animation.AnimationSpeedScale = AccelerationFactor;
    }

    public void OnDecelerate()
    {
        foreach ((var _, CombatAnimation animation) in _animations)
            animation.AnimationSpeedScale = 1;
    }

    public void OnTimerTimeout()
    {
        if (!_canceled)
        {
            _canceled = true;
            SceneManager.Singleton.Connect(SceneManager.SignalName.SceneLoaded, Callable.From<Node>(_ => QueueFree()), (uint)ConnectFlags.OneShot);
            SceneManager.EndCombat();
        }
    }

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
        {
            MusicController.Resume(BackgroundMusic);
            MusicController.FadeIn(SceneManager.CurrentTransition.TransitionTime/2);
            SceneManager.Singleton.Connect(SceneManager.SignalName.TransitionCompleted, Callable.From(Start), (uint)ConnectFlags.OneShot);
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionPressed(InputActions.Cancel) && !_canceled)
        {
            TransitionDelay.Stop();
            _canceled = true;
            SceneManager.Singleton.Connect(SceneManager.SignalName.SceneLoaded, Callable.From<Node>(_ => QueueFree()), (uint)ConnectFlags.OneShot);
            SceneManager.EndCombat();
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_remaining > 0)
        {
            _remaining = Math.Max(_remaining - delta*(FastForward.Active ? AccelerationFactor : 1), 0);
            if (_remaining == 0)
                EmitSignal(SignalName.TimeExpired);
        }
    }
}