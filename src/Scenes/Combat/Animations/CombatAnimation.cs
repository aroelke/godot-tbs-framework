using Godot;
using Nodes;

namespace Scenes.Combat.Animations;

[GlobalClass, Tool]
public partial class CombatAnimation : BoundedNode2D
{
    private readonly NodeCache _cache;

    public CombatAnimation() : base()
    {
        _cache = new(this);
    }

    private static readonly StringName IdleLeft = "idle_left";
    private static readonly StringName IdleRight = "idle_right";
    private static readonly StringName AttackLeft = "attack_left";
    private static readonly StringName AttackRight = "attack_right";

    [Signal] public delegate void ShakeCameraEventHandler();

    [Signal] public delegate void AttackStrikeEventHandler();

    private AnimationPlayer Animations => _cache.GetNode<AnimationPlayer>("AnimationPlayer");

    [Export] public bool Left = false;

    public void Idle(bool left) => Animations.Play(left ? IdleLeft : IdleRight);
    public void Idle() => Idle(Left);

    public async void Attack(bool left)
    {
        ZIndex = 1;
        Animations.Play(left ? AttackLeft : AttackRight);
        await ToSignal(Animations, AnimationPlayer.SignalName.AnimationFinished);
        ZIndex = 0;
    }
    public void Attack() => Attack(Left);
}