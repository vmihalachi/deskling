#if os(macOS)
    import Foundation

    /// Whether the screen is locked, from the distributed notifications `com.apple.screenIsLocked` and
    /// `com.apple.screenIsUnlocked`. Those names are undocumented but have been stable since 10.x; still,
    /// an app should treat idle time as the fallback signal rather than rely on this alone. Starts as
    /// `false` (there is no way to ask) and tracks changes from then on. The callback runs on the main queue.
    /// Reads notifications only: never locks, unlocks or prompts.
    public final class ScreenLockWatcher {
        public private(set) var isLocked = false
        /// Called on the main queue after `isLocked` changed.
        public var onChange: ((Bool) -> Void)?
        private var observers: [NSObjectProtocol] = []

        public init() {
            let center = DistributedNotificationCenter.default()
            observers = [
                center.addObserver(forName: Self.lockedName, object: nil, queue: .main) { [weak self] _ in
                    self?.update(locked: true)
                },
                center.addObserver(forName: Self.unlockedName, object: nil, queue: .main) { [weak self] _ in
                    self?.update(locked: false)
                },
            ]
        }

        deinit {
            let center = DistributedNotificationCenter.default()
            for observer in observers { center.removeObserver(observer) }
        }

        public static let lockedName = Notification.Name("com.apple.screenIsLocked")
        public static let unlockedName = Notification.Name("com.apple.screenIsUnlocked")

        private func update(locked: Bool) {
            guard locked != isLocked else { return }
            isLocked = locked
            onChange?(locked)
        }
    }
#endif
