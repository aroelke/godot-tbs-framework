using System;
using Godot;
using TbsTemplate.Nodes.Components;

namespace TbsTemplate.Demo;

public partial class TestSoldierMapAnimations : UnitMapAnimations
{
    private static readonly StringName IdleAnimation     = "idle";
    private static readonly StringName UpAnimation       = "up";
    private static readonly StringName RightAnimation    = "right";
    private static readonly StringName DownAnimation     = "down";
    private static readonly StringName LeftAnimation     = "left";
    private static readonly StringName SelectedAnimation = "selected";
    private static readonly StringName DoneAnimation     = "done";

    private readonly NodeCache _cache = null;
    private TextureProgressBar HealthBar => _cache.GetNode<TextureProgressBar>("%Health");
    private AudioStreamPlayer  StepSound => _cache.GetNode<AudioStreamPlayer>("StepSound");
    private AnimationPlayer    Player    => _cache.GetNode<AnimationPlayer>("AnimationPlayer");
    private Timer              StepTimer => _cache.GetNode<Timer>("StepTimer");

    private void StartAnimation(StringName animation)
    {
        if (Player.CurrentAnimation != animation)
            Player.Play(animation);
    }

    public TestSoldierMapAnimations() : base() { _cache = new(this); }

    public override void PlayIdle()
    {
        StepTimer.Stop();
        StartAnimation(IdleAnimation);
    }

    public override void PlaySelected()
    {
        StepTimer.Stop();
        StartAnimation(SelectedAnimation);
    }

    public override void PlayDone()
    {
        StepTimer.Stop();
        StartAnimation(DoneAnimation);
    }

    public override void PlayMove(Vector2 direction)
    {
        if (direction == Vector2.Zero)
        {
            GD.PushWarning("Unit is moving in a zero direction.");
            StartAnimation(IdleAnimation);
        }
        else
        {
            double angle = Math.Atan2(direction.Y, direction.X);
            StartAnimation(angle switch
            {
                >  -3*Math.PI/4 and <  -Math.PI/4 => UpAnimation,
                >=   -Math.PI/4 and <   Math.PI/4 => RightAnimation,
                >=    Math.PI/4 and < 3*Math.PI/4 => DownAnimation,
                _                                   => LeftAnimation
            });
        }
        if (StepTimer.TimeLeft == 0)
        {
            StepSound.Play();
            StepTimer.Start();
        }
    }

    public override void SetHealthValue(int value) => HealthBar.Value = value;
    public override void SetHealthMax(int value) => HealthBar.MaxValue = value;
}