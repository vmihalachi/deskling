#if os(macOS)
    import AppKit
    import Foundation

    /// Light or dark, as the app resolves it.
    public enum Appearance: Equatable, Sendable {
        case light, dark
    }

    /// Follows the app's effective appearance (System Settings → Appearance, or the app's own override)
    /// through KVO on `NSApplication.shared`. Main actor only, as `NSApp` is; the callback runs there too.
    /// Reads state only, never changes the appearance.
    @MainActor
    public final class AppearanceWatcher {
        public private(set) var current: Appearance
        /// Called after `current` changed.
        public var onChange: ((Appearance) -> Void)?
        private var observation: NSKeyValueObservation?

        public init() {
            current = Self.appearance(of: NSApplication.shared.effectiveAppearance)
            observation = NSApplication.shared.observe(\.effectiveAppearance, options: [.new]) { [weak self] app, _ in
                MainActor.assumeIsolated {
                    guard let self else { return }
                    let next = Self.appearance(of: app.effectiveAppearance)
                    guard next != self.current else { return }
                    self.current = next
                    self.onChange?(next)
                }
            }
        }

        public static func appearance(of appearance: NSAppearance) -> Appearance {
            appearance.bestMatch(from: [.aqua, .darkAqua]) == .darkAqua ? .dark : .light
        }
    }
#endif
