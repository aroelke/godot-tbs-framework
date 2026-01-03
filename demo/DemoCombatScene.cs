using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes;
using TbsFramework.Scenes.Combat;
using TbsFramework.Scenes.Combat.Data;
using TbsFramework.Scenes.Level.Object;
using TbsFramework.UI;
using TbsFramework.UI.Controls.Device;

namespace TbsFramework.Demo;

/// <summary>Example combat scene to display the results of two demo units engaging each other.</summary>
public partial class DemoCombatScene : CombatScene
{
    [Signal] public delegate void TimeExpiredEventHandler();

    private readonly NodeCache _cache = null;
    private IImmutableList<CombatAction> _actions = null;
    private readonly Dictionary<UnitData, CombatAnimations> _animations = [];
    private readonly Dictionary<UnitData, CombatantData> _infos = [];
    private double _remaining = 0;
    private bool _canceled = false;

    private Camera2DController Camera          => _cache.GetNode<Camera2DController>("Camera");
    private Timer              TransitionDelay => _cache.GetNode<Timer>("TransitionDelay");

    private async Task Delay(double seconds)
    {
        _remaining = seconds;
        await ToSignal(this, SignalName.TimeExpired);
    }

    /// <summary>Time, in seconds, after an attack connects to wait until beginning the next attack.</summary>
    [Export(PropertyHint.None, "suffix:s")] public double HitDelay = 0.3;

    /// <summary>Time, in seconds, after a combat action has completed to wait until beginning the next one.</summary>
    [Export(PropertyHint.None, "suffix:s")] public double TurnDelay = 0.2;

    /// <summary>Position of the combatant on the left side of the screen.</summary>
    [Export(PropertyHint.None, "suffix:px")] public Vector2 LeftPosition = new(48, 120);

    /// <summary>Position of the combatant on the right side of the screen.</summary>
    [Export(PropertyHint.None, "suffix:px")] public Vector2 RightPosition = new(272, 120);

    /// <summary>Magnitude of the camera shake when an attack connects.</summary>
    [Export] public double CameraShakeHitTrauma = 0.2;

    public DemoCombatScene() : base() { _cache = new(this); }

    public override void Initialize(UnitData left, UnitData right, IImmutableList<CombatAction> actions)
    {
        foreach (CombatAction action in actions)
            if (action.Actor != left && action.Actor != right)
                throw new ArgumentException($"Unit at cell {action.Actor.Cell} is not a participant in combat");
        _actions = actions;

        _animations[left] = left.Class.InstantiateCombatAnimations(left.Faction);
        _animations[left].SetFacing(Vector2.Right);
        _animations[left].Position = LeftPosition;
        _infos[left] = GetNode<CombatantData>("%LeftData");
        _infos[left].Health.Maximum = left.Stats.Health;
        _infos[left].Health.Value = left.Health;
        _infos[left].Damage = [.. _actions.Where((a) => a.Actor == left).Select(static (a) => (int)a.Damage)];
        _infos[left].HitChance = _actions.Any((a) => a.Actor == left) ? Math.Min(CombatCalculations.HitChance(left, right), 100) : -1;
        _infos[left].TransitionDuration = HitDelay;

        _animations[right] = right.Class.InstantiateCombatAnimations(right.Faction);
        _animations[right].SetFacing(Vector2.Left);
        _animations[right].Position = RightPosition;
        _infos[right] = GetNode<CombatantData>("%RightData");
        _infos[right].Health.Maximum = right.Stats.Health;
        _infos[right].Health.Value = right.Health;
        _infos[right].Damage = [.. _actions.Where((a) => a.Actor == right).Select(static (a) => (int)a.Damage)];
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
                    _infos[action.Target].Health.Value -= (int)action.Damage;
                    Camera.Trauma += CameraShakeHitTrauma;
                }
                await Task.WhenAll(_animations[action.Actor].FinishAttack(), Delay(HitDelay));
                break;
            case CombatActionType.Support:
                await _animations[action.Actor].BeginSupport(_animations[action.Target]);
                _infos[action.Target].Health.Value -= (int)action.Damage;
                await Delay(HitDelay);
                await _animations[action.Actor].FinishSupport();
                break;
            default:
                break;
            }

            foreach ((UnitData unit, CombatantData data) in _infos)
            {
                if (data.Health.Value <= 0)
                {
                    await Delay(HitDelay);
                    await _animations[unit].Die();
                }
            }
            await Delay(TurnDelay);
        }

        if (!_canceled)
            TransitionDelay.Start();
    }

    public override void End()
    {
        if (!_canceled)
        {
            TransitionDelay.Stop();
            _canceled = true;
            SceneManager.Singleton.Connect<Node>(SceneManager.SignalName.SceneLoaded, _ => QueueFree(), (uint)ConnectFlags.OneShot);
            SceneManager.ReturnToPreviousScene();
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
            if (_remaining <= 0)
                EmitSignal(SignalName.TimeExpired);
        }
    }
}