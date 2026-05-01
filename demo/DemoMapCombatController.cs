using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes.Combat;
using TbsFramework.Scenes.Data;
using TbsFramework.UI.Controls.Device;

namespace TbsFramework.Demo;

public partial class DemoMapCombatController : CombatController
{
    private double _remaining = 0;
    private Dictionary<UnitData, UnitMapAnimations> _animations = [];
    private readonly Queue<(Action action, GodotObject actor, StringName signal, double delay)> _actions = [];
    private GodotObject _lastActor = null;
    private StringName _lastSignal = null;
    private Callable _lastAction;
    private bool _canceled = true;

    private void ExecuteNextAction()
    {
        void next(double delay)
        {
            _lastActor = null;
            _lastSignal = null;
            _lastAction = default;
            if (!_canceled)
            {
                if (delay <= 0)
                    ExecuteNextAction();
                else
                    _remaining = delay;
            }
        }

        if (_actions.Count > 0)
        {
            (Action action, GodotObject actor, StringName signal, double delay) = _actions.Dequeue();
            action();
            if (actor is not null && signal is not null)
            {
                _lastActor = actor;
                _lastSignal = signal;
                _lastAction = Callable.From(() => next(delay));
                actor.Connect(signal, _lastAction, (uint)ConnectFlags.OneShot);
            }
            else
                next(delay);
        }
        else
            End();
    }

    /// <summary>Time, in seconds, after a combat action has completed to wait until beginning the next one.</summary>
    [Export(PropertyHint.None, "suffix:s")] public double TurnDelay = 0.2;

    public override void Initialize(UnitData left, UnitData right, IImmutableList<CombatAction> actions)
    {
        base.Initialize(left, right, actions);

        _animations = new()
        {
            { left,  left.Renderer.Animations  },
            { right, right.Renderer.Animations }
        };
        _canceled = false;

        Dictionary<UnitData, double> damage = _animations.Keys.ToDictionary((k) => k, _ => 0.0);
        foreach (CombatAction action in actions)
        {
            double dmg = 0;
            _actions.Enqueue((() => _animations[action.Actor].ZIndex = 1, this, null, 0));
            switch (action.Type)
            {
            case CombatActionType.Attack:
                _actions.Enqueue((() => _animations[action.Actor].BeginAttack(action.Actor.Cell, action.Target.Cell, action.Hit), _animations[action.Actor], UnitMapAnimations.SignalName.AnimationFinished, 0));
                if (action.Hit)
                {
                    dmg = damage[action.Target] += action.Damage;
                    _actions.Enqueue((() => _animations[action.Target].SetHealthValue(Math.Max(0, action.Target.Health - dmg)), this, null, 0));
                }
                _actions.Enqueue((() => _animations[action.Actor].FinishAttack(), _animations[action.Actor], UnitMapAnimations.SignalName.AnimationFinished, 0));
                break;
            case CombatActionType.Support:
                _actions.Enqueue((() => _animations[action.Actor].BeginSupport(action.Actor.Cell, action.Target.Cell), _animations[action.Actor], UnitMapAnimations.SignalName.AnimationFinished, 0));
                dmg = damage[action.Target] += action.Damage;
                _actions.Enqueue((() => {
                    _animations[action.Target].SetHealthValue(Math.Max(0, action.Target.Health - dmg));
                    _animations[action.Actor].FinishSupport();
                }, _animations[action.Actor], UnitMapAnimations.SignalName.AnimationFinished, 0));
                break;
            default:
                break;
            }
            _actions.Enqueue((() => {
                _animations[action.Actor].ZIndex = 0;
                _animations[action.Actor].PlayIdle();
            }, this, null, 0));

            foreach ((UnitData unit, UnitMapAnimations animations) in _animations)
            {
                if (damage[unit] >= unit.Health)
                    _actions.Enqueue((animations.PlayDie, animations, UnitMapAnimations.SignalName.AnimationFinished, 0));
            }

            _actions.Enqueue((() => {}, this, null, TurnDelay));
        }
    }

    public override void Start() => ExecuteNextAction();

    public override void End()
    {
        _actions.Clear();
        if (_lastActor is not null)
        {
            _lastActor.Disconnect(_lastSignal, _lastAction);
            _lastActor = null;
            _lastSignal = null;
            _lastAction = default;
        }
        if (!_canceled)
        {
            _canceled = true;
            foreach ((_, UnitMapAnimations animations) in _animations)
                animations.CancelAnimation();
            EmitSignal(SignalName.CombatEnded);
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event.IsActionPressed(InputManager.Cancel))
            End();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_remaining > 0)
        {
            _remaining -= delta;
            if (!_canceled && _remaining <= 0)
                ExecuteNextAction();
        }
    }
}