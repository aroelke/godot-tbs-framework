using Godot;

namespace UI;

/// <summary>Global controller for background music.</summary>
public partial class MusicController : AudioStreamPlayer
{
    private static MusicController _singleton = null;

    /// <summary>
    /// Reference to the auto-loaded music controller to help with signal connection. Other functions and properties should be accessed via
    /// static methods.
    /// </summary>
    public static MusicController Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<MusicController>("MusicController");

    /// <summary>Play a song. If the song is already playing, it won't restart or fade.</summary>
    /// <param name="music">Song to play. If <c>null</c>, the current song won't stop.</param>
    /// <param name="outDuration">Time in seconds to fade out the current track.</param>
    /// <param name="inDuration">Time in seconds to fade out the current track.</param>
    public static async void Play(AudioStream music=null, double outDuration=0, double inDuration=0)
    {
        if (music is not null)
        {
            if (Singleton.Stream != music)
            {
                float volume = Singleton.VolumeDb;

                if (outDuration > 0)
                {
                    Tween fade = Singleton.CreateTween();
                    fade.TweenProperty(Singleton, new(AudioStreamPlayer.PropertyName.VolumeDb), Singleton.FadeVolume, outDuration);
                    await Singleton.ToSignal(fade, Tween.SignalName.Finished);
                }

                Singleton.Stream = music;
                ((AudioStreamPlayer)_singleton).Play();

                if (inDuration > 0)
                {
                    Singleton.VolumeDb = Singleton.FadeVolume;
                    Tween fade = Singleton.CreateTween();
                    fade.TweenProperty(Singleton, new(AudioStreamPlayer.PropertyName.VolumeDb), volume, inDuration);
                }
                else if (outDuration > 0)
                    Singleton.VolumeDb = volume;
            }
        }
        else if (Singleton.Stream is not null && !Singleton.Playing)
            ((AudioStreamPlayer)_singleton).Play();
    }

    /// <summary>Stop playing the current song.</summary>
    public static new void Stop() => ((AudioStreamPlayer)Singleton).Stop();

    [Export(PropertyHint.None, "suffix:dB")] public float FadeVolume = -20;
}