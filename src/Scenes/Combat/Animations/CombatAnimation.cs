using Godot;
using Nodes;

namespace Scenes.Combat.Animations;

[GlobalClass, Tool]
public partial class CombatAnimation : Node2D
{
    private readonly NodeCache _cache;

    public CombatAnimation() : base()
    {
        _cache = new(this);
    }

    private static readonly StringName AttackLeft = "attack_left";
    private static readonly StringName AttackRight = "attack_right";

    [Signal] public delegate void ShakeCameraEventHandler();

    [Signal] public delegate void AttackStrikeEventHandler();

    private AnimationPlayer Animations => _cache.GetNode<AnimationPlayer>("AnimationPlayer");

    public void Attack(bool left) => Animations.Play(left ? AttackLeft : AttackRight);
}