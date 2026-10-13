using Deskling.Companion.Shell.Speech;
using Windows.Media.Playback;

namespace Deskling.Windows.Companion.Speech;

/// <summary>
/// Plays the app's short sounds (<c>&lt;sound&gt;.wav</c> in its sounds folder), several at once if asked, each through
/// its own <see cref="MediaPlayer"/> at the user's volume, kept out of the system media controls (Swift's
/// <c>ChimePlayer</c>). Asks for no permission. Used on the UI thread: create it there (or pass that thread's
/// <see cref="SynchronizationContext"/>), and completions run there.
/// </summary>
public sealed class ChimePlayer : IChimePlaying
{
    private readonly string folder;
    private readonly SynchronizationContext ui;
    private readonly Dictionary<int, (MediaPlayer Player, Action? Completion)> pending = [];
    private int next;

    /// <param name="folder">Where the sounds are; <c>Sounds</c> next to the app by default.</param>
    /// <param name="ui">Where completions run; the current thread's context by default.</param>
    public ChimePlayer(string? folder = null, SynchronizationContext? ui = null)
    {
        this.folder = folder ?? Path.Combine(AppContext.BaseDirectory, "Sounds");
        this.ui =
            ui
            ?? SynchronizationContext.Current
            ?? throw new InvalidOperationException("Create the chime player on the UI thread, or pass its SynchronizationContext.");
    }

    public void Play(string sound, double volume, Action? completion)
    {
        var path = Path.Combine(folder, sound + ".wav");
        if (!File.Exists(path))
        {
            completion?.Invoke();
            return;
        }
        var id = ++next;
        var player = MediaPlayers.Create(MediaPlayerAudioCategory.SoundEffects, volume, () => ui.Post(_ => Finished(id), null));
        pending[id] = (player, completion);
        player.Source = MediaPlayers.File(path);
        player.Play();
    }

    /// <summary>Cuts every sound playing; their completions never run.</summary>
    public void Stop()
    {
        foreach (var (player, _) in pending.Values)
            MediaPlayers.Release(player);
        pending.Clear();
    }

    private void Finished(int id)
    {
        if (!pending.Remove(id, out var entry))
            return;
        MediaPlayers.Release(entry.Player);
        entry.Completion?.Invoke();
    }
}
