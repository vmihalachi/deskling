using Deskling.Companion.Desktop;

namespace Deskling.Companion.Phrases;

/// <summary>
/// How the parts of a sequence are spaced. Pure numbers; the shell runs the timers. Pinned by
/// <c>conformance/companion/desktop.json</c> (<c>partDelays</c>).
/// </summary>
public static class PhraseTiming
{
    /// <summary>The silence after a spoken part before the next one starts: the beat before a punchline.</summary>
    public const double PartBeatSeconds = 1.2;

    /// <summary>
    /// How long to wait before the next part: when spoken, <see cref="PartBeatSeconds"/> after the previous part
    /// finished; when shown without speech, as long as the previous part's bubble stays
    /// (<see cref="DesktopPresence.BubbleSeconds"/>) after it appeared.
    /// </summary>
    public static double PartDelaySeconds(bool spoken, string previousPart) =>
        spoken ? PartBeatSeconds : DesktopPresence.BubbleSeconds(previousPart);
}
