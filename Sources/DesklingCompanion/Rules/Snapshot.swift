import DesklingCore
import Foundation

/// What the shell knows about this moment, read from the system without any permission, as a value the
/// rules can judge.
public struct Snapshot: Equatable, Sendable {
    /// Another app uses the camera or microphone, or an app is full screen.
    public var busy: Set<BusyReason>
    public var idleSeconds: TimeInterval
    public var isScreenLocked: Bool
    /// nil when the output state can't be read.
    public var isOutputMuted: Bool?
    /// A line is being spoken right now.
    public var isSpeaking: Bool
    /// Notifications may be shown (the user allowed them earlier).
    public var notificationsAuthorized: Bool

    public init(
        busy: Set<BusyReason> = [], idleSeconds: TimeInterval = 0, isScreenLocked: Bool = false,
        isOutputMuted: Bool? = nil, isSpeaking: Bool = false, notificationsAuthorized: Bool = false
    ) {
        self.busy = busy
        self.idleSeconds = idleSeconds
        self.isScreenLocked = isScreenLocked
        self.isOutputMuted = isOutputMuted
        self.isSpeaking = isSpeaking
        self.notificationsAuthorized = notificationsAuthorized
    }

    /// Nobody busy, nobody away, nothing muted.
    public static let calm = Snapshot()
}
