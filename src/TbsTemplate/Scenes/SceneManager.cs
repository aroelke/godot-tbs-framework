using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using TbsTemplate.Scenes.Combat;
using TbsTemplate.Scenes.Combat.Data;
using TbsTemplate.Scenes.Level;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Transitions;
using TbsTemplate.UI;

namespace TbsTemplate.Scenes;

/// <summary>Global autoloaded scene manager used to change scenes and enter or exit combat.</summary>
[SceneTree]
public partial class SceneManager : Node
{
    /// <summary>Signals that a transition to a new scene has begun.</summary>
    [Signal] public delegate void TransitionStartedEventHandler();

    /// <summary>Signals that a transition to a new scene has completed.</summary>
    [Signal] public delegate void TransitionCompletedEventHandler();

    /// <summary>Signals that a scene has finished loading.</summary>
    /// <param name="scene">Scene that finished loading.</param>
    /// <param name="time">Time, in seconds, to transition into the new scene.</param>
    [Signal] public delegate void SceneLoadedEventHandler(Node scene, int time);

    private static SceneManager _singleton = null;
    private static Node _currentLevel = null;
    private static CombatScene _combat = null;

    /// <summary>Reference to the autoloaded scene manager.</summary>
    public static SceneManager Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<SceneManager>("SceneManager");

    /// <summary>Load a new scene and change to it with transition.</summary>
    /// <param name="path">Path pointing to the scene file to load.</param>
    public static void ChangeScene(string path) => Singleton.BeginFade(() => GD.Load<PackedScene>(path).Instantiate<Node>());

    /// <summary>Begin the combat animation by switching to the <see cref="CombatScene"/>, remembering where to return when the animation completes.</summary>
    public static void BeginCombat(Unit left, Unit right, IImmutableList<CombatAction> actions) => Singleton.DoBeginCombat(left, right, actions);

    /// <summary>End combat and return to the previous scene.</summary>
    public static void EndCombat() => Singleton.DoEndCombat();

    private void GoToScene(Node target)
    {
        GetTree().Root.RemoveChild(GetTree().CurrentScene);
        GetTree().Root.AddChild(target);
        GetTree().CurrentScene = target;
        FadeToBlack.TransitionIn();
    }

    private async void CompleteFade<T>(Task<T> task) where T : Node
    {
        T target = await task;
        EmitSignal(SignalName.SceneLoaded, target, FadeToBlack.TransitionTime/2);
        GoToScene(target);
    }

    private void BeginFade<T>(Func<T> gen) where T : Node
    {
        Task<T> task = Task.Run(gen);
        EmitSignal(SignalName.TransitionStarted);
        MusicController.FadeOut(FadeToBlack.TransitionTime/2);
        FadeToBlack.Connect(SceneTransition.SignalName.TransitionedOut, Callable.From(() => CompleteFade(task)), (uint)ConnectFlags.OneShot);
        FadeToBlack.TransitionOut();
    }

    private void DoSceneTransition(Node target, AudioStream bgm)
    {
        EmitSignal(SignalName.TransitionStarted);
        MusicController.PlayTrack(bgm, outDuration:FadeToBlack.TransitionTime/2, inDuration:FadeToBlack.TransitionTime/2);
        FadeToBlack.Connect(SceneTransition.SignalName.TransitionedOut, Callable.From(() => GoToScene(target)), (uint)ConnectFlags.OneShot);
        FadeToBlack.TransitionOut();
    }

    private void DoBeginCombat(Unit left, Unit right, IImmutableList<CombatAction> actions)
    {
        if (_currentLevel is not null)
            throw new InvalidOperationException("Combat has already begun.");

        _currentLevel = Singleton.GetTree().CurrentScene;
        BeginFade(() => _combat = CombatScene.Instantiate(left, right, actions));
    }

    private void DoEndCombat()
    {
        if (_currentLevel is null)
            throw new InvalidOperationException("There is no level to return to");

        FadeToBlack.Connect(SceneTransition.SignalName.TransitionedOut, Callable.From(() => {
            _combat.QueueFree();
            _combat = null;
        }), (uint)ConnectFlags.OneShot);
        DoSceneTransition(_currentLevel, _currentLevel.GetNode<LevelManager>("LevelManager").BackgroundMusic);
        _currentLevel = null;
    }

    public void OnTransitionedIn() => EmitSignal(SignalName.TransitionCompleted);
}