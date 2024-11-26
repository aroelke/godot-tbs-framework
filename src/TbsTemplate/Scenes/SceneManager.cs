using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Godot;
using TbsTemplate.Scenes.Combat;
using TbsTemplate.Scenes.Combat.Data;
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
    [Signal] public delegate void SceneLoadedEventHandler(Node scene);

    private static SceneManager _singleton = null;
    private static readonly Stack<Node> _history = new();

    /// <summary>Reference to the autoloaded scene manager.</summary>
    public static SceneManager Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<SceneManager>("SceneManager");

    /// <summary>Currently-running scene transition, or the one that will run next scene change if the scene isn't changing.</summary>
    public static SceneTransition CurrentTransition => Singleton.FadeToBlack;

    /// <summary>Load a new scene and change to it with transition.</summary>
    /// <param name="path">Path pointing to the scene file to load.</param>
    public static void ChangeScene(string path)
    {
        _history.Push(Singleton.GetTree().CurrentScene);
        Singleton.DoBeginTransition(() => GD.Load<PackedScene>(path).Instantiate<Node>());
    }

    /// <summary>End combat and return to the previous scene.</summary>
    public static void EndCombat() => Singleton.DoEndCombat();

    private async void DoSceneChange<T>(Task<T> task) where T : Node
    {
        // Wait for completion of the task loading the next scene
        T target = await task;
        EmitSignal(SignalName.SceneLoaded, target);

        // Switch to the next scene
        GetTree().Root.RemoveChild(GetTree().CurrentScene);
        GetTree().Root.AddChild(target);
        GetTree().CurrentScene = target;
        CurrentTransition.TransitionIn();
    }

    private void DoBeginTransition<T>(Func<T> gen) where T : Node
    {
        Task<T> task = Task.Run(gen);
        EmitSignal(SignalName.TransitionStarted);
        MusicController.FadeOut(CurrentTransition.TransitionTime/2);
        CurrentTransition.Connect(SceneTransition.SignalName.TransitionedOut, Callable.From(() => DoSceneChange(task)), (uint)ConnectFlags.OneShot);
        CurrentTransition.TransitionOut();
    }

    private void DoEndCombat()
    {
        if (!_history.TryPop(out Node target))
            throw new InvalidOperationException("There is no level to return to");
        DoBeginTransition(() => target);
        CurrentTransition.TransitionOut();
    }

    public void OnTransitionedIn() => EmitSignal(SignalName.TransitionCompleted);
}