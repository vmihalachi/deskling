namespace Deskling.Companion.Shell.Speech;

/// <summary>
/// Runs work later on the UI thread: the beat before a punchline, a silent part's bubble time, the busy watch, the
/// shell's ticks. The Windows shell implements it over a <c>DispatcherQueueTimer</c>; tests run a hand-driven one
/// (<c>Deskling.Testing.ManualTimers</c>). Disposing the returned handle cancels work that hasn't run yet.
/// </summary>
public interface ITimers
{
    /// <summary>Runs <paramref name="work"/> once, <paramref name="seconds"/> from now.</summary>
    IDisposable After(double seconds, Action work);

    /// <summary>Runs <paramref name="work"/> every <paramref name="seconds"/> until the handle is disposed.</summary>
    IDisposable Every(double seconds, Action work);
}
