using System;
using Godot;
using TbsTemplate.Nodes.Components;

namespace TbsTemplate.Demo;

public partial class ShortTankMapAnimations : UnitMapAnimations
{
    private readonly NodeCache _cache = null;

    private Sprite2D Sprite => _cache.GetNode<Sprite2D>("Sprite");
    private Sprite2D Health => _cache.GetNode<Sprite2D>("Health");

    public ShortTankMapAnimations() : base() { _cache = new(this); }

    public override void PlayMove(Vector2 direction)
    {
        double angle = Math.Atan2(direction.Y, direction.X);
        switch (angle)
        {
          case >  -3*Math.PI/4 and <  -Math.PI/4:
            Sprite.Transform = Sprite.Transform.Rotated(-90).Translated(Vector2.Down*16);
            Sprite.FlipH = false;
            break;
          case >=   -Math.PI/4 and <   Math.PI/4:
            Sprite.Transform = new();
            Sprite.FlipH = false;
            break;
          case >=    Math.PI/4 and < 3*Math.PI/4:
            Sprite.Transform = Sprite.Transform.Rotated(-90).Translated(Vector2.Down*16);
            Sprite.FlipH = true;
            break;
          default:
            Sprite.Transform = new();
            Sprite.FlipH = true;
            break;
        };
}

    public override void PlayDone() => PlayMove(Vector2I.Right);
    public override void PlayIdle() => PlayMove(Vector2I.Right);
    public override void PlaySelected() => PlayMove(Vector2I.Right);
    public override void SetHealthMax(int value) {}
    public override void SetHealthValue(int value) => Health.FrameCoords = new(value > 9 || value < 0 ? 10 : value, 10);
}