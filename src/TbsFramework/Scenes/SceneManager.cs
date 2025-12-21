using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Transitions;
using TbsFramework.UI;

namespace TbsFramework.Scenes;

/// <summary>Global autoloaded scene manager used to change scenes and enter or exit combat.</summary>
public partial class SceneManager : Node
{
    /// <summary>Signals that a transition to a new scene has begun.</summary>
    [Signal] public delegate void TransitionStartedEventHandler();

    /// <summary>Signals that a transition to a new scene has completed.</summary>
    [Signal] public delegate void TransitionCompletedEventHandler();

    /// <summary>Signals that a scene has finished loading.</summary>
    /// <param name="scene">Scene that finished loading.</param>
    [Signal] public delegate void SceneLoadedEventHandler(Node scene);

    private static readonly Stack<Node> _history = new();

    /// <summary>Reference to the autoloaded scene manager.</summary>
    public static SceneManager Singleton => AutoloadNodes.GetNode<SceneManager>("SceneManager");

    /// <summary>Currently-running scene transition, or the one that will run next scene change if the scene isn't changing.</summary>
    public static SceneTransition CurrentTransition => Singleton.FadeToBlack;

    /// <summary>Load a new scene and change to it with transition without saving history.</summary>
    /// <param name="path">Path pointing to the scene file to load.</param>
    public static void JumpToScene(string path) => Singleton.DoBeginTransition(() => GD.Load<PackedScene>(path).Instantiate<Node>());

    /// <summary>Load a new scene and change to it with transition, saving the previous scene to return to later.</summary>
    /// <param name="path">Path pointing to the scene file to load.</param>
    public static void CallScene(string path)
    {
        _history.Push(Singleton.GetTree().CurrentScene);
        JumpToScene(path);
    }

    /// <summary>Change to the previous scene in the history with transition.</summary>
    /// <exception cref="InvalidOperationException">If there is no scene to return to or the previous scene is invalid.</exception>
    public static void ReturnToPreviousScene()
    {
        if (!_history.TryPop(out Node prev))
            throw new InvalidOperationException("No previous scene to return to");
        if (!IsInstanceValid(prev))
            throw new InvalidOperationException("Previous scene is null or freed");
        Singleton.DoBeginTransition(() => prev);
    }

    private SceneTransition _fade = null;
    private SceneTransition FadeToBlack => _fade ??= GetNode<SceneTransition>("%FadeToBlack");

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
        CurrentTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => DoSceneChange(task), (uint)ConnectFlags.OneShot);
        CurrentTransition.TransitionOut();
    }

    public void OnTransitionedIn() => EmitSignal(SignalName.TransitionCompleted);
}