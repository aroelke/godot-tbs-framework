using System.Collections.Generic;
using System.Collections.Immutable;
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
        _animations[left] = left.Renderer.Animations;
        _animations[right] = right.Renderer.Animations;
    }

    public override async void Start()
    {
        foreach (CombatAction action in _actions)
        {
            await _animations[action.Actor].PlayAttack(action.Actor.Cell, action.Target.Cell);
            _animations[action.Actor].PlayIdle();
        }
        End();
    }

    public override void End()
    {
        EmitSignal(SignalName.CombatEnded);
    }
}