using Godot;
using Scenes.Combat.Animations;

namespace Scenes.Combat;

/// <summary>Scene used to display the results of combat in a cut scene.</summary>
public partial class CombatScene : Node
{
    private CombatAnimation _left = null, _right = null;

    public void OnTimerTimeout() => SceneManager.EndCombat();

    /// <summary>Set up the combat scene and then begin animation.</summary>
    /// <param name="left">Unit to display on the left side of the screen.</param>
    /// <param name="right">Unit to display on the right side of the screen.</param>
    public async void Start(CombatAnimation left, CombatAnimation right)
    {
        AddChild(left);
        AddChild(right);

        left.Left = true;
        left.Position  = new(44, 64);
        right.Left = false;
        right.Position = new(116, 64);

        left.Attack();
        right.Idle();
        await ToSignal(left, CombatAnimation.SignalName.AttackFinished);
        left.Idle();
        right.Attack();
        GetNode<Timer>("CombatDelay").Start();
    }
}