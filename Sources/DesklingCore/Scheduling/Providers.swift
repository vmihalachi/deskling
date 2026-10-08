import Foundation

/// Reports how long the user has been away from mouse and keyboard.
public protocol IdleTimeProvider {
    func secondsSinceLastInput() -> TimeInterval
}

/// Something that makes now a bad moment for a reminder.
public enum BusyReason: Hashable, Sendable {
    /// Another app is using the camera or the microphone.
    case call
    /// The frontmost app is full screen (video, presenting, games).
    case fullScreen
}

/// Reports what the user is busy with right now. Implementations must never prompt for a permission.
public protocol BusyStateProvider {
    func currentBusyReasons() -> Set<BusyReason>
}

/// Never busy. The default when no provider is injected.
public struct NeverBusy: BusyStateProvider, Sendable {
    public init() {}
    public func currentBusyReasons() -> Set<BusyReason> { [] }
}
