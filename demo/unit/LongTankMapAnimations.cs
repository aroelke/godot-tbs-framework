using System;
using System.Threading.Tasks;
using Godot;
using TbsFramework.Nodes.Components;

namespace TbsFramework.Demo;

public partial class LongTankMapAnimations : UnitMapAnimations
{
    private const int DigitRow = 10;
    private static readonly Vector2I QuestionCoords = new(10, DigitRow);

    private readonly NodeCache _cache = null;

    private Sprite2D          Sprite     => _cache.GetNode<Sprite2D>("Sprite");
    private Sprite2D          Inactive   => _cache.GetNode<Sprite2D>("Inactive");
    private Sprite2D          Health1    => _cache.GetNode<Sprite2D>("Health1");
    private Sprite2D          Health10   => _cache.GetNode<Sprite2D>("Health10");
    private AudioStreamPlayer DeathSound => _cache.GetNode<AudioStreamPlayer>("DeathSound");

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

    public LongTankMapAnimations() : base() { _cache = new(this); }

    public override async Task PlayAttack(Vector2I source, Vector2I target)
    {
        PlayAnimation(target - source, true);
        PropertyTweener hit = CreateTween().TweenProperty(this, new(Sprite2D.PropertyName.Position), Grid.PositionOf(target) - Grid.PositionOf(source), 0.1);
        await ToSignal(hit, PropertyTweener.SignalName.Finished);
        PropertyTweener @return = CreateTween().TweenProperty(this, new(Sprite2D.PropertyName.Position), Vector2.Zero, 0.1);
        await ToSignal(@return, PropertyTweener.SignalName.Finished);
        EmitSignal(SignalName.AnimationFinished);
    }

    public override async Task PlayDie()
    {
        PlayIdle();
        DeathSound.Play();
        PropertyTweener animation = CreateTween().TweenProperty(this, new(PropertyName.Modulate), Colors.Transparent, DeathTime);
        await ToSignal(animation, PropertyTweener.SignalName.Finished);
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