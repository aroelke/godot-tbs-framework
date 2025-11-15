using System;
using System.Threading.Tasks;
using Godot;
using TbsTemplate.Nodes.Components;

namespace TbsTemplate.Demo;

public partial class LongTankCombatAnimations : CombatAnimations
{
    private readonly NodeCache _cache = null;
    private Sprite2D         Cannon       => _cache.GetNode<Sprite2D>("Cannon");
    private Sprite2D         Missile      => _cache.GetNode<Sprite2D>("Missile");
    private AnimatedSprite2D MuzzleFlash  => _cache.GetNode<AnimatedSprite2D>("MuzzleFlash");
    private AnimatedSprite2D HitExplosion => _cache.GetNode<AnimatedSprite2D>("HitExplosion");

    private Vector2 _missile = Vector2.Zero;
    private Vector2 _explosion = Vector2.Zero;

    public override Vector2 ContactPoint => throw new NotImplementedException();

    public override Rect2 BoundingBox => throw new NotImplementedException();

    public override float AnimationSpeedScale { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    [Export(PropertyHint.None, "suffix:px/s")] public double MissileSpeed = 600;

    [Export(PropertyHint.None, "suffix:px/s/s")] public double Gravity = 750;

    [Export(PropertyHint.None, "suffix:s")] public double CannonMoveTime = 0.75;

    public LongTankCombatAnimations() : base() { _cache = new(this); }

    public override Task ActionCompleted()
    {
        throw new NotImplementedException();
    }

    public override void Idle() {}

    public override void SetFacing(Vector2 direction) => Transform = new(Transform.Rotation, new(direction == Vector2.Right ? 1 : -1, 1), Transform.Skew, Transform.Origin);

    public async override Task BeginAttack(CombatAnimations target, bool hit)
    {
        _missile = Missile.Position;
        _explosion = HitExplosion.Position;

        double distance = Math.Abs(target.Position.X - Position.X);
        float theta = (float)Math.Asin(Gravity*distance/(MissileSpeed*MissileSpeed))/2;
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
            MuzzleFlash.Visible = Missile.Visible = true;
            MethodTweener animation = CreateTween().TweenMethod(Callable.From<double>(MoveMissile), 0f, duration, duration);
            animation.Finished += () => {
                EmitSignal(SignalName.AttackStrike);
                EmitSignal(SignalName.AnimationFinished);
            };
        };
        await ToSignal(this, SignalName.AnimationFinished);
    }

    public void OnMuzzleFlashFinished() => MuzzleFlash.Visible = false;

    public override async Task FinishAttack()
    {
        HitExplosion.Position = Missile.Position;
        Missile.Visible = false;
        Missile.Position = _missile;
        HitExplosion.Visible = true;
        HitExplosion.Play();

        CreateTween().TweenProperty(Cannon, new(Sprite2D.PropertyName.Rotation), 0, CannonMoveTime);

        await ToSignal(HitExplosion, AnimatedSprite2D.SignalName.AnimationFinished);
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

    public override Task BeginSupport(CombatAnimations target)
    {
        throw new NotImplementedException();
    }

    public override Task FinishSupport()
    {
        throw new NotImplementedException();
    }

    public override Task Die()
    {
        throw new NotImplementedException();
    }
}