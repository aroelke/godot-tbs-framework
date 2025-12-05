using System;
using Godot;
using TbsTemplate.Nodes.Components;

namespace TbsTemplate.Demo;

public partial class ShortTankMapAnimations : UnitMapAnimations
{
    private const int DigitRow = 10;
    private static readonly Vector2I QuestionCoords = new(10, DigitRow);

    private readonly NodeCache _cache = null;

    private Sprite2D Sprite   => _cache.GetNode<Sprite2D>("Sprite");
    private Sprite2D Inactive => _cache.GetNode<Sprite2D>("Inactive");
    private Sprite2D Health1  => _cache.GetNode<Sprite2D>("Health1");
    private Sprite2D Health10 => _cache.GetNode<Sprite2D>("Health10");

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

    public ShortTankMapAnimations() : base() { _cache = new(this); }

    public override void SetHealthValue(int value)
    {
        Health10.Visible = value > 9;
        if (value < 0 || value > 99)
            Health10.FrameCoords = Health1.FrameCoords = QuestionCoords;
        else
        {
            Health1.FrameCoords = new(value % 10, DigitRow);
            Health10.FrameCoords = new(value/10, DigitRow);
        }
    }

    public override void PlayMove(Vector2 direction) => PlayAnimation(direction, true);
    public override void PlayDone() => PlayAnimation(Vector2.Right, false);
    public override void PlayIdle() => PlayAnimation(Vector2I.Right, true);
    public override void PlaySelected() => PlayAnimation(Vector2I.Right, true);
    public override void SetHealthMax(int value) {}
}