using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsTemplate.Nodes.Components;
using TbsTemplate.Scenes.Combat;
using TbsTemplate.Scenes.Combat.Data;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Demo;

public partial class DemoCombatScene : CombatScene
{
    private IImmutableList<CombatAction> _actions = null;
    private readonly Dictionary<Unit, CombatAnimations> _animations = [];
    private readonly Dictionary<Unit, CombatantData> _infos = [];

    [Export] public double HitDelay = 0.3;

    [Export] public Vector2 LeftPosition = new(48, 120);

    [Export] public Vector2 RightPosition = new(272, 120);

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
                await _animations[action.Actor].BeginAttack(_animations[action.Target]);
                _infos[action.Target].Health.Value -= action.Damage;
                await _animations[action.Actor].FinishAttack();
                break;
            default:
                break;
            }
        }
    }
}