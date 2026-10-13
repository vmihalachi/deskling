using Windows.Media.Core;
using Windows.Media.Playback;

namespace Deskling.Windows.Companion.Speech;

/// <summary>The <see cref="MediaPlayer"/>s the speaker and the chime play through.</summary>
internal static class MediaPlayers
{
    /// <summary>
    /// A player for one sound, kept out of the system media controls (no "now playing" card in the volume flyout, no
    /// media keys), that reports its end or failure through <paramref name="ended"/>, off the UI thread.
    /// </summary>
    public static MediaPlayer Create(MediaPlayerAudioCategory category, double volume, Action ended)
    {
        var player = new MediaPlayer { AudioCategory = category, Volume = Math.Clamp(volume, 0, 1) };
        player.CommandManager.IsEnabled = false;
        player.MediaEnded += (_, _) => ended();
        player.MediaFailed += (_, _) => ended();
        return player;
    }

    /// <summary>A file on disk as a media source.</summary>
    public static MediaSource File(string path) => MediaSource.CreateFromUri(new Uri(Path.GetFullPath(path)));

    /// <summary>Stops and lets go of a player; its late events are the owner's to ignore.</summary>
    public static void Release(MediaPlayer player)
    {
        player.Pause();
        player.Source = null;
        player.Dispose();
    }
}
