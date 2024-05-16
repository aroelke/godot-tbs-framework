using Godot;
using Nodes;
using Scenes.Combat.Animations;
using Scenes.Level.Object;

namespace Scenes.Combat;

/// <summary>Scene used to display the results of combat in a cut scene.</summary>
public partial class CombatScene : Node
{
    private NodeCache _cache;

    public CombatScene() : base() => _cache = new(this);

    private AudioStreamPlayer HitSound => _cache.GetNode<AudioStreamPlayer>("%HitSound");

    /// <summary>Position to display the left unit's sprite.</summary>
    [Export] public Vector2 Left  = new(44, 64);

    /// <summary>Position to display the right unit's sprite.</summary>
    [Export] public Vector2 Right = new(116, 64);

    public void OnTimerTimeout() => SceneManager.EndCombat();

    public void OnAttackStrike()
    {
        HitSound.Play();
    }

    /// <summary>Set up the combat scene and then begin animation.</summary>
    /// <param name="left">Unit to display on the left side of the screen.</param>
    /// <param name="right">Unit to display on the right side of the screen.</param>
    public async void Start(Unit left, Unit right)
    {
        CombatAnimation leftAnimation = left.Class.CombatAnimations.Instantiate<CombatAnimation>();
        leftAnimation.Modulate = left.Affiliation.Color;
        CombatAnimation rightAnimation = right.Class.CombatAnimations.Instantiate<CombatAnimation>();
        rightAnimation.Modulate = right.Affiliation.Color;

        AddChild(leftAnimation);
        AddChild(rightAnimation);

        leftAnimation.Left = true;
        leftAnimation.Position  = Left;
        leftAnimation.AttackStrike += OnAttackStrike;
        rightAnimation.Left = false;
        rightAnimation.Position = Right;
        rightAnimation.AttackStrike += OnAttackStrike;

        leftAnimation.ZIndex = 1;
        leftAnimation.Attack();
        rightAnimation.Idle();
        await ToSignal(leftAnimation, CombatAnimation.SignalName.Returned);
        leftAnimation.ZIndex = 0;

        rightAnimation.ZIndex = 1;
        leftAnimation.Idle();
        rightAnimation.Attack();
        await ToSignal(rightAnimation, CombatAnimation.SignalName.Returned);
        rightAnimation.ZIndex = 0;

        GetNode<Timer>("CombatDelay").Start();
    }
}