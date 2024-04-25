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

    /// <summary>Play a song. If the song is already playing, it won't restart.</summary>
    /// <param name="music">Song to play. If <c>null</c>, the current song won't stop.</param>
    public static void Play(AudioStream music=null)
    {
        if (music is not null)
        {
            if (Singleton.Stream != music)
            {
                Singleton.Stream = music;
                ((AudioStreamPlayer)_singleton).Play();
            }
        }
        else if (Singleton.Stream is not null && !Singleton.Playing)
            ((AudioStreamPlayer)_singleton).Play();
    }

    /// <summary>Stop playing the current song.</summary>
    public static new void Stop() => ((AudioStreamPlayer)Singleton).Stop();
}