using System;
using System.Threading.Tasks;
using Godot;
using TbsTemplate.Nodes.Components;

namespace TbsTemplate.Demo;

public partial class LongTankCombatAnimations : CombatAnimations
{
    private readonly NodeCache _cache = null;
    private Sprite2D          Cannon       => _cache.GetNode<Sprite2D>("Cannon");
    private Sprite2D          Missile      => _cache.GetNode<Sprite2D>("Missile");
    private AnimatedSprite2D  MuzzleFlash  => _cache.GetNode<AnimatedSprite2D>("MuzzleFlash");
    private AnimatedSprite2D  HitExplosion => _cache.GetNode<AnimatedSprite2D>("HitExplosion");
    private Sprite2D          HealBeam     => _cache.GetNode<Sprite2D>("HealBeam");
    private Sprite2D          HealCircle   => _cache.GetNode<Sprite2D>("HealCircle");
    private AudioStreamPlayer ShootSound   => _cache.GetNode<AudioStreamPlayer>("ShootSound");
    private AudioStreamPlayer HitSound     => _cache.GetNode<AudioStreamPlayer>("HitSound");
    private AudioStreamPlayer MissSound    => _cache.GetNode<AudioStreamPlayer>("MissSound");
    private AudioStreamPlayer HealSound    => _cache.GetNode<AudioStreamPlayer>("HealSound");
    private AudioStreamPlayer DeathSound   => _cache.GetNode<AudioStreamPlayer>("DeathSound");

    private bool _hit = true;
    private CombatAnimations _target = null;
    private Vector2 _missile = Vector2.Zero;
    private Vector2 _explosion = Vector2.Zero;
    private Vector2 _beam = Vector2.Zero;

    private double ComputeLaunchAngle(double distance) => Math.Asin(Gravity*distance/(MissileSpeed*MissileSpeed))/2;

    public override Vector2 ContactPoint => throw new NotImplementedException();
    public override Rect2 BoundingBox => throw new NotImplementedException();
    public override float AnimationSpeedScale { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    [Export(PropertyHint.None, "suffix:px/s")] public double MissileSpeed = 600;

    [Export(PropertyHint.None, "suffix:px/s/s")] public double Gravity = 750;

    [Export(PropertyHint.None, "suffix:s")] public double CannonMoveTime = 0.75;

    [Export(PropertyHint.None, "suffix:s")] public double BeamTravelTime = 1.5;

    [Export(PropertyHint.None, "suffix:s")] public double DeathTime = 0.5;

    public LongTankCombatAnimations() : base() { _cache = new(this); }

    public override Task ActionCompleted()
    {
        throw new NotImplementedException();
    }

    public override void Idle() {}

    public override void SetFacing(Vector2 direction) => Transform = new(Transform.Rotation, new(direction == Vector2.Right ? 1 : -1, 1), Transform.Skew, Transform.Origin);

    public async override Task BeginAttack(CombatAnimations target, bool hit)
    {
        _hit = hit;
        _target = target;
        _missile = Missile.Position;
        _explosion = HitExplosion.Position;

        double distance = Math.Abs(_target.Position.X - Position.X);
        float theta = (float)ComputeLaunchAngle(distance);
        Vector2 position = Missile.Position;
        Vector2 velocity = new((float)(MissileSpeed*Math.Cos(theta)), -(float)(MissileSpeed*Math.Sin(theta)));
        double duration = distance/velocity.X;

        void MoveMissile(double t)
        {
            Missile.Position = position + new Vector2((float)(velocity.X*t), (float)(Gravity*t*t/2 + velocity.Y*t));
            Missile.Transform = new((float)Math.Atan((Gravity*t + velocity.Y)/velocity.X), Missile.Transform.Origin);
        }
        CreateTween().TweenProperty(Cannon, new(Sprite2D.PropertyName.Rotation), -theta, CannonMoveTime).Finished += () => {
            MuzzleFlash.Rotation = -theta;
            MuzzleFlash.Play();
            ShootSound.Play();
            MuzzleFlash.Visible = Missile.Visible = true;
            MethodTweener animation = CreateTween().TweenMethod(Callable.From<double>(MoveMissile), 0f, duration, duration);
            animation.Finished += () => {
                CreateTween().TweenProperty(Cannon, new(Sprite2D.PropertyName.Rotation), 0, CannonMoveTime);
                EmitSignal(SignalName.AttackStrike);
                EmitSignal(SignalName.AnimationFinished);
            };
        };
        await ToSignal(this, SignalName.AnimationFinished);
    }

    public void OnMuzzleFlashFinished() => MuzzleFlash.Visible = false;

    public override async Task FinishAttack()
    {
        if (_hit)
        {
            HitExplosion.Position = Missile.Position;
            Missile.Visible = false;
            Missile.Position = _missile;
            HitExplosion.Visible = true;

            HitExplosion.Play();
            HitSound.Play();
            await ToSignal(HitExplosion, AnimatedSprite2D.SignalName.AnimationFinished);
        }
        else
        {
            float theta = (float)ComputeLaunchAngle(Math.Abs(_target.Position.X - Position.X));
            Vector2 velocity = new((float)(MissileSpeed*Math.Cos(theta)), (float)(MissileSpeed*Math.Sin(theta)));
            const double duration = 0.5;

            MissSound.Play();
            PropertyTweener animation = CreateTween().TweenProperty(Missile, new(Sprite2D.PropertyName.Position), Missile.Position + velocity*(float)duration, duration);
            animation.Finished += () => {
                Missile.Visible = false;
                Missile.Position = _missile;
                EmitSignal(SignalName.AnimationFinished);
            };
            await ToSignal(animation, PropertyTweener.SignalName.Finished);
        }
    }

    public void OnHitExplosionFinished()
    {
        HitExplosion.Visible = false;
        HitExplosion.Position = _explosion;
        EmitSignal(SignalName.AnimationFinished);
    }

    public override Task TakeHit(CombatAnimations attacker)
    {
        throw new NotImplementedException();
    }

    public override Task BeginDodge(CombatAnimations attacker)
    {
        throw new NotImplementedException();
    }

    public override Task FinishDodge()
    {
        throw new NotImplementedException();
    }

    public override async Task BeginSupport(CombatAnimations target)
    {
        _beam = HealBeam.Position;
        HealBeam.Visible = true;
        ShootSound.Play();
        PropertyTweener animation = CreateTween()
            .TweenProperty(HealBeam, new(Sprite2D.PropertyName.Position), new Vector2(target.Position.X - Position.X, HealBeam.Position.Y), BeamTravelTime)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        animation.Finished += () => {
            HealSound.Play();
            HealCircle.Position = HealBeam.Position;
            HealCircle.Visible = true;
            HealBeam.Visible = false;
            EmitSignal(SignalName.AnimationFinished);
        };
        await ToSignal(animation, PropertyTweener.SignalName.Finished);
    }

    public override Task FinishSupport()
    {
        HealBeam.Position = HealCircle.Position = _beam;
        HealCircle.Visible = false;
        return Task.CompletedTask;
    }

    public override async Task Die()
    {
        DeathSound.Play();
        PropertyTweener animation = CreateTween().TweenProperty(this, new(PropertyName.Modulate), Colors.Transparent, DeathTime);
        await ToSignal(animation, PropertyTweener.SignalName.Finished);
    }
}