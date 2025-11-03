using System;
using Godot;
using TbsTemplate.Nodes.Components;

namespace TbsTemplate.Demo;

public partial class ShortTankMapAnimations : UnitMapAnimations
{
    private readonly NodeCache _cache = null;

    private Sprite2D Sprite   => _cache.GetNode<Sprite2D>("Sprite");
    private Sprite2D Inactive => _cache.GetNode<Sprite2D>("Inactive");
    private Sprite2D Health   => _cache.GetNode<Sprite2D>("Health");

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

    public override void PlayMove(Vector2 direction) => PlayAnimation(direction, true);
    public override void PlayDone() => PlayAnimation(Vector2.Right, false);
    public override void PlayIdle() => PlayAnimation(Vector2I.Right, true);
    public override void PlaySelected() => PlayAnimation(Vector2I.Right, true);
    public override void SetHealthMax(int value) {}
    public override void SetHealthValue(int value) => Health.FrameCoords = new(value > 9 || value < 0 ? 10 : value, 10);
}