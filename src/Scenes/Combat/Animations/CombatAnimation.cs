using Godot;
using Nodes;

namespace Scenes.Combat.Animations;

/// <summary>Collection of animations to play during combat for a class or character.</summary>
[GlobalClass, Tool]
public partial class CombatAnimation : BoundedNode2D
{
    private readonly NodeCache _cache;

    public CombatAnimation() : base()
    {
        _cache = new(this);
    }

    private static readonly StringName IdleAnimation = "RESET";
    private static readonly StringName AttackAnimation = "attack";
    private static readonly StringName ReturnAnimation = "return";

    /// <summary>Signals that the current animation has completed.</summary>
    [Signal] public delegate void AnimationFinishedEventHandler();

    /// <summary>Signals that the camera should begin to shake for an animation frame.</summary>
    [Signal] public delegate void ShakeCameraEventHandler();

    /// <summary>Signals the frame in which the attack animation connects (or misses) with the opponent.</summary>
    [Signal] public delegate void AttackStrikeEventHandler();

    /// <summary>Signals that the complete animation sequence has completed and the unit is back in its idle pose.</summary>
    [Signal] public delegate void ReturnedEventHandler();

    private bool _left = true;
    private Vector2 _spriteOffset = Vector2.Zero;
    private AnimationPlayer Animations => _cache.GetNode<AnimationPlayer>("AnimationPlayer");
    private Sprite2D Sprite => _cache.GetNodeOrNull<Sprite2D>("Sprite");

    /// <summary>Whether or not the animation is on the left or right side of the screen (and facing toward the other side).</summary>
    [Export] public bool Left {
        get => _left;
        set
        {
            if (_left != value)
            {
                _left = value;
                if (Sprite is not null)
                {
                    if (_left)
                        Sprite.Offset = _spriteOffset;
                    else
                    {
                        _spriteOffset = Sprite.Offset;
                        Sprite.Offset = ReflectionOffset;
                    }
                    Sprite.FlipH = !_left;
                    Sprite.Position = Sprite.Position with { X = -Sprite.Position.X };
                }
            }
        }
    }

    /// <summary>Offset to set the sprite to when reflecting it (moving it to the right side of the screen and facing left).</summary>
    [Export] public Vector2 ReflectionOffset = Vector2.Zero;

    /// <summary>Position of the sprite of the animation relative to its initial position.</summary>
    /// <remarks>Sets <see cref="Sprite2D.Position"/>, not <see cref="Sprite2D.Offset"/>.</remarks>
    [Export] public Vector2 SpriteOffset
    {
        get => Sprite?.Position ?? Vector2.Zero;
        set
        {
            if (Sprite is not null)
                Sprite.Position = Left ? value : value with { X = -value.X };
        }
    }

    /// <summary>Set the sprite back to its idle pose.</summary>
    public void Idle() => Animations.Play(IdleAnimation);

    /// <summary>Play the attack animation.</summary>
    public void Attack() => Animations.Play(AttackAnimation);

    /// <summary>Forward the <see cref="AnimationPlayer"/>'s <see cref="AnimationMixer.SignalName.AnimationFinished"/> signal to any listeners.</summary>
    /// <param name="name">Name of the animation that completed.</param>
    public void OnAnimationFinished(StringName name) => EmitSignal(SignalName.AnimationFinished);
}