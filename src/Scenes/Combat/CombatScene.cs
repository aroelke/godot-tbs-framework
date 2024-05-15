using Godot;
using Nodes;
using Scenes.Combat.Animations;

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
    public async void Start(CombatAnimation left, CombatAnimation right)
    {
        AddChild(left);
        AddChild(right);

        left.Left = true;
        left.Position  = Left;
        left.AttackStrike += OnAttackStrike;
        right.Left = false;
        right.Position = Right;
        right.AttackStrike += OnAttackStrike;

        left.Attack();
        right.Idle();
        await ToSignal(left, CombatAnimation.SignalName.AttackFinished);
        left.Idle();
        right.Attack();
        GetNode<Timer>("CombatDelay").Start();
    }
}