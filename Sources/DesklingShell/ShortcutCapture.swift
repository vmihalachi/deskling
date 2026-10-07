#if os(macOS)
    import AppKit
    import DesklingCore
    import Foundation

    /// Records a key combo for a global shortcut: call `start()` when the user clicks the record button, then
    /// the next key press with ⌘, ⌃ or ⌥ becomes a `KeyShortcut` and `onCaptured` gets it. Delete or
    /// Forward Delete clears (`onCaptured(nil)`), Escape cancels (stops, no callback) and a combo without a
    /// required modifier beeps and keeps listening. Uses a local `NSEvent` monitor, so it only sees keys
    /// typed into the app's own windows and needs no permission. Draws nothing: the app renders the button
    /// from `isCapturing` and the shortcut it stores. The app should unregister the live hotkey while
    /// capturing, so the combo being recorded isn't swallowed by its own registration.
    @MainActor
    public final class ShortcutCapture: ObservableObject {
        @Published public private(set) var isCapturing = false
        /// Called once capturing has stopped, with the new shortcut or `nil` when the user cleared it.
        public var onCaptured: ((KeyShortcut?) -> Void)?
        private var monitor: Any?

        public init() {}

        public func start() {
            guard monitor == nil else { return }
            isCapturing = true
            monitor = NSEvent.addLocalMonitorForEvents(matching: .keyDown) { [weak self] event in
                MainActor.assumeIsolated { self?.handle(event) }
                return nil
            }
        }

        public func stop() {
            if let monitor { NSEvent.removeMonitor(monitor) }
            monitor = nil
            isCapturing = false
        }

        private func handle(_ event: NSEvent) {
            let flags = event.modifierFlags.intersection(.deviceIndependentFlagsMask)
            if event.keyCode == 53, flags.isEmpty {  // Esc
                stop()
                return
            }
            if event.keyCode == 51 || event.keyCode == 117, flags.isEmpty {  // Delete
                stop()
                onCaptured?(nil)
                return
            }
            var modifiers: UInt32 = 0
            if flags.contains(.command) { modifiers |= KeyShortcut.command }
            if flags.contains(.shift) { modifiers |= KeyShortcut.shift }
            if flags.contains(.option) { modifiers |= KeyShortcut.option }
            if flags.contains(.control) { modifiers |= KeyShortcut.control }
            let key =
                KeyShortcut.label(forKeyCode: event.keyCode)
                ?? event.charactersIgnoringModifiers?.uppercased() ?? ""
            let shortcut = KeyShortcut(keyCode: UInt32(event.keyCode), modifiers: modifiers, key: key)
            guard shortcut.isValid, !key.isEmpty else {
                NSSound.beep()
                return
            }
            stop()
            onCaptured?(shortcut)
        }
    }
#endif
