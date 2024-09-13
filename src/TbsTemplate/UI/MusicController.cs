using System.Collections.Generic;
using Godot;

namespace TbsTemplate.UI;

/// <summary>Global controller for background music.</summary>
[Tool]
public partial class MusicController : AudioStreamPlayer
{
    [Signal] public delegate void FadeCompletedEventHandler();

    private static MusicController _singleton = null;
    private static readonly Dictionary<AudioStream, float> _positions = [];

    /// <summary>
    /// Reference to the auto-loaded music controller to help with signal connection. Other functions and properties should be accessed via
    /// static methods.
    /// </summary>
    public static MusicController Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<MusicController>("MusicController");

    /// <summary>Play a song. If the song is already playing, it won't restart or fade.</summary>
    /// <param name="music">Song to play. If <c>null</c>, the current song won't stop.</param>
    /// <param name="outDuration">Time in seconds to fade out the current track.</param>
    /// <param name="inDuration">Time in seconds to fade out the current track.</param>
    public static async void PlayTrack(AudioStream music=null, double outDuration=0, double inDuration=0)
    {
        if (music is not null)
        {
            if (Singleton.Stream != music)
            {
                FadeOut(outDuration);
                await Singleton.ToSignal(Singleton, SignalName.FadeCompleted);
            
                Singleton.Stream = music;
                Singleton.Play(_positions.GetValueOrDefault(Singleton.Stream));

                if (inDuration > 0)
                {
                    Singleton.VolumeDb = Singleton.FadeVolume;
                    Tween fade = Singleton.CreateTween();
                    fade.TweenProperty(Singleton, new(AudioStreamPlayer.PropertyName.VolumeDb), Singleton.PlayVolume, inDuration);
                }
                else if (outDuration > 0)
                    Singleton.VolumeDb = Singleton.PlayVolume;
            }
        }
        else if (Singleton.Stream is not null && !Singleton.Playing)
            Singleton.Play();
    }

    /// <summary>Change the volume of the currently-playing track over time.</summary>
    /// <param name="targetVolume">Volume, in dB, to change to.</param>
    /// <param name="duration">Time, in seconds, to fade the volume to <paramref name="targetVolume"/>.</param>
    public static void Fade(double targetVolume, double duration=0)
    {
        if (Singleton.Stream is null)
            Callable.From(() => Singleton.EmitSignal(SignalName.FadeCompleted)).CallDeferred();
        else
        {
            static void CompleteFade()
            {
                _positions[Singleton.Stream] = Singleton.GetPlaybackPosition();
                Callable.From(() => Singleton.EmitSignal(SignalName.FadeCompleted)).CallDeferred();
            }

            if (duration > 0)
            {
                Tween fade = Singleton.CreateTween();
                fade.TweenProperty(Singleton, new(AudioStreamPlayer.PropertyName.VolumeDb), targetVolume, duration);
                fade.Finished += CompleteFade;
            }
            else
                CompleteFade();
        }
    }

    /// <summary>Fade the volume out to <see cref="FadeVolume"/> over time.</summary>
    /// <param name="duration">Time, in seconds, to fade the volume out.</param>
    /// <remarks>Note that the music does not stop when the fade out is completed.</remarks>
    public static void FadeOut(double duration=0) => Fade(Singleton.FadeVolume, duration);

    /// <summary>Stop playing the current song.</summary>
    public static new void Stop() => ((AudioStreamPlayer)Singleton).Stop();

    /// <summary>Reset music playback position memory.</summary>
    /// <param name="bgm">Track whose playback position is to be forgotten. Omit or set to <c>null</c> to forget all playback positions.</param>
    public static void ResetPlayback(AudioStream bgm=null)
    {
        if (bgm is null)
            _positions.Clear();
        else
            _positions.Remove(bgm);
    }

    private float _volume = -10;

    /// <summary>Volume to play music tracks at.</summary>
    [Export(PropertyHint.None, "suffix:dB")] public float PlayVolume
    {
        get => _volume;
        set
        {
            _volume = value;
            if (Engine.IsEditorHint())
                VolumeDb = _volume;
        }
    }

    /// <summary>Volume to fade to when fading between music tracks.</summary>
    [Export(PropertyHint.None, "suffix:dB")] public float FadeVolume = -25;
}