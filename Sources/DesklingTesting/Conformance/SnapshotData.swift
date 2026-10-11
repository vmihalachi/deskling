import DesklingCompanion
import DesklingCore
import Foundation

/// A `Snapshot` as the vectors write it (`quiet/`, `desktop.json`): busy reasons by name, sorted; whole idle seconds;
/// `isOutputMuted` written as `null` rather than left out, so ports see the field.
public struct SnapshotData: Codable, Equatable, Sendable {
    public var busy: [String]
    public var idleSeconds: Int
    public var isScreenLocked: Bool
    public var isOutputMuted: Bool?
    public var isSpeaking: Bool
    public var notificationsAuthorized: Bool

    public init(_ s: Snapshot) {
        busy = s.busy.map(Self.name).sorted()
        idleSeconds = Int(s.idleSeconds)
        isScreenLocked = s.isScreenLocked
        isOutputMuted = s.isOutputMuted
        isSpeaking = s.isSpeaking
        notificationsAuthorized = s.notificationsAuthorized
    }

    public var snapshot: Snapshot {
        Snapshot(
            busy: Set(busy.compactMap(Self.busyReason)), idleSeconds: TimeInterval(idleSeconds),
            isScreenLocked: isScreenLocked, isOutputMuted: isOutputMuted, isSpeaking: isSpeaking,
            notificationsAuthorized: notificationsAuthorized)
    }

    public func encode(to encoder: Encoder) throws {
        var c = encoder.container(keyedBy: CodingKeys.self)
        try c.encode(busy, forKey: .busy)
        try c.encode(idleSeconds, forKey: .idleSeconds)
        try c.encode(isScreenLocked, forKey: .isScreenLocked)
        try c.encode(isOutputMuted, forKey: .isOutputMuted)
        try c.encode(isSpeaking, forKey: .isSpeaking)
        try c.encode(notificationsAuthorized, forKey: .notificationsAuthorized)
    }

    /// `BusyReason` as the vectors spell it.
    public static func name(_ reason: BusyReason) -> String {
        switch reason {
        case .call: "call"
        case .fullScreen: "fullScreen"
        }
    }

    public static func busyReason(_ name: String) -> BusyReason? {
        switch name {
        case "call": .call
        case "fullScreen": .fullScreen
        default: nil
        }
    }
}
