using System.Threading.Tasks;
using Godot;

namespace TbsTemplate.Nodes.Components;

/// <summary>Collection of animations to play during combat for a class or character.</summary>
[GlobalClass, Tool]
public partial class TestSoldierCombatAnimation : CombatAnimations
{
    public static readonly StringName IdleAnimation = "RESET";
    public static readonly StringName AttackAnimation = "attack";
    public static readonly StringName AttackReturnAnimation = "attack_return";
    public static readonly StringName SupportAnimation = "support";
    public static readonly StringName SupportReturnAnimation = "support_return";
    public static readonly StringName DodgeAnimation = "dodge";
    public static readonly StringName DodgeReturnAnimation = "dodge_return";
    public static readonly StringName DieAnimation = "die";

    private bool _left = true;
    private Vector2 _spriteOffset = Vector2.Zero;
    private AnimationPlayer _animations = null;
    private Sprite2D _sprite = null;

    private void SetSpriteFacing(bool left)
    {
        if (left)
            _sprite.Offset = _spriteOffset;
        else
        {
            _spriteOffset = _sprite.Offset;
            _sprite.Offset = ReflectionOffset;
        }
        _sprite.FlipH = !left;
        _sprite.Position = _sprite.Position with { X = -_sprite.Position.X };
    }

    /// <summary>Whether or not the animation is on the left or right side of the screen (and facing toward the other side).</summary>
    [Export] public bool Left {
        get => _left;
        set
        {
            if (_left != value)
            {
                _left = value;
                if (_sprite is not null)
                    SetSpriteFacing(_left);
            }
        }
    }

    /// <summary>Offset to set the sprite to when reflecting it (moving it to the right side of the screen and facing left).</summary>
    [Export] public Vector2 ReflectionOffset = Vector2.Zero;

    /// <summary>Position of the sprite of the animation relative to its initial position.</summary>
    /// <remarks>Sets <see cref="Sprite2D.Position"/>, not <see cref="Sprite2D.Offset"/>.</remarks>
    [Export] public Vector2 SpriteOffset
    {
        get => _sprite?.Position ?? Vector2.Zero;
        set
        {
            if (_sprite is not null)
                _sprite.Position = Left ? value : value with { X = -value.X };
        }
    }

    /// <summary>Animation speed scaling ratio. Higher numbers mean faster animation, and negative numbers mean play animations backwards.</summary>
    [Export] public override float AnimationSpeedScale
    {
        get => _animations?.SpeedScale ?? 1;
        set
        {
            if (_animations is not null)
                _animations.SpeedScale = value;
        }
    }

    /// <summary>Offset on the animation where contact is made with the target during an attack, for use with hit effects.</summary>
    [Export] public Vector2 ContactOffset = new(0, 0);

    /// <summary>Point on the animation where contact is made with the target during an attack, accounting for which way it's facing.</summary>
    public override Vector2 ContactPoint => Left ? ContactOffset : ContactOffset with { X = -ContactOffset.X };
    public override void SetFacing(Vector2 direction) => Left = direction == Vector2.Left;
    public override void Idle() => PlayAnimation(IdleAnimation);
    public override void BeginAttack(CombatAnimations target) => PlayAnimation(AttackAnimation);
    public override void FinishAttack() => PlayAnimation(AttackReturnAnimation);
    public override void TakeHit(CombatAnimations attacker) {}
    public override void BeginDodge(CombatAnimations attacker) => PlayAnimation(DodgeAnimation);
    public override void FinishDodge() => PlayAnimation(DodgeReturnAnimation);
    public override void BeginSupport(CombatAnimations target) => PlayAnimation(SupportAnimation);
    public override void FinishSupport() => PlayAnimation(SupportReturnAnimation);
    public override void Die() => PlayAnimation(DieAnimation);

    /// <summary>Play a combat animation.</summary>
    /// <param name="name">Name of the animation to play.</param>
    public void PlayAnimation(StringName name) => _animations.Play(name);

    /// <summary>
    /// Forward the <see cref="AnimationPlayer"/>'s <see cref="AnimationMixer.SignalName.AnimationFinished"/> signal to any listeners. Also plays the\
    /// idle animation when returning from an action.
    /// </summary>
    /// <param name="name">Name of the animation that completed.</param>
    public void OnAnimationFinished(StringName name)
    {
        if (name == AttackReturnAnimation || name == DodgeReturnAnimation)
            _animations.Play(IdleAnimation);
        EmitSignal(SignalName.AnimationFinished);
    }

    /// <summary>If a non-idle animation is playing, wait for it to end.</summary>
    public override async Task ActionCompleted()
    {
        if (_animations.CurrentAnimation != IdleAnimation && _animations.IsPlaying())
            await ToSignal(_animations, AnimationPlayer.SignalName.AnimationFinished);
    }

    public override void _Ready()
    {
        base._Ready();

        _animations = GetNode<AnimationPlayer>("AnimationPlayer");
        _sprite = GetNode<Sprite2D>("Sprite");
        if (Left)
            _spriteOffset = _sprite.Offset;

        if (!Engine.IsEditorHint())
            _sprite.Material = (Material)_sprite.Material.Duplicate();
        SetSpriteFacing(Left);
    }
}