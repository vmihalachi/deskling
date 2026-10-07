namespace Deskling.Windows.Shell;

/// <summary>
/// Plays a <c>.wav</c> file through <c>PlaySound</c>, asynchronously and without falling back to the default beep.
/// Windows' own sounds live in <see cref="WindowsMediaFolder"/>; which ones to use is the app's choice.
/// </summary>
public static class WavPlayer
{
    /// <summary><c>C:\Windows\Media</c>: the sound scheme's files.</summary>
    public static string WindowsMediaFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media");

    /// <summary>Starts the file and returns at once; false when it doesn't exist or can't be played.</summary>
    public static bool Play(string path)
    {
        if (!File.Exists(path))
            return false;
        return NativeMethods.PlaySoundW(
            path,
            0,
            NativeMethods.SND_ASYNC | NativeMethods.SND_FILENAME | NativeMethods.SND_NODEFAULT
        );
    }

    /// <summary>Stops whatever <see cref="Play"/> started.</summary>
    public static void Stop() => NativeMethods.PlaySoundW(null, 0, 0);
}
