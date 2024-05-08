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
    public void Start(CombatAnimation left, CombatAnimation right)
    {
        AddChild(left);
        AddChild(right);

        right.Position = new(GetViewport().GetVisibleRect().End.X - right.Size.X, 0);

        left.Attack(true);
        right.Attack(false);
        GetNode<Timer>("CombatDelay").Start();
    }
}