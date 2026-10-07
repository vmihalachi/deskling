#if os(macOS)
    import AppKit
    import Foundation
    import SwiftUI

    /// Where a new window goes.
    public enum WindowPlacement: Equatable, Sendable {
        case center
        /// Top right, just under the menu bar where the app's icon lives, so "look up there" is obvious.
        case underMenuBar
    }

    /// One of an app's windows: an enum in the app, with a title, a size and a few optional traits. The
    /// defaults (not exclusive, centered, no minimum size, normal level, no remembered frame) suit a plain
    /// window; the protocol extension supplies them.
    public protocol WindowID: Hashable {
        /// Already localized by the app.
        var title: String { get }
        var size: CGSize { get }
        /// Exclusive windows share one slot: opening one closes whichever other exclusive window is open.
        var isExclusive: Bool { get }
        var placement: WindowPlacement { get }
        /// The content's own minimum, so a remembered or dragged frame can't clip it.
        var minSize: CGSize? { get }
        var level: NSWindow.Level { get }
        /// Remembers the frame across launches under this name (`NSWindow.setFrameAutosaveName`).
        var frameAutosaveName: String? { get }
    }

    extension WindowID {
        public var isExclusive: Bool { false }
        public var placement: WindowPlacement { .center }
        public var minSize: CGSize? { nil }
        public var level: NSWindow.Level { .normal }
        public var frameAutosaveName: String? { nil }
    }

    /// Owns an app's AppKit windows so they can be opened from anywhere (menu, notification actions) in an
    /// `LSUIElement` app, with SwiftUI content from `contentProvider`.
    ///
    /// The app is menu-bar-only while idle. While any window is open it becomes a regular app (Dock icon,
    /// ⌘Tab, main menu) and drops back when the last closes. Needs the main actor and a running
    /// `NSApplication`; never changes anything but its own windows and the activation policy.
    @MainActor
    public final class WindowManager<ID: WindowID> {
        private var windows: [ID: NSWindow] = [:]
        private let backgroundColor: NSColor
        private let contentProvider: (ID) -> AnyView
        private let delegate: WindowDelegateProxy
        public var onClose: ((ID) -> Void)?
        /// A window became big (fills most of its screen) or stopped being big.
        public var onBigChange: ((ID, Bool) -> Void)?
        /// The frame each window had before "Bigger", to go back to on "Smaller".
        private var smallFrames: [ID: NSRect] = [:]

        /// Screenshot and preview modes: stay a regular app between windows. Dropping to accessory hands
        /// activation to the app behind, and macOS may not give it back, so the next window would be drawn
        /// inactive.
        public var staysRegular = false

        public init(backgroundColor: NSColor, contentProvider: @escaping (ID) -> AnyView) {
            self.backgroundColor = backgroundColor
            self.contentProvider = contentProvider
            delegate = WindowDelegateProxy()
            delegate.onResize = { [weak self] window in self?.windowDidResize(window) }
            delegate.onWillClose = { [weak self] window in self?.windowWillClose(window) }
        }

        public func isOpen(_ id: ID) -> Bool { windows[id] != nil }

        /// The `NSWindow` behind an open window, for the odd AppKit call this class doesn't cover.
        public func window(for id: ID) -> NSWindow? { windows[id] }

        /// Brings the open windows forward; false when none is open.
        public func showOpenWindows() -> Bool {
            guard !windows.isEmpty else { return false }
            for window in windows.values where window.isMiniaturized { window.deminiaturize(nil) }
            for window in windows.values { bringToFront(window) }
            return true
        }

        public func show(_ id: ID) {
            if let existing = windows[id] {
                closeOtherExclusive(than: id)
                bringToFront(existing)
                return
            }
            let content = contentProvider(id)
            let window = NSWindow(
                contentRect: NSRect(origin: .zero, size: id.size),
                styleMask: [.titled, .closable, .miniaturizable, .resizable],
                backing: .buffered, defer: false)
            window.title = id.title
            window.titleVisibility = .hidden
            window.titlebarAppearsTransparent = true
            window.styleMask.insert(.fullSizeContentView)
            window.backgroundColor = backgroundColor
            window.isReleasedWhenClosed = false
            window.contentViewController = NSHostingController(rootView: content)
            window.setContentSize(id.size)
            switch id.placement {
            case .center: window.center()
            case .underMenuBar: placeUnderMenuBar(window)
            }
            window.delegate = delegate
            if let minSize = id.minSize { window.contentMinSize = minSize }
            window.level = id.level
            if let name = id.frameAutosaveName { window.setFrameAutosaveName(name) }
            windows[id] = window
            onBigChange?(id, isBig(window))
            // Register the new window first so the app never briefly drops to
            // accessory (Dock icon flicker) while the old one closes.
            closeOtherExclusive(than: id)
            updateActivationPolicy()
            bringToFront(window)
        }

        /// Top right, just under the menu bar where the icon lives, so "look up there" is obvious
        /// even on a light wallpaper.
        private func placeUnderMenuBar(_ window: NSWindow) {
            guard let screen = NSScreen.main ?? NSScreen.screens.first else { return window.center() }
            let frame = screen.visibleFrame
            let size = window.frame.size
            window.setFrameTopLeftPoint(NSPoint(x: frame.maxX - size.width - 24, y: frame.maxY - 12))
        }

        /// Fills the screen, or goes back to the size the window had before (its default size when it
        /// opened big, from the remembered frame). Not `zoom(_:)`: when the remembered frame is already
        /// screen-sized, zoom has nothing to go back to and does nothing.
        public func toggleBig(_ id: ID) {
            guard let window = windows[id], let screen = window.screen ?? NSScreen.main else { return }
            let visible = screen.visibleFrame
            if isBig(window) {
                var frame = window.frameRect(forContentRect: NSRect(origin: .zero, size: id.size))
                frame.origin = NSPoint(x: visible.midX - frame.width / 2, y: visible.midY - frame.height / 2)
                if let small = smallFrames[id], visible.contains(small) { frame = small }
                window.setFrame(frame, display: true, animate: true)
            } else {
                smallFrames[id] = window.frame
                window.setFrame(visible, display: true, animate: true)
            }
        }

        /// Covers most of its screen, whether from "Bigger", the green button or a manual resize.
        public func isBig(_ id: ID) -> Bool {
            guard let window = windows[id] else { return false }
            return isBig(window)
        }

        private func isBig(_ window: NSWindow) -> Bool {
            guard let visible = (window.screen ?? NSScreen.main)?.visibleFrame else { return false }
            return window.frame.width >= visible.width * 0.9 && window.frame.height >= visible.height * 0.9
        }

        public func close(_ id: ID) {
            windows[id]?.close()
        }

        public func closeAll() {
            for window in windows.values { window.close() }
        }

        private func closeOtherExclusive(than id: ID) {
            guard id.isExclusive else { return }
            for (other, window) in windows where other != id && other.isExclusive {
                window.close()
            }
        }

        private func updateActivationPolicy() {
            let policy: NSApplication.ActivationPolicy = windows.isEmpty ? .accessory : .regular
            if staysRegular && policy == .accessory { return }
            if NSApp.activationPolicy() != policy { NSApp.setActivationPolicy(policy) }
        }

        private func bringToFront(_ window: NSWindow) {
            present(window)
            // Menu bar actions fire while the menu is still dismissing, which can
            // swallow activation. Repeat once the menu has closed.
            DispatchQueue.main.async { [weak window] in
                guard let window, window.isVisible else { return }
                self.present(window)
            }
        }

        private func present(_ window: NSWindow) {
            // macOS 14+ treats activate() as a request it can turn down while another app is
            // in front, so also force the window above other apps. (activate(ignoringOtherApps:)
            // still gets through when nobody is using the Mac; screenshot and preview modes
            // rely on that.)
            NSApp.activate()
            window.makeKeyAndOrderFront(nil)
            window.orderFrontRegardless()
        }

        private func identifier(of window: NSWindow) -> ID? {
            windows.first { $0.value === window }?.key
        }

        private func windowDidResize(_ window: NSWindow) {
            guard let id = identifier(of: window) else { return }
            onBigChange?(id, isBig(window))
        }

        private func windowWillClose(_ window: NSWindow) {
            guard let id = identifier(of: window) else { return }
            windows[id] = nil
            onClose?(id)
            updateActivationPolicy()
        }
    }

    /// `NSWindowDelegate` for `WindowManager`: a generic class can't hold `@objc` delegate methods, so this
    /// non-generic object forwards the two it needs.
    @MainActor
    final class WindowDelegateProxy: NSObject, NSWindowDelegate {
        var onResize: (@MainActor (NSWindow) -> Void)?
        var onWillClose: (@MainActor (NSWindow) -> Void)?

        nonisolated func windowDidResize(_ notification: Notification) {
            MainActor.assumeIsolated {
                guard let window = notification.object as? NSWindow else { return }
                onResize?(window)
            }
        }

        nonisolated func windowWillClose(_ notification: Notification) {
            MainActor.assumeIsolated {
                guard let window = notification.object as? NSWindow else { return }
                onWillClose?(window)
            }
        }
    }
#endif
