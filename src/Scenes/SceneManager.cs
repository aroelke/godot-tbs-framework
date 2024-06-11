using System;
using System.Collections.Immutable;
using Godot;
using Scenes.Combat;
using Scenes.Combat.Data;
using Scenes.Level.Object;
using Scenes.Transitions;

namespace Scenes;

/// <summary>Global autoloaded scene manager used to change scenes and enter or exit combat.</summary>
public partial class SceneManager : Node
{
    /// <summary>Signals that the combat cut scene has completed.</summary>
    [Signal] public delegate void CombatFinishedEventHandler();

    private static SceneManager _singleton = null;
    private static Node CurrentLevel = null;
    private static CombatScene Combat = null;

    /// <summary>Reference to the autoloaded scene manager.</summary>
    public static SceneManager Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<SceneManager>("SceneManager");

    /// <summary>Begin the combat animation by switching to the <see cref="Combat.CombatScene"/>, remembering where to return when the animation completes.</summary>
    public static void BeginCombat(Unit left, Unit right, IImmutableList<CombatAction> actions) => Singleton.DoBeginCombat(left, right, actions);

    /// <summary>End combat and return to the previous scene.</summary>
    public static void EndCombat() => Singleton.DoEndCombat();

    [Export] public double TransitionTime = 1;

    /// <summary>Scene to instantiate when displaying a combat animation.</summary>
    [Export] public PackedScene CombatScene = null;

    private async void DoBeginCombat(Unit left, Unit right, IImmutableList<CombatAction> actions)
    {
        if (CurrentLevel is not null)
            throw new InvalidOperationException("Combat has already begun.");

        await GetNode<SceneTransition>("Transitions/FadeToBlack").TransitionIn();

        CurrentLevel = Singleton.GetTree().CurrentScene;
        GetTree().Root.RemoveChild(CurrentLevel);
        Combat = Singleton.CombatScene.Instantiate<CombatScene>();
        GetTree().Root.AddChild(Combat);
        Combat.Initialize(left, right, actions);
        GetTree().CurrentScene = Combat;

        await GetNode<SceneTransition>("Transitions/FadeToBlack").TransitionOut();

        Combat.Start();
    }

    private async void DoEndCombat()
    {
        if (CurrentLevel is null)
            throw new InvalidOperationException("There is no level to return to");

        EmitSignal(SignalName.CombatFinished);

        await GetNode<SceneTransition>("Transitions/FadeToBlack").TransitionIn();

        GetTree().Root.RemoveChild(Combat);
        GetTree().Root.AddChild(CurrentLevel);
        GetTree().CurrentScene = CurrentLevel;
        CurrentLevel = null;

        await GetNode<SceneTransition>("Transitions/FadeToBlack").TransitionOut();

        Combat.QueueFree();
        Combat = null;
    }
}