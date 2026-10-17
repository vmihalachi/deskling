#if os(macOS)
    import Foundation

    extension Timer {
        /// Schedules a timer on the main run loop that runs `body` on the main actor after `interval` seconds (and every
        /// `interval` seconds after that when `repeats`). Invalidate the returned timer to cancel it. The one place the
        /// shell's timers assume the main actor, so the call sites stay strict-concurrency clean. Needs the main actor;
        /// never sleeps a thread.
        @MainActor
        @discardableResult
        public static func main(
            after interval: TimeInterval, repeats: Bool = false, _ body: @escaping @MainActor @Sendable () -> Void
        ) -> Timer {
            scheduledTimer(withTimeInterval: interval, repeats: repeats) { _ in
                MainActor.assumeIsolated { body() }
            }
        }
    }
#endif
