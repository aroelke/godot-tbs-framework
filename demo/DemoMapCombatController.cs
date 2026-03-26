using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes.Combat;
using TbsFramework.Scenes.Data;

public partial class DemoMapCombatController : CombatController
{
    private IImmutableList<CombatAction> _actions = null;
    private Dictionary<UnitData, UnitMapAnimations> _animations = [];

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
            await _animations[action.Actor].PlayAttack(action.Actor.Cell, action.Target.Cell);
            damage[action.Target] += action.Damage;
            _animations[action.Target].SetHealthValue(Math.Max(0, action.Target.Health - damage[action.Target]));
            _animations[action.Actor].ZIndex = 0;
            _animations[action.Actor].PlayIdle();

            bool defeated = false;
            foreach ((UnitData unit, UnitMapAnimations animations) in _animations)
            {
                if (damage[unit] >= unit.Health)
                {
                    defeated = true;
                    await animations.PlayDie();
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
}