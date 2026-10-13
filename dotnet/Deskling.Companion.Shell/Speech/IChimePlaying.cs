namespace Deskling.Companion.Shell.Speech;

/// <summary>What an app needs from its chime, so tests can play nothing (Swift's <c>ChimePlaying</c>). UI thread only.</summary>
public interface IChimePlaying
{
    /// <summary>
    /// Plays the app's sound <c>&lt;sound&gt;.wav</c>; <paramref name="completion"/> runs once it ended (or right away if
    /// it can't play), so a blip can precede a spoken line.
    /// </summary>
    void Play(string sound, double volume, Action? completion);

    void Stop();
}
