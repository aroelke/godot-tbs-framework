using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Godot;
using TbsTemplate.Scenes.Combat;
using TbsTemplate.Scenes.Combat.Data;
using TbsTemplate.Scenes.Level;
using TbsTemplate.Scenes.Level.Object;
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

    private static SceneManager _singleton = null;
    private static Node _currentLevel = null;
    private static CombatScene _combat = null;

    /// <summary>Reference to the autoloaded scene manager.</summary>
    public static SceneManager Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<SceneManager>("SceneManager");

    /// <summary>Begin the combat animation by switching to the <see cref="Combat.CombatScene"/>, remembering where to return when the animation completes.</summary>
    public static void BeginCombat(Unit left, Unit right, IImmutableList<CombatAction> actions) => Singleton.DoBeginCombat(left, right, actions);

    /// <summary>End combat and return to the previous scene.</summary>
    public static void EndCombat() => Singleton.DoEndCombat();

    private Node _target = null;

    private void DoSceneTransition(Node target, AudioStream bgm)
    {
        _target = target;
        EmitSignal(SignalName.TransitionStarted);
        MusicController.PlayTrack(bgm, outDuration:FadeToBlack.TransitionTime/2, inDuration:FadeToBlack.TransitionTime/2);
        FadeToBlack.TransitionOut();
    }

    private void DoBeginCombat(Unit left, Unit right, IImmutableList<CombatAction> actions)
    {
        if (_currentLevel is not null)
            throw new InvalidOperationException("Combat has already begun.");

        var task = Task.Run(() => CombatScene.Instantiate(left, right, actions));
        _currentLevel = Singleton.GetTree().CurrentScene;

        async void CompleteFade()
        {
            _target = _combat = await task;
            FadeToBlack.TransitionedIn += _combat.Start;
            MusicController.Resume(_combat.BackgroundMusic);
            MusicController.FadeIn(FadeToBlack.TransitionTime/2);
            FadeToBlack.TransitionedOut -= CompleteFade;
        }
        FadeToBlack.TransitionedOut += CompleteFade;
        EmitSignal(SignalName.TransitionStarted);
        MusicController.FadeOut(FadeToBlack.TransitionTime/2);
        FadeToBlack.TransitionOut();
    }

    private void DoEndCombat()
    {
        if (_currentLevel is null)
            throw new InvalidOperationException("There is no level to return to");

        void CleanUp()
        {
            _combat.QueueFree();
            _combat = null;
            FadeToBlack.TransitionedOut -= CleanUp;
        }

        FadeToBlack.TransitionedOut += CleanUp;
        FadeToBlack.TransitionedIn -= _combat.Start;
        DoSceneTransition(_currentLevel, _currentLevel.GetNode<LevelManager>("LevelManager").BackgroundMusic);
        _currentLevel = null;
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