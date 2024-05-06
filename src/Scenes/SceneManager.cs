using System;
using Godot;

namespace Scenes;

/// <summary>Global autoloaded scene manager used to change scenes and enter or exit combat.</summary>
public partial class SceneManager : Node
{
    /// <summary>Signals that the combat cut scene has completed.</summary>
    [Signal] public delegate void CombatFinishedEventHandler();

    private static SceneManager _singleton = null;
    private static Node CurrentLevel = null;
    private static Node Combat = null;

    /// <summary>Reference to the autoloaded scene manager.</summary>
    public static SceneManager Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<SceneManager>("SceneManager");

    /// <summary>Begin the combat animation by switching to the <see cref="Combat.CombatScene"/>, remembering where to return when the animation completes.</summary>
    public static void BeginCombat() => Singleton.DoBeginCombat();

    /// <summary>End combat and return to the previous scene.</summary>
    public static void EndCombat() => Singleton.DoEndCombat();

    /// <summary>Scene to instantiate when displaying a combat animation.</summary>
    [Export] public PackedScene CombatScene = null;

    private void DoBeginCombat()
    {
        if (CurrentLevel is not null)
            throw new InvalidOperationException("Combat has already begun.");

        CurrentLevel = Singleton.GetTree().CurrentScene;
        GetTree().Root.RemoveChild(CurrentLevel);

        Combat = Singleton.CombatScene.Instantiate();
        GetTree().Root.AddChild(Combat);
    }

    private void DoEndCombat()
    {
        if (CurrentLevel is null)
            throw new InvalidOperationException("There is no level to return to");

        Callable.From(() => {
            Combat.Free();
            GetTree().Root.AddChild(CurrentLevel);
            GetTree().CurrentScene = CurrentLevel;
            CurrentLevel = null;
            EmitSignal(SignalName.CombatFinished);
        }).CallDeferred();
    }
}