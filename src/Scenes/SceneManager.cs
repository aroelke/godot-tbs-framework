using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
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

    /// <summary>Signals that the combat cut scene has completed.</summary>
    [Signal] public delegate void CombatFinishedEventHandler();

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

    private SceneTransition FadeToBlack => _cache.GetNode<SceneTransition>("Transitions/FadeToBlack");

    /// <summary>Scene to instantiate when displaying a combat animation.</summary>
    [Export] public PackedScene CombatScene = null;

    private async Task DoSceneTransition(Node target)
    {
        await FadeToBlack.TransitionIn();

        GetTree().Root.RemoveChild(GetTree().CurrentScene);
        GetTree().Root.AddChild(target);
        GetTree().CurrentScene = target;

        await FadeToBlack.TransitionOut();

        EmitSignal(SignalName.TransitionCompleted);
    }

    private async void DoBeginCombat(Unit left, Unit right, IImmutableList<CombatAction> actions)
    {
        if (CurrentLevel is not null)
            throw new InvalidOperationException("Combat has already begun.");

        Combat = Singleton.CombatScene.Instantiate<CombatScene>();
        Combat.Initialize(left, right, actions);
        CurrentLevel = Singleton.GetTree().CurrentScene;
        static void PlayCombatBGM() => MusicController.Play(Combat.BackgroundMusic);

        FadeToBlack.TransitionFinished += PlayCombatBGM;
        await DoSceneTransition(Combat);
        FadeToBlack.TransitionFinished -= PlayCombatBGM;

        Combat.Start();
    }

    private async void DoEndCombat()
    {
        if (CurrentLevel is null)
            throw new InvalidOperationException("There is no level to return to");

        EmitSignal(SignalName.CombatFinished);
        static void PlayLevelBGM() => MusicController.Play(CurrentLevel.GetNode<LevelManager>("LevelManager").BackgroundMusic);

        FadeToBlack.TransitionFinished += PlayLevelBGM;
        await DoSceneTransition(CurrentLevel);
        FadeToBlack.TransitionFinished -= PlayLevelBGM;

        CurrentLevel = null;
        Combat.QueueFree();
        Combat = null;
    }
}