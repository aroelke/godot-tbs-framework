using System.Threading.Tasks;
using Godot;
using Nodes;

namespace Scenes.Combat.Animations;

/// <summary>Collection of animations to play during combat for a class or character.</summary>
[GlobalClass, Tool]
public partial class CombatAnimation : BoundedNode2D
{
    private readonly NodeCache _cache;
    public CombatAnimation() : base() => _cache = new(this);

    public static readonly StringName IdleAnimation = "RESET";
    public static readonly StringName AttackAnimation = "attack";
    public static readonly StringName AttackReturnAnimation = "attack_return";
    public static readonly StringName DodgeAnimation = "dodge";
    public static readonly StringName DodgeReturnAnimation = "dodge_return";
    public static readonly StringName DieAnimation = "die";

    /// <summary>Signals that the current animation has completed.</summary>
    [Signal] public delegate void AnimationFinishedEventHandler();

    [Signal] public delegate void StepTakenEventHandler();

    /// <summary>Signals that the camera should begin to shake for an animation frame.</summary>
    [Signal] public delegate void ShakeCameraEventHandler();

    /// <summary>Signals that the attack is about to land, so the opponent's dodge animation should begin now.</summary>
    /// <remarks>Currently, all dodge animations are expected to be 0.1 seconds long, so this fires 0.1 seconds before the attack lands.</remarks>
    [Signal] public delegate void AttackDodgedEventHandler();

    /// <summary>Signals the frame in which the attack animation connects (or misses) with the opponent.</summary>
    [Signal] public delegate void AttackStrikeEventHandler();

    private bool _left = true;
    private Vector2 _spriteOffset = Vector2.Zero;
    private AnimationPlayer Animations => _cache.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
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

    /// <summary>Animation speed scaling ratio. Higher numbers mean faster animation, and negative numbers mean play animations backwards.</summary>
    [Export] public float AnimationSpeedScale
    {
        get => Animations?.SpeedScale ?? 1;
        set
        {
            if (Animations is not null)
                Animations.SpeedScale = value;
        }
    }

    /// <summary>Offset on the animation where contact is made with the target during an attack, for use with hit effects.</summary>
    [Export] public Vector2 ContactOffset = new(0, 0);

    /// <summary>Point on the animation where contact is made with the target during an attack, accounting for which way it's facing.</summary>
    public Vector2 ContactPoint => Left ? ContactOffset : ContactOffset with { X = -ContactOffset.X };

    /// <summary>Play a combat animation.</summary>
    /// <param name="name">Name of the animation to play.</param>
    public void PlayAnimation(StringName name) => Animations.Play(name);

    /// <summary>
    /// Forward the <see cref="AnimationPlayer"/>'s <see cref="AnimationMixer.SignalName.AnimationFinished"/> signal to any listeners. Also plays the\
    /// idle animation when returning from an action.
    /// </summary>
    /// <param name="name">Name of the animation that completed.</param>
    public void OnAnimationFinished(StringName name)
    {
        if (name == AttackReturnAnimation || name == DodgeReturnAnimation)
            Animations.Play(IdleAnimation);
        EmitSignal(SignalName.AnimationFinished);
    }

    /// <summary>If a non-idle animation is playing, wait for it to end.</summary>
    public async Task ActionFinished()
    {
        if (Animations.CurrentAnimation != IdleAnimation && Animations.IsPlaying())
            await ToSignal(Animations, AnimationPlayer.SignalName.AnimationFinished);
    }

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
            Sprite.Material = (Material)Sprite.Material.Duplicate();
    }
}