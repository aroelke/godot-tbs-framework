using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes.Combat;
using TbsFramework.Scenes.Data;

public partial class DemoMapCombatController : CombatController
{
    [Signal] public delegate void TimeExpiredEventHandler();

    private double _remaining = 0;
    private IImmutableList<CombatAction> _actions = null;
    private Dictionary<UnitData, UnitMapAnimations> _animations = [];

    private async Task Delay(double seconds)
    {
        _remaining = seconds;
        await ToSignal(this, SignalName.TimeExpired);
    }

    /// <summary>Time, in seconds, after an attack connects to wait until beginning the next attack.</summary>
    [Export(PropertyHint.None, "suffix:s")] public double HitDelay = 0.3;

    /// <summary>Time, in seconds, after a combat action has completed to wait until beginning the next one.</summary>
    [Export(PropertyHint.None, "suffix:s")] public double TurnDelay = 0.2;

    public override void Initialize(UnitData left, UnitData right, IImmutableList<CombatAction> actions)
    {
        base.Initialize(left, right, actions);

        _actions = actions;
        _animations = new()
        {
            { left,  left.Renderer.Animations  },
            { right, right.Renderer.Animations }
        };
    }

    public override async void Start()
    {
        Dictionary<UnitData, double> damage = _animations.Keys.ToDictionary((k) => k, _ => 0.0);

        foreach (CombatAction action in _actions)
        {
            _animations[action.Actor].ZIndex = 1;
            switch (action.Type)
            {
            case CombatActionType.Attack:
                _animations[action.Actor].BeginAttack(action.Actor.Cell, action.Target.Cell, action.Hit);
                await ToSignal(_animations[action.Actor], UnitMapAnimations.SignalName.AnimationFinished);
                if (action.Hit)
                {
                    damage[action.Target] += action.Damage;
                    _animations[action.Target].SetHealthValue(Math.Max(0, action.Target.Health - damage[action.Target]));
                }
                _animations[action.Actor].FinishAttack();
                await Task.WhenAll(this.AwaitSignal(_animations[action.Actor], UnitMapAnimations.SignalName.AnimationFinished), Delay(HitDelay));
                break;
            case CombatActionType.Support:
                _animations[action.Actor].BeginSupport(action.Actor.Cell, action.Target.Cell);
                await ToSignal(_animations[action.Actor], UnitMapAnimations.SignalName.AnimationFinished);
                damage[action.Target] += action.Damage;
                _animations[action.Target].SetHealthValue(Math.Max(0, action.Target.Health - damage[action.Target]));
                _animations[action.Actor].FinishSupport();
                await Task.WhenAll(this.AwaitSignal(_animations[action.Actor], UnitMapAnimations.SignalName.AnimationFinished), Delay(HitDelay));
                break;
            default:
                break;
            }
            _animations[action.Actor].ZIndex = 0;
            _animations[action.Actor].PlayIdle();

            bool defeated = false;
            foreach ((UnitData unit, UnitMapAnimations animations) in _animations)
            {
                if (damage[unit] >= unit.Health)
                {
                    defeated = true;
                    animations.PlayDie();
                    await ToSignal(animations, UnitMapAnimations.SignalName.AnimationFinished);
                }
            }
            if (defeated)
                break;
        }
        End();
    }

    public override void End()
    {
        EmitSignal(SignalName.CombatEnded);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_remaining > 0)
        {
            _remaining -= delta;
            if (_remaining <= 0)
                EmitSignal(SignalName.TimeExpired);
        }
    }
}