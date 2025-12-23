using System;
using System.Threading.Tasks;
using Godot;
using TbsFramework.Nodes;
using TbsFramework.Nodes.Components;

namespace TbsFramework.Demo;

/// <summary>Example demo class to use for most enemy units.</summary>
public partial class ShortTankCombatAnimations : CombatAnimations
{
    private readonly NodeCache _cache = null;
    private Sprite2D          Bullet       => _cache.GetNode<Sprite2D>("Bullet");
    private AnimatedSprite2D  MuzzleFlash  => _cache.GetNode<AnimatedSprite2D>("MuzzleFlash");
    private AnimatedSprite2D  HitExplosion => _cache.GetNode<AnimatedSprite2D>("HitExplosion");
    private AudioStreamPlayer ShootSound   => _cache.GetNode<AudioStreamPlayer>("ShootSound");
    private AudioStreamPlayer HitSound     => _cache.GetNode<AudioStreamPlayer>("HitSound");
    private AudioStreamPlayer MissSound    => _cache.GetNode<AudioStreamPlayer>("MissSound");
    private AudioStreamPlayer DeathSound   => _cache.GetNode<AudioStreamPlayer>("DeathSound");

    private CombatAnimations _target = null;
    private Vector2 _bullet = Vector2.Zero;
    private Vector2 _explosion = Vector2.Zero;
    private bool _hit = true;

    public override Vector2 ContactPoint => throw new NotImplementedException();
    public override Rect2 BoundingBox => _cache.GetNode<BoundedNode2D>("BoundingBox").BoundingBox;
    public override float AnimationSpeedScale { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    /// <summary>Speed the projectile travels across the screen when it's fired.</summary>
    [Export(PropertyHint.None, "suffix:px/s")] public float BulletSpeed = 600;

    /// <summary>Distance to continue moving the projectile if the attack is a miss. Should cause it to end up off screen.</summary>
    [Export(PropertyHint.None, "suffix:px")] public float OvershootDistance = 100;

    /// <summary>Time, in seconds, the death animation takes.</summary>
    [Export(PropertyHint.None, "suffix:s")] public double DeathTime = 0.5;

    public ShortTankCombatAnimations() : base()  { _cache = new(this); }

    public override void SetFacing(Vector2 direction) => Transform = new(Transform.Rotation, new(direction == Vector2.Right ? 1 : -1, 1), Transform.Skew, Transform.Origin);

    public override async Task ActionCompleted()
    {
        await ToSignal(this, SignalName.AnimationFinished);
    }

    public override async Task BeginAttack(CombatAnimations target, bool hit)
    {
        float distance = Math.Abs(target.Position.X - Position.X);
        _target = target;
        _bullet = Bullet.Position;
        _explosion = HitExplosion.Position;
        _hit = hit;

        MuzzleFlash.Play();
        ShootSound.Play();
        MuzzleFlash.Visible = Bullet.Visible = true;
        PropertyTweener animation = CreateTween().TweenProperty(Bullet, new(Sprite2D.PropertyName.Position), Bullet.Position + Vector2.Right*distance, distance/BulletSpeed);
        animation.Finished += () => {
            EmitSignal(SignalName.AttackStrike);
            EmitSignal(SignalName.AnimationFinished);
        };
        await ToSignal(animation, PropertyTweener.SignalName.Finished);
    }

    public void OnMuzzleFlashFinished() => MuzzleFlash.Visible = false;

    public override async Task FinishAttack()
    {
        if (_hit)
        {
            Bullet.Visible = false;
            HitExplosion.Position = Bullet.Position;
            Bullet.Position = _bullet;
            HitExplosion.Visible = true;
            HitExplosion.Play();
            HitSound.Play();
            await ToSignal(HitExplosion, AnimatedSprite2D.SignalName.AnimationFinished);
        }
        else
        {
            MissSound.Play();
            PropertyTweener animation = CreateTween().TweenProperty(Bullet, new(Sprite2D.PropertyName.Position), Bullet.Position + Vector2.Right*OvershootDistance, OvershootDistance/BulletSpeed);
            animation.Finished += () => {
                Bullet.Visible = false;
                Bullet.Position = MuzzleFlash.Position;
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

    public override async Task Die()
    {
        DeathSound.Play();
        PropertyTweener animation = CreateTween().TweenProperty(this, new(PropertyName.Modulate), Colors.Transparent, DeathTime);
        await ToSignal(animation, PropertyTweener.SignalName.Finished);
    }

    public override void Idle() => throw new NotImplementedException();

    // Tanks miss their shots rather than dodging and don't react to hits
    public override Task BeginDodge(CombatAnimations attacker) => throw new NotImplementedException();
    public override Task FinishDodge() => throw new NotImplementedException();
    public override Task TakeHit(CombatAnimations attacker) => throw new NotImplementedException();

    // This class can't support its allies
    public override Task BeginSupport(CombatAnimations target) => throw new NotImplementedException();
    public override Task FinishSupport() => throw new NotImplementedException();
}