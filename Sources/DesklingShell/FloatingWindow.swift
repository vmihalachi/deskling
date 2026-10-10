#if os(macOS)
    import AppKit
    import SwiftUI

    /// A small borderless window that floats above other apps' windows on every Space, for a desktop pet or a
    /// sticky note: SwiftUI content on a clear background, no shadow. It never becomes key and never activates
    /// the app, so the frontmost app keeps the keyboard and an `LSUIElement` app gets no Dock icon (it isn't one of
    /// `WindowManager`'s windows). Full-screen Spaces don't show it.
    ///
    /// It stands on its `anchor`, the middle of its bottom edge in AppKit's global coordinates, so `setSize(_:)`
    /// grows it upward in place, and it stays wholly on a screen (`FloatingPlacement.clamped`): a drag ends inside
    /// one, and when displays change it moves onto one, returning to the anchor once that fits again. A click
    /// (less than 3 pt of movement) calls `onClick`; a drag moves it and reports the new anchor; a right or
    /// Control click shows `menu`; a trackpad pinch reports one step per gesture.
    ///
    /// A second floating window can ride along beside it (`attach(to:)`, for a speech bubble): it moves with its
    /// parent and is placed by the app with `setFrame(_:)`. Needs the main actor; asks for no permission.
    @MainActor
    public final class FloatingWindow {
        /// A click without a drag.
        public var onClick: (() -> Void)?
        /// A drag ended; the new anchor, already on a screen.
        public var onDragEnded: ((CGPoint) -> Void)?
        /// A trackpad pinch: +1 when spreading, -1 when pinching, once per gesture.
        public var onPinch: ((Int) -> Void)?
        /// The menu for a right or Control click; nil shows none.
        public var menu: (() -> NSMenu?)?
        /// The displays changed (one was added, removed or rearranged), after the window moved onto a screen.
        public var onScreensChanged: (() -> Void)?
        /// Whether a drag moves the window. Off for a window riding along beside another.
        public var isMovable: Bool
        public private(set) var isShown = false

        public static let fadeInDuration: TimeInterval = 0.4
        public static let fadeOutDuration: TimeInterval = 0.2
        /// How far the pointer moves before a press is a drag rather than a click.
        public static let dragThreshold: CGFloat = 3
        /// How much a trackpad pinch must magnify (or shrink) before it counts as a step.
        public static let pinchThreshold: CGFloat = 0.15

        /// Where the window stands, in AppKit's global coordinates. Setting it moves the window (onto a screen).
        public var anchor: CGPoint {
            didSet { place() }
        }
        public private(set) var size: CGSize

        private let panel: FloatingPanel
        private let host: NSHostingView<AnyView>
        private weak var parent: FloatingWindow?
        /// Bumped by every show and hide, so a fade that finishes late doesn't undo a newer call.
        private var generation = 0

        public init(content: AnyView, size: CGSize, anchor: CGPoint, isMovable: Bool = true) {
            self.size = size
            self.anchor = anchor
            self.isMovable = isMovable
            panel = FloatingPanel(size: size)
            host = NSHostingView(rootView: content)
            host.sizingOptions = []
            let container = NSView(frame: NSRect(origin: .zero, size: size))
            host.frame = container.bounds
            host.autoresizingMask = [.width, .height]
            container.addSubview(host)
            let events = FloatingEventView(frame: container.bounds)
            events.autoresizingMask = [.width, .height]
            container.addSubview(events)
            panel.contentView = container
            events.owner = self
            panel.onScreensChanged = { [weak self] in self?.screensChanged() }
            place()
        }

        /// The window's frame on screen right now.
        public var frame: CGRect { panel.frame }

        /// The visible frame (without menu bar and Dock) of the screen the window is on.
        public var visibleFrame: CGRect? {
            (panel.screen ?? NSScreen.main ?? NSScreen.screens.first)?.visibleFrame
        }

        public func setContent(_ content: AnyView) {
            host.rootView = content
        }

        /// The content's ideal size, at most `maxWidth` wide.
        public func idealSize(maxWidth: CGFloat) -> CGSize {
            NSHostingController(rootView: host.rootView)
                .sizeThatFits(in: CGSize(width: maxWidth, height: CGFloat.greatestFiniteMagnitude))
        }

        /// Resizes around the anchor: the bottom middle stays put.
        public func setSize(_ size: CGSize) {
            self.size = size
            place()
        }

        /// Puts a riding-along window exactly at `frame` (a `FloatingPlacement` result), without clamping.
        public func setFrame(_ frame: CGRect) {
            size = frame.size
            panel.setFrame(frame, display: true)
        }

        /// Rides along beside `parent` from now on: shown above it and moved with it.
        public func attach(to parent: FloatingWindow) {
            self.parent = parent
            if isShown { parent.panel.addChildWindow(panel, ordered: .above) }
        }

        // MARK: Showing

        /// Orders the window in without activating the app, fading in unless `animated` is false.
        public func show(animated: Bool) {
            generation += 1
            let wasVisible = panel.isVisible
            isShown = true
            if parent == nil { place() }
            if let parent {
                parent.panel.addChildWindow(panel, ordered: .above)
            } else {
                panel.orderFrontRegardless()
            }
            guard animated else {
                panel.alphaValue = 1
                return
            }
            if !wasVisible { panel.alphaValue = 0 }
            NSAnimationContext.runAnimationGroup { context in
                context.duration = Self.fadeInDuration
                panel.animator().alphaValue = 1
            }
        }

        /// Fades the window out (unless `animated` is false), then orders it out.
        public func hide(animated: Bool) {
            guard isShown else { return }
            isShown = false
            generation += 1
            let current = generation
            guard animated, panel.isVisible else {
                orderOut()
                return
            }
            NSAnimationContext.runAnimationGroup { context in
                context.duration = Self.fadeOutDuration
                panel.animator().alphaValue = 0
            } completionHandler: { [weak self] in
                MainActor.assumeIsolated {
                    guard let self, self.generation == current else { return }
                    self.orderOut()
                }
            }
        }

        private func orderOut() {
            panel.parent?.removeChildWindow(panel)
            panel.orderOut(nil)
            panel.alphaValue = 1
        }

        // MARK: Placement

        private func place() {
            let target = FloatingPlacement.clamped(anchor: anchor, size: size, into: NSScreen.screens.map(\.visibleFrame))
            panel.setFrame(FloatingPlacement.frame(anchor: target, size: size), display: true)
        }

        private func screensChanged() {
            if parent == nil { place() }
            onScreensChanged?()
        }

        // MARK: Events (from FloatingEventView)

        fileprivate func dragEnded() {
            let dropped = FloatingPlacement.anchor(of: panel.frame)
            anchor = FloatingPlacement.clamped(anchor: dropped, size: size, into: NSScreen.screens.map(\.visibleFrame))
            onDragEnded?(anchor)
        }
    }

    /// The panel behind a `FloatingWindow`: borderless, non-activating, never key, on every Space but full-screen ones.
    final class FloatingPanel: NSPanel {
        var onScreensChanged: (() -> Void)?

        init(size: CGSize) {
            super.init(
                contentRect: NSRect(origin: .zero, size: size), styleMask: [.borderless, .nonactivatingPanel],
                backing: .buffered, defer: false)
            isFloatingPanel = true
            level = .floating
            // No .fullScreenAuxiliary: full-screen Spaces belong to the app that's full screen.
            collectionBehavior = [.canJoinAllSpaces, .stationary, .ignoresCycle]
            isOpaque = false
            backgroundColor = .clear
            hasShadow = false
            hidesOnDeactivate = false
            isReleasedWhenClosed = false
            becomesKeyOnlyIfNeeded = true
            animationBehavior = .none
            NotificationCenter.default.addObserver(
                self, selector: #selector(screenParametersChanged(_:)),
                name: NSApplication.didChangeScreenParametersNotification, object: nil)
        }

        override var canBecomeKey: Bool { false }
        override var canBecomeMain: Bool { false }

        @objc private func screenParametersChanged(_ notification: Notification) {
            onScreensChanged?()
        }
    }

    /// Covers a `FloatingWindow`'s content and turns the pointer into clicks, drags, menus and pinches.
    final class FloatingEventView: NSView {
        weak var owner: FloatingWindow?
        /// Where a press started (screen coordinates) and where the window was then.
        private var pressStart: NSPoint?
        private var originAtPress: NSPoint = .zero
        private var dragging = false
        private var magnification: CGFloat = 0
        private var pinched = false

        override func acceptsFirstMouse(for event: NSEvent?) -> Bool { true }

        override func mouseDown(with event: NSEvent) {
            if event.modifierFlags.contains(.control) {
                showMenu(for: event)
                return
            }
            pressStart = NSEvent.mouseLocation
            originAtPress = window?.frame.origin ?? .zero
            dragging = false
        }

        override func mouseDragged(with event: NSEvent) {
            guard let pressStart, let owner, owner.isMovable, let window else { return }
            let now = NSEvent.mouseLocation
            let dx = now.x - pressStart.x
            let dy = now.y - pressStart.y
            if !dragging && hypot(dx, dy) < FloatingWindow.dragThreshold { return }
            dragging = true
            window.setFrameOrigin(NSPoint(x: originAtPress.x + dx, y: originAtPress.y + dy))
        }

        override func mouseUp(with event: NSEvent) {
            guard pressStart != nil else { return }
            pressStart = nil
            if dragging {
                dragging = false
                owner?.dragEnded()
            } else {
                owner?.onClick?()
            }
        }

        override func rightMouseDown(with event: NSEvent) {
            showMenu(for: event)
        }

        override func magnify(with event: NSEvent) {
            if event.phase == .began {
                magnification = 0
                pinched = false
            }
            magnification += event.magnification
            if !pinched, abs(magnification) >= FloatingWindow.pinchThreshold {
                pinched = true
                owner?.onPinch?(magnification > 0 ? 1 : -1)
            }
            if event.phase == .ended || event.phase == .cancelled {
                magnification = 0
                pinched = false
            }
        }

        private func showMenu(for event: NSEvent) {
            guard let menu = owner?.menu?() else { return }
            NSMenu.popUpContextMenu(menu, with: event, for: self)
        }
    }
#endif
