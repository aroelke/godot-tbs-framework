using Godot;

namespace UI;

public partial class MusicController : AudioStreamPlayer
{
    private static MusicController _singleton = null;

    public static MusicController Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<MusicController>("MusicController");

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

    public static new void Stop() => ((AudioStreamPlayer)Singleton).Stop();
}