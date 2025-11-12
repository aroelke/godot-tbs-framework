using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsTemplate.Nodes.Components;
using TbsTemplate.Scenes.Combat;
using TbsTemplate.Scenes.Combat.Data;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.UI;

namespace TbsTemplate.Demo;

public partial class DemoCombatScene : CombatScene
{
    [Signal] public delegate void TimeExpiredEventHandler();

    private readonly NodeCache _cache = null;
    private IImmutableList<CombatAction> _actions = null;
    private readonly Dictionary<Unit, CombatAnimations> _animations = [];
    private readonly Dictionary<Unit, CombatantData> _infos = [];
    private double _remaining = 0;

    private Camera2DController Camera => _cache.GetNode<Camera2DController>("Camera");

    private async Task Delay(double seconds)
    {
        _remaining = seconds;
        await ToSignal(this, SignalName.TimeExpired);
    }

    [Export] public double HitDelay = 0.3;

    [Export] public double TurnDelay = 0.2;

    [Export] public Vector2 LeftPosition = new(48, 120);

    [Export] public Vector2 RightPosition = new(272, 120);

    [Export] public double CameraShakeHitTrauma = 0.2;

    public DemoCombatScene() : base() { _cache = new(this); }

    public override void Initialize(Unit left, Unit right, IImmutableList<CombatAction> actions)
    {
        foreach (CombatAction action in actions)
            if (action.Actor != left && action.Actor != right)
                throw new ArgumentException($"CombatAction {action.Actor.Name} is not a participant in combat");
        _actions = actions;

        _animations[left] = left.Class.InstantiateCombatAnimations(left.Faction);
        _animations[left].SetFacing(Vector2.Right);
        _animations[left].Position = LeftPosition;
        _infos[left] = GetNode<CombatantData>("%LeftData");
        _infos[left].Health = left.Health;
        _infos[left].Damage = [.. _actions.Where((a) => a.Actor == left).Select(static (a) => a.Damage)];
        _infos[left].HitChance = _actions.Any((a) => a.Actor == left) ? Math.Min(CombatCalculations.HitChance(left, right), 100) : -1;
        _infos[left].TransitionDuration = HitDelay;

        _animations[right] = left.Class.InstantiateCombatAnimations(right.Faction);
        _animations[right].SetFacing(Vector2.Left);
        _animations[right].Position = RightPosition;
        _infos[right] = GetNode<CombatantData>("%RightData");
        _infos[right].Health = right.Health;
        _infos[right].Damage = [.. _actions.Where((a) => a.Actor == right).Select(static (a) => a.Damage)];
        _infos[right].HitChance = _actions.Any((a) => a.Actor == right) ? Math.Min(CombatCalculations.HitChance(right, left), 100) : -1;
        _infos[right].TransitionDuration = HitDelay;

        foreach ((_, CombatAnimations animation) in _animations)
            AddChild(animation);
    }

    public override async void Start()
    {
        foreach (CombatAction action in _actions)
        {
            foreach ((_, CombatAnimations animation) in _animations)
                animation.ZIndex = 0;
            _animations[action.Actor].ZIndex = 1;
            
            switch (action.Type)
            {
            case CombatActionType.Attack:
                await _animations[action.Actor].BeginAttack(_animations[action.Target], action.Hit);
                if (action.Hit)
                {
                    _infos[action.Target].Health.Value -= action.Damage;
                    Camera.Trauma += CameraShakeHitTrauma;
                }
                await Task.WhenAll(_animations[action.Actor].FinishAttack(), Delay(HitDelay));
                break;
            default:
                break;
            }

            await Delay(TurnDelay);
        }
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