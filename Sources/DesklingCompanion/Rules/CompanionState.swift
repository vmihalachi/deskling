import DesklingCore
import Foundation

/// How the character looks and behaves right now. Raw values appear in the vectors; never rename one.
public enum Mood: String, CaseIterable, Codable, Sendable {
    case awake
    case talking
    /// Shh is on: drooped, mouth closed.
    case shushed
    /// Away or the screen is locked: eyes closed, drooped.
    case asleep
    /// A call holds the lines.
    case hushedForCall
    /// A full-screen app holds the lines.
    case hushedFullScreen
    /// Outside the active hours and days.
    case offHours
}

/// Derives the character's mood from the scheduler's status and the moment. Pure; part of `conformance/companion/quiet`.
public enum CompanionState {
    public static func mood(status: SchedulerStatus, snapshot: Snapshot) -> Mood {
        if snapshot.isSpeaking { return .talking }
        if case .paused = status { return .shushed }
        if snapshot.busy.contains(.call) { return .hushedForCall }
        if snapshot.busy.contains(.fullScreen) { return .hushedFullScreen }
        if snapshot.isScreenLocked || snapshot.idleSeconds >= QuietRules.awayThreshold { return .asleep }
        if case .postponed(let reason) = status { return reason == .call ? .hushedForCall : .hushedFullScreen }
        if status == .away { return .asleep }
        if status == .outsideActiveHours { return .offHours }
        return .awake
    }
}
