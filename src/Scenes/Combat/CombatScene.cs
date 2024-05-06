using Godot;

namespace Scenes.Combat;

/// <summary>Scene used to display the results of combat in a cut scene.</summary>
public partial class CombatScene : Node
{
    public void OnTimerTimeout() => SceneManager.EndCombat();
}