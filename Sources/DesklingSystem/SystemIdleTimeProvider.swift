#if os(macOS)
    import CoreGraphics
    import DesklingCore
    import Foundation

    /// Seconds since the last keyboard or mouse event, from the HID event source. Needs nothing: the combined
    /// session state is readable by every app, so it never prompts for Accessibility or Input Monitoring.
    public struct SystemIdleTimeProvider: IdleTimeProvider {
        public init() {}

        public func secondsSinceLastInput() -> TimeInterval {
            // `~0` is kCGAnyInputEventType.
            let anyInput = CGEventType(rawValue: ~0)!
            return CGEventSource.secondsSinceLastEventType(.combinedSessionState, eventType: anyInput)
        }
    }
#endif
