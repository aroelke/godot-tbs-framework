using System;
using System.Collections.Immutable;
using Godot;
using Nodes;
using Scenes.Combat;
using Scenes.Combat.Data;
using Scenes.Level;
using Scenes.Level.Object;
using Scenes.Transitions;
using UI;

namespace Scenes;

/// <summary>Global autoloaded scene manager used to change scenes and enter or exit combat.</summary>
public partial class SceneManager : Node
{
    private readonly NodeCache _cache = null;
    public SceneManager() : base() => _cache = new(this);

    /// <summary>Signals that a transition to a new scene has begun.</summary>
    [Signal] public delegate void TransitionStartedEventHandler();

    /// <summary>Signals that a transition to a new scene has completed.</summary>
    [Signal] public delegate void TransitionCompletedEventHandler();

    private static SceneManager _singleton = null;
    private static Node CurrentLevel = null;
    private static CombatScene Combat = null;

    /// <summary>Reference to the autoloaded scene manager.</summary>
    public static SceneManager Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<SceneManager>("SceneManager");

    /// <summary>Begin the combat animation by switching to the <see cref="Combat.CombatScene"/>, remembering where to return when the animation completes.</summary>
    public static void BeginCombat(Unit left, Unit right, IImmutableList<CombatAction> actions) => Singleton.DoBeginCombat(left, right, actions);

    /// <summary>End combat and return to the previous scene.</summary>
    public static void EndCombat() => Singleton.DoEndCombat();

    private Node _target = null;

    private SceneTransition FadeToBlack => _cache.GetNode<SceneTransition>("Transitions/FadeToBlack");

    /// <summary>Scene to instantiate when displaying a combat animation.</summary>
    [Export] public PackedScene CombatScene = null;

    private void DoSceneTransition(Node target, AudioStream bgm)
    {
        _target = target;
        EmitSignal(SignalName.TransitionStarted);
        MusicController.PlayTrack(bgm, outDuration:FadeToBlack.TransitionTime/2, inDuration:FadeToBlack.TransitionTime/2);
        FadeToBlack.TransitionOut();
    }

    private void DoBeginCombat(Unit left, Unit right, IImmutableList<CombatAction> actions)
    {
        if (CurrentLevel is not null)
            throw new InvalidOperationException("Combat has already begun.");

        Combat = Singleton.CombatScene.Instantiate<CombatScene>();
        Combat.Initialize(left, right, actions);
        CurrentLevel = Singleton.GetTree().CurrentScene;

        FadeToBlack.TransitionedIn += Combat.Start;
        DoSceneTransition(Combat, Combat.BackgroundMusic);
    }

    private void DoEndCombat()
    {
        if (CurrentLevel is null)
            throw new InvalidOperationException("There is no level to return to");

        void CleanUp()
        {
            Combat.QueueFree();
            Combat = null;
            FadeToBlack.TransitionedOut -= CleanUp;
        }

        FadeToBlack.TransitionedOut += CleanUp;
        FadeToBlack.TransitionedIn -= Combat.Start;
        DoSceneTransition(CurrentLevel, CurrentLevel.GetNode<LevelManager>("LevelManager").BackgroundMusic);
        CurrentLevel = null;
    }

    public void OnTransitionedOut()
    {
        GetTree().Root.RemoveChild(GetTree().CurrentScene);
        GetTree().Root.AddChild(_target);
        GetTree().CurrentScene = _target;
        FadeToBlack.TransitionIn();
    }

    public void OnTransitionedIn()
    {
        _target = null;
        EmitSignal(SignalName.TransitionCompleted);
    }
}