import LachingCore
import Foundation

/// Whether the character stands on the desktop right now, and how long its speech bubble stays there.
///
/// The character steps aside while another app uses the camera or microphone or an app is full screen, and comes
/// back `reappearDelay` after the last busy moment, so a microphone that blinks on and off doesn't make it flicker.
/// "Hide for a while" lives here too, in memory only: a relaunch brings the character back. Pure: the shell passes
/// the moment in. Pinned by `conformance/companion/desktop.json`.
public struct DesktopPresence: Equatable, Sendable {
    /// How long after the last call or full-screen moment the character comes back.
    public static let reappearDelay: TimeInterval = 10
    /// "Hide for an hour".
    public static let hourHide: TimeInterval = 3600

    /// Hidden for a while, until this moment ("for an hour", "for the rest of today").
    public private(set) var hiddenUntil: Date?
    /// The last moment a call or a full-screen app was seen.
    public private(set) var lastBusy: Date?

    public init() {}

    public mutating func hide(until date: Date) {
        hiddenUntil = date
    }

    /// Ends a hide for a while early.
    public mutating func showNow() {
        hiddenUntil = nil
    }

    /// Whether the character is on the desktop at `now`, with the desktop setting `enabled`. Call it on every tick and busy
    /// poll: it remembers the last busy moment and forgets a hide that has run out.
    @discardableResult
    public mutating func update(enabled: Bool, snapshot: Snapshot, now: Date) -> Bool {
        if !snapshot.busy.isEmpty { lastBusy = now }
        if let hiddenUntil, now >= hiddenUntil { self.hiddenUntil = nil }
        guard enabled, hiddenUntil == nil else { return false }
        if let lastBusy, now.timeIntervalSince(lastBusy) < Self.reappearDelay { return false }
        return true
    }

    /// How long the desktop bubble stays once the line has been said (or shown, when nothing is spoken): half a
    /// second plus 40 ms a character, between 2 and 5 s. Characters are Unicode scalars, as on Windows.
    public static func bubbleSeconds(for text: String) -> TimeInterval {
        min(max(0.5 + 0.04 * Double(text.unicodeScalars.count), 2), 5)
    }
}
