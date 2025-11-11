using System;
using System.Threading.Tasks;
using Godot;
using TbsTemplate.Nodes;
using TbsTemplate.Nodes.Components;

namespace TbsTemplate.Demo;

public partial class ShortTankCombatAnimations : CombatAnimations
{
    private readonly NodeCache _cache = null;
    private Sprite2D         Bullet       => _cache.GetNode<Sprite2D>("Bullet");
    private AnimatedSprite2D MuzzleFlash  => _cache.GetNode<AnimatedSprite2D>("MuzzleFlash");
    private AnimatedSprite2D HitExplosion => _cache.GetNode<AnimatedSprite2D>("HitExplosion");

    private CombatAnimations _target = null;
    private Vector2 _explosion = Vector2.Zero;

    public override Vector2 ContactPoint => throw new NotImplementedException();
    public override Rect2 BoundingBox => _cache.GetNode<BoundedNode2D>("BoundingBox").BoundingBox;
    public override float AnimationSpeedScale { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    [Export(PropertyHint.None, "suffix:px")] public float BulletSpeed = 600;

    public ShortTankCombatAnimations() : base()  { _cache = new(this); }

    public override void SetFacing(Vector2 direction) => Transform = new(Transform.Rotation, new(direction == Vector2.Right ? 1 : -1, 1), Transform.Skew, Transform.Origin);

    public override async Task ActionCompleted()
    {
        await ToSignal(this, SignalName.AnimationFinished);
    }

    public override void Idle() {}

    public override async Task BeginAttack(CombatAnimations target)
    {
        float distance = Math.Abs(target.Position.X - Position.X);
        Vector2 initial = Bullet.Position;
        _target = target;
        _explosion = HitExplosion.Position;

        MuzzleFlash.Play();
        MuzzleFlash.Visible = true;
        Bullet.Visible = true;
        PropertyTweener animation = CreateTween().TweenProperty(Bullet, new(Sprite2D.PropertyName.Position), Bullet.Position + Vector2.Right*distance, distance/BulletSpeed);
        animation.Finished += () => {
            Bullet.Visible = false;
            HitExplosion.Position = Bullet.Position;
            Bullet.Position = initial;
            EmitSignal(SignalName.AttackStrike);
            EmitSignal(SignalName.AnimationFinished);
        };
        await ToSignal(animation, PropertyTweener.SignalName.Finished);
    }

    public void OnMuzzleFlashFinished() => MuzzleFlash.Visible = false;

    public override async Task FinishAttack()
    {
        HitExplosion.Visible = true;
        HitExplosion.Play();
        await ToSignal(HitExplosion, AnimatedSprite2D.SignalName.AnimationFinished);
    }

    public void OnHitExplosionFinished()
    {
        HitExplosion.Visible = false;
        HitExplosion.Position = _explosion;
        EmitSignal(SignalName.AnimationFinished);
    }

    public override Task BeginDodge(CombatAnimations attacker) { throw new System.NotImplementedException(); }

    public override Task BeginSupport(CombatAnimations target)
    {
        throw new System.NotImplementedException();
    }

    public override Task FinishDodge()
    {
        throw new System.NotImplementedException();
    }

    public override Task FinishSupport()
    {
        throw new System.NotImplementedException();
    }

    public override Task TakeHit(CombatAnimations attacker) { return Task.CompletedTask; }

    public override Task Die()
    {
        throw new System.NotImplementedException();
    }
}