using System;
using System.Threading.Tasks;
using Godot;
using TbsFramework.Nodes.Components;

namespace TbsFramework.Demo;

public partial class TankMapAnimations : UnitMapAnimations
{
    private const int DigitRow = 10;
    private static readonly Vector2I QuestionCoords = new(10, DigitRow);

    private readonly NodeCache _cache = null;
    private Vector2I _source = -Vector2I.One;
    private Vector2I _target = -Vector2I.One;
    private bool _hit = false;

    private Sprite2D          Sprite       => _cache.GetNode<Sprite2D>("Sprite");
    private Sprite2D          Inactive     => _cache.GetNode<Sprite2D>("Inactive");
    private Sprite2D          Health1      => _cache.GetNode<Sprite2D>("Health1");
    private Sprite2D          Health10     => _cache.GetNode<Sprite2D>("Health10");
    private Sprite2D          Heart        => _cache.GetNode<Sprite2D>("Heart");
    private Sprite2D          Bullet       => _cache.GetNode<Sprite2D>("Bullet");
    private AnimatedSprite2D  HitExplosion => _cache.GetNode<AnimatedSprite2D>("HitExplosion");
    private AudioStreamPlayer ShootSound   => _cache.GetNode<AudioStreamPlayer>("ShootSound");
    private AudioStreamPlayer HitSound     => _cache.GetNode<AudioStreamPlayer>("HitSound");
    private AudioStreamPlayer MissSound    => _cache.GetNode<AudioStreamPlayer>("MissSound");
    private AudioStreamPlayer HealSound    => _cache.GetNode<AudioStreamPlayer>("HealSound");
    private AudioStreamPlayer DeathSound   => _cache.GetNode<AudioStreamPlayer>("DeathSound");

    private void PlayAnimation(Vector2 direction, bool active)
    {
        Inactive.Visible = !(Sprite.Visible = active);
        double angle = Math.Atan2(direction.Y, direction.X);
        switch (angle)
        {
        case > -3*Math.PI/4 and < -Math.PI/4:
            Sprite.Transform = new((float)(-Math.PI/2), Vector2.Down*16);
            Sprite.FlipH = false;
            break;
        case >= -Math.PI/4 and < Math.PI/4:
            Sprite.Transform = new(0.0f, Vector2.Zero);
            Sprite.FlipH = false;
            break;
        case >= Math.PI/4 and < 3*Math.PI/4:
            Sprite.Transform = new((float)(-Math.PI/2), Vector2.Down*16);
            Sprite.FlipH = true;
            break;
        default:
            Sprite.Transform = new(0.0f, Vector2.Zero);
            Sprite.FlipH = true;
            break;
        };
    }

    /// <summary>Time, in seconds, the death animation takes.</summary>
    [Export(PropertyHint.None, "suffix:s")] public double DeathTime = 0.5;

    /// <summary>Speed, in world pixels per second, the bullet should travel toward its target.</summary>
    [Export(PropertyHint.None, "suffix:px/s")] public double BulletSpeed = 120;

    public TankMapAnimations() : base() { _cache = new(this); }

    public override async void BeginAttack(Vector2I source, Vector2I target, bool hit)
    {
        _source = source;
        _target = target;
        _hit = hit;
        PlayAnimation(target - source, true);
        Bullet.Transform = new((float)(Math.Atan2((source - target).Y, (source - target).X) + Math.PI), Vector2.Zero);
        Bullet.Visible = true;
        ShootSound.Play();
        PropertyTweener shoot = CreateTween().TweenProperty(Bullet, new(Sprite2D.PropertyName.Position), Grid.PositionOf(target) - Grid.PositionOf(source), Grid.PositionOf(target).DistanceTo(Grid.PositionOf(source))/BulletSpeed);
        await ToSignal(shoot, PropertyTweener.SignalName.Finished);
        EmitSignal(SignalName.AnimationFinished);
    }

    public override async void FinishAttack()
    {
        Bullet.Visible = false;
        Bullet.Position = Vector2.Zero;
        if (_hit)
        {
            HitExplosion.Visible = true;
            HitExplosion.Position = Grid.PositionOf(_target) - Grid.PositionOf(_source);
            HitExplosion.Visible = true;
            HitExplosion.Play();
            HitSound.Play();
            await ToSignal(HitExplosion, AnimatedSprite2D.SignalName.AnimationFinished);
            HitExplosion.Visible = false;
            HitExplosion.Position = Vector2.Zero;
        }
        else
        {
            MissSound.Play();
            await ToSignal(MissSound, AudioStreamPlayer.SignalName.Finished);
        }
        _source = _target = -Vector2I.One;
        _hit = false;
        EmitSignal(SignalName.AnimationFinished);
    }

    public override async void BeginSupport(Vector2I source, Vector2I target)
    {
        Heart.Visible = true;
        PropertyTweener heal = CreateTween().TweenProperty(Heart, new(Sprite2D.PropertyName.Position), Grid.PositionOf(target) - Grid.PositionOf(source), 0.4).SetEase(Tween.EaseType.Out);
        await ToSignal(heal, PropertyTweener.SignalName.Finished);
        EmitSignal(SignalName.AnimationFinished);
    }

    public override async void FinishSupport()
    {
        PropertyTweener fade = CreateTween().TweenProperty(Heart, new(Sprite2D.PropertyName.Modulate), Colors.Transparent, 0.25);
        await ToSignal(fade, PropertyTweener.SignalName.Finished);
        Heart.Visible = false;
        EmitSignal(SignalName.AnimationFinished);
    }

    public override async void PlayDie()
    {
        PlayIdle();
        DeathSound.Play();
        PropertyTweener animation = CreateTween().TweenProperty(this, new(PropertyName.Modulate), Colors.Transparent, DeathTime);
        await ToSignal(animation, PropertyTweener.SignalName.Finished);
        EmitSignal(SignalName.AnimationFinished);
    }

    public override void SetHealthValue(double value)
    {
        Health10.Visible = value > 9;
        if (value < 0 || value > 99)
            Health10.FrameCoords = Health1.FrameCoords = QuestionCoords;
        else
        {
            Health1.FrameCoords = new((int)value % 10, DigitRow);
            Health10.FrameCoords = new((int)value/10, DigitRow);
        }
    }

    public override void PlayMove(Vector2 direction) => PlayAnimation(direction, true);
    public override void PlayDone() => PlayAnimation(Vector2.Right, false);
    public override void PlayIdle() => PlayAnimation(Vector2I.Right, true);
    public override void PlaySelected() => PlayAnimation(Vector2I.Right, true);
    public override void SetHealthMax(double value) {}
}