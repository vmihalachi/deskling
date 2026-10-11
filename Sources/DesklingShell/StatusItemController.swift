#if os(macOS)
    import AppKit
    import SwiftUI

    /// Owns a menu bar item: a glyph drawn by the app (stepped at 8 fps while animating, one frame under Reduce Motion),
    /// a panel shown as a transient popover on left click, and a native menu on right click. The glyph comes from
    /// `image(animating:frame:)`, which the app draws (a template image, so macOS tints it); the tooltip and the
    /// accessibility label are the app's, already localized. Needs the main actor; asks for no permission.
    @MainActor
    public final class StatusItemController: NSObject {
        /// The right-click menu, built fresh each time; nil shows none.
        public var menu: (() -> NSMenu)?

        /// The glyph's frame rate while animating.
        public static let frameInterval: TimeInterval = 1.0 / 8.0

        private let item: NSStatusItem
        private let popover = NSPopover()
        private var image: @MainActor (_ animating: Bool, _ frame: Int) -> NSImage
        private var animating = false
        private var frame = 0
        private var frameTimer: Timer?
        private var waveEnd: Timer?

        /// `image` draws the glyph: `animating` while talking or waving, `frame` counting up from 1 then (0 at rest).
        public init(
            panel: AnyView, tooltip: String, accessibilityLabel: String,
            image: @escaping @MainActor (_ animating: Bool, _ frame: Int) -> NSImage
        ) {
            item = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
            self.image = image
            super.init()
            let controller = NSHostingController(rootView: panel)
            controller.sizingOptions = [.preferredContentSize]
            popover.behavior = .transient
            popover.animates = true
            popover.contentViewController = controller
            if let button = item.button {
                button.image = image(false, 0)
                button.toolTip = tooltip
                button.setAccessibilityLabel(accessibilityLabel)
                button.target = self
                button.action = #selector(clicked(_:))
                _ = button.sendAction(on: [.leftMouseUp, .rightMouseUp])
            }
        }

        // MARK: Glyph

        /// Replaces the glyph drawing (for example when the skin changes) and redraws.
        public func setImage(_ image: @escaping @MainActor (_ animating: Bool, _ frame: Int) -> NSImage) {
            self.image = image
            redraw()
        }

        /// Steps the frames while `on` (talking); a wave in progress keeps them going until it ends.
        public func setAnimating(_ on: Bool) {
            guard on != animating else { return }
            animating = on
            if on { startFrames() } else if waveEnd == nil { stopFrames() }
            redraw()
        }

        /// A wave: the animating frames for a moment, even though nothing is said.
        public func wave(for duration: TimeInterval = 1.2) {
            waveEnd?.invalidate()
            startFrames()
            waveEnd = Timer.scheduledTimer(withTimeInterval: duration, repeats: false) { [weak self] _ in
                MainActor.assumeIsolated {
                    guard let self else { return }
                    self.waveEnd = nil
                    if !self.animating { self.stopFrames() }
                    self.redraw()
                }
            }
            redraw()
        }

        /// Draws the glyph again, for when what `image` draws changed (the mood).
        public func redraw() {
            item.button?.image = image(animating || waveEnd != nil, frame)
        }

        private var reduceMotion: Bool { NSWorkspace.shared.accessibilityDisplayShouldReduceMotion }

        private func startFrames() {
            frameTimer?.invalidate()
            frame = 1
            guard !reduceMotion else { return }
            frameTimer = Timer.scheduledTimer(withTimeInterval: Self.frameInterval, repeats: true) { [weak self] _ in
                MainActor.assumeIsolated { self?.step() }
            }
        }

        private func stopFrames() {
            frameTimer?.invalidate()
            frameTimer = nil
            frame = 0
        }

        private func step() {
            frame += 1
            redraw()
        }

        // MARK: Panel

        public func togglePanel() {
            if popover.isShown { closePanel() } else { openPanel() }
        }

        public func openPanel() {
            guard let button = item.button, !popover.isShown else { return }
            popover.show(relativeTo: button.bounds, of: button, preferredEdge: .minY)
            popover.contentViewController?.view.window?.makeKey()
        }

        public func closePanel() {
            popover.performClose(nil)
        }

        @objc private func clicked(_ sender: Any?) {
            if NSApp.currentEvent?.type == .rightMouseUp {
                showMenu()
            } else {
                togglePanel()
            }
        }

        // MARK: Menu

        /// A status item shows its `menu` on any click, so the menu is set only for this one click.
        private func showMenu() {
            closePanel()
            guard let menu = menu?() else { return }
            item.menu = menu
            item.button?.performClick(nil)
            item.menu = nil
        }
    }
#endif
