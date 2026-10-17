namespace Laching.Companion.Shell.Speech;

/// <summary>
/// What an app needs from a voice, so tests can speak silently (Swift's <c>Speaking</c>). Used on the UI thread only.
/// </summary>
public interface ISpeaking
{
    bool IsSpeaking { get; }

    /// <summary>Called once a line finished or was stopped.</summary>
    Action? OnFinished { get; set; }

    /// <summary>
    /// Plays <paramref name="clip"/> (the path of a recorded voice's file) when given, else says <paramref name="text"/>
    /// with the system voice. <paramref name="rate"/> and <paramref name="volume"/> are the settings' 0…1 scales.
    /// </summary>
    void Speak(string text, string? clip, string? voiceIdentifier, double rate, double volume);

    void Stop();

    /// <summary>How far into the playing clip it is, in the clip's own seconds; null when no clip plays (the system voice).</summary>
    double? ClipTime => null;
}

/// <summary>The settings' speech scales onto a player's, the same on every platform.</summary>
public static class SpeechRates
{
    /// <summary>
    /// The settings' 0…1 scale onto a recorded clip's playback rate: 0.5 plays it as recorded, and the ends stay close
    /// (0.85×–1.2×) so the time stretch keeps the voice natural (the player keeps the pitch).
    /// </summary>
    public static double ClipRate(double unit)
    {
        unit = Math.Clamp(unit, 0, 1);
        return unit <= 0.5 ? 0.85 + 0.15 * (unit / 0.5) : 1 + 0.2 * ((unit - 0.5) / 0.5);
    }
}
