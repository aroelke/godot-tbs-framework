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
    public static void ChangeScene(string path) => Singleton.DoBeginTransition(() => GD.Load<PackedScene>(path).Instantiate<Node>());

    /// <summary>Begin the combat animation by switching to the <see cref="CombatScene"/>, remembering where to return when the animation completes.</summary>
    public static void BeginCombat(Unit left, Unit right, IImmutableList<CombatAction> actions)
    {
        if (_currentLevel is not null)
            throw new InvalidOperationException("Combat has already begun.");

        _currentLevel = Singleton.GetTree().CurrentScene;
        Singleton.DoBeginTransition(() => _combat = CombatScene.Instantiate(left, right, actions));
    }

    /// <summary>End combat and return to the previous scene.</summary>
    public static void EndCombat() => Singleton.DoEndCombat();

    private async void DoSceneChange<T>(Task<T> task) where T : Node
    {
        // Wait for completion of the task loading the next scene
        T target = await task;

        // Switch to the next scene
        GetTree().Root.RemoveChild(GetTree().CurrentScene);
        GetTree().Root.AddChild(target);
        GetTree().CurrentScene = target;
        FadeToBlack.TransitionIn();

        // Next frame, signal that the scene has loaded. _EnterTree in that scene will have already run,
        // but not _Ready
        EmitSignal(SignalName.SceneLoaded, target, FadeToBlack.TransitionTime/2);
    }

    private void DoBeginTransition<T>(Func<T> gen) where T : Node
    {
        Task<T> task = Task.Run(gen);
        EmitSignal(SignalName.TransitionStarted);
        MusicController.FadeOut(FadeToBlack.TransitionTime/2);
        FadeToBlack.Connect(SceneTransition.SignalName.TransitionedOut, Callable.From(() => DoSceneChange(task)), (uint)ConnectFlags.OneShot);
        FadeToBlack.TransitionOut();
    }

    private void DoEndCombat()
    {
        if (_currentLevel is null)
            throw new InvalidOperationException("There is no level to return to");
        Node target = _currentLevel;
        _currentLevel = null;

        DoBeginTransition(() => target);
        FadeToBlack.Connect(SceneTransition.SignalName.TransitionedOut, Callable.From(() => {
            MusicController.Resume(target.GetNode<LevelManager>("LevelManager").BackgroundMusic);
            MusicController.FadeIn(FadeToBlack.TransitionTime/2);
            _combat.QueueFree();
            _combat = null;
        }), (uint)ConnectFlags.OneShot);
        FadeToBlack.TransitionOut();
    }

    public void OnTransitionedIn() => EmitSignal(SignalName.TransitionCompleted);
}