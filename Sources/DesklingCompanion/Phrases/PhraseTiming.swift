import Foundation

/// How the parts of a sequence are spaced. Pure numbers; the shell runs the timers. Pinned by
/// `conformance/companion/desktop.json` (`partDelays`).
public enum PhraseTiming {
    /// The silence after a spoken part before the next one starts: the beat before a punchline.
    public static let partBeat: TimeInterval = 1.2

    /// How long to wait before the next part: when spoken, `partBeat` after the previous part finished; when shown
    /// without speech, as long as the previous part's bubble stays (`DesktopPresence.bubbleSeconds`) after it appeared.
    public static func partDelay(spoken: Bool, previousPart: String) -> TimeInterval {
        spoken ? partBeat : DesktopPresence.bubbleSeconds(for: previousPart)
    }
}
