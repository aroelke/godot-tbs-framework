using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
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

        _animations[right] = left.Class.InstantiateCombatAnimations(right.Faction);
        _animations[right].SetFacing(Vector2.Left);
        _animations[right].Position = RightPosition;

        foreach ((_, CombatAnimations animation) in _animations)
            AddChild(animation);
    }

    public override async void Start()
    {
        foreach (CombatAction action in _actions)
        {
            foreach ((_, CombatAnimations animation) in _animations)
                animation.ZIndex = 0;
            
            switch (action.Type)
            {
            case CombatActionType.Attack:
                _animations[action.Actor].BeginAttack(_animations[action.Target]);
                await _animations[action.Actor].ActionCompleted();
                break;
            default:
                break;
            }
        }
    }
}