using Godot;
using Nodes;
using Scenes.Combat.Animations;
using Scenes.Level.Object;

namespace Scenes.Combat;

/// <summary>Scene used to display the results of combat in a cut scene.</summary>
public partial class CombatScene : Node
{
    private readonly NodeCache _cache;
    public CombatScene() : base() => _cache = new(this);

    private static bool leftMiss = false, rightMiss = true;

    private AudioStreamPlayer HitSound => _cache.GetNode<AudioStreamPlayer>("%HitSound");
    private AudioStreamPlayer MissSound => _cache.GetNode<AudioStreamPlayer>("%MissSound");

    /// <summary>Position to display the left unit's sprite.</summary>
    [Export] public Vector2 Left  = new(44, 64);

    /// <summary>Position to display the right unit's sprite.</summary>
    [Export] public Vector2 Right = new(116, 64);

    public void OnTimerTimeout()
    {
        leftMiss = !leftMiss;
        rightMiss = !rightMiss;
        SceneManager.EndCombat();
    }

    public void OnAttackStrike(bool miss)
    {
        if (miss)
            MissSound.Play();
        else
            HitSound.Play();
    }

    /// <summary>Set up the combat scene and then begin animation.</summary>
    /// <param name="left">Unit to display on the left side of the screen.</param>
    /// <param name="right">Unit to display on the right side of the screen.</param>
    public void Start(Unit left, Unit right)
    {
        CombatAnimation leftAnimation = left.Class.CombatAnimations.Instantiate<CombatAnimation>();
        leftAnimation.Modulate = left.Affiliation.Color;
        CombatAnimation rightAnimation = right.Class.CombatAnimations.Instantiate<CombatAnimation>();
        rightAnimation.Modulate = right.Affiliation.Color;

        AddChild(leftAnimation);
        AddChild(rightAnimation);

        leftAnimation.Left = true;
        leftAnimation.Position = Left;
        leftAnimation.AttackStrike += () => OnAttackStrike(leftMiss);
        if (leftMiss)
        {
            leftAnimation.AttackDodged += () => {
                rightAnimation.ZIndex = 2;
                rightAnimation.Dodge();
            };
            leftAnimation.Returning += rightAnimation.DodgeReturn;
        }

        rightAnimation.Left = false;
        rightAnimation.Position = Right;
        rightAnimation.AttackStrike += () => OnAttackStrike(rightMiss);
        if (rightMiss)
        {
            rightAnimation.AttackDodged += () => {
                leftAnimation.ZIndex = 2;
                leftAnimation.Dodge();
            };
            rightAnimation.Returning += leftAnimation.DodgeReturn;
        }

        static void InitiateAttack(CombatAnimation attacker, CombatAnimation defender)
        {
            attacker.ZIndex = 1;
            attacker.Attack();
            defender.ZIndex = 0;
            defender.Idle();
        }

        InitiateAttack(leftAnimation, rightAnimation);
        leftAnimation.Returned += () => InitiateAttack(rightAnimation, leftAnimation);
        rightAnimation.Returned += () => GetNode<Timer>("CombatDelay").Start();
    }
}