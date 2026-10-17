#if os(macOS)
    import CoreGraphics
    import Foundation

    /// Where a `FloatingWindow` and a window beside it go: plain rectangle math in AppKit's global coordinates
    /// (origin at the primary screen's bottom left, y up), with no AppKit state, so it is unit-tested.
    ///
    /// A floating window stands on its anchor, the middle of its bottom edge, so a size change grows it upward
    /// in place.
    public enum FloatingPlacement {
        /// Which side of a window another one sits on.
        public enum Side: String, Sendable {
            case left, right
        }

        /// The frame of a window of `size` standing on `anchor`.
        public static func frame(anchor: CGPoint, size: CGSize) -> CGRect {
            CGRect(x: anchor.x - size.width / 2, y: anchor.y, width: size.width, height: size.height)
        }

        /// The middle of `frame`'s bottom edge.
        public static func anchor(of frame: CGRect) -> CGPoint {
            CGPoint(x: frame.midX, y: frame.minY)
        }

        /// Bottom right of `visibleFrame` (above the Dock when it sits at the bottom), `inset` in from both edges.
        public static func defaultAnchor(size: CGSize, in visibleFrame: CGRect, inset: CGFloat) -> CGPoint {
            CGPoint(x: visibleFrame.maxX - inset - size.width / 2, y: visibleFrame.minY + inset)
        }

        /// `anchor`, moved as little as needed for a window of `size` to sit wholly inside one of `visibleFrames`:
        /// the one holding the anchor, or else the nearest. Unchanged when the window already fits on a screen,
        /// or when there are no screens.
        public static func clamped(anchor: CGPoint, size: CGSize, into visibleFrames: [CGRect]) -> CGPoint {
            guard !visibleFrames.isEmpty else { return anchor }
            let window = frame(anchor: anchor, size: size)
            if visibleFrames.contains(where: { holds($0, window) }) { return anchor }
            let screen =
                visibleFrames.first { holds($0, anchor) }
                ?? visibleFrames.min { distance(from: anchor, to: $0) < distance(from: anchor, to: $1) }!
            return CGPoint(
                x: clamp(anchor.x, screen.minX + size.width / 2, screen.maxX - size.width / 2),
                y: clamp(anchor.y, screen.minY, screen.maxY - size.height))
        }

        /// The side of `parent` with more room on its screen, so a window beside it opens toward the middle.
        public static func side(beside parent: CGRect, in visibleFrame: CGRect) -> Side {
            parent.midX > visibleFrame.midX ? .left : .right
        }

        /// A window of `size` on `side` of `parent`, `gap` away (negative tucks it in), its middle at `centerY`,
        /// kept inside `visibleFrame`.
        public static func frame(
            size: CGSize, beside parent: CGRect, on side: Side, centerY: CGFloat, gap: CGFloat, in visibleFrame: CGRect
        ) -> CGRect {
            let x = side == .left ? parent.minX - gap - size.width : parent.maxX + gap
            return CGRect(
                x: clamp(x, visibleFrame.minX, visibleFrame.maxX - size.width),
                y: clamp(centerY - size.height / 2, visibleFrame.minY, visibleFrame.maxY - size.height),
                width: size.width, height: size.height)
        }

        /// A global AppKit point as seen from the primary screen's top left with y down, the platform-neutral form
        /// an app stores (Windows' virtual screen has the same origin and direction). `primaryHeight` is the
        /// primary screen's height.
        public static func topLeftOrigin(_ point: CGPoint, primaryHeight: CGFloat) -> CGPoint {
            CGPoint(x: point.x, y: primaryHeight - point.y)
        }

        /// The inverse of `topLeftOrigin(_:primaryHeight:)`.
        public static func fromTopLeftOrigin(_ point: CGPoint, primaryHeight: CGFloat) -> CGPoint {
            CGPoint(x: point.x, y: primaryHeight - point.y)
        }

        // MARK: Helpers

        /// Half a point of slack, so a frame computed from a screen's own edges counts as inside it.
        private static let slack: CGFloat = 0.5

        private static func holds(_ screen: CGRect, _ window: CGRect) -> Bool {
            window.minX >= screen.minX - slack && window.maxX <= screen.maxX + slack
                && window.minY >= screen.minY - slack && window.maxY <= screen.maxY + slack
        }

        private static func holds(_ screen: CGRect, _ point: CGPoint) -> Bool {
            point.x >= screen.minX && point.x <= screen.maxX && point.y >= screen.minY && point.y <= screen.maxY
        }

        private static func distance(from point: CGPoint, to rect: CGRect) -> CGFloat {
            let dx = max(rect.minX - point.x, 0, point.x - rect.maxX)
            let dy = max(rect.minY - point.y, 0, point.y - rect.maxY)
            return hypot(dx, dy)
        }

        /// `value` within `low...high`; the middle when the range is empty (a window bigger than the screen).
        private static func clamp(_ value: CGFloat, _ low: CGFloat, _ high: CGFloat) -> CGFloat {
            guard low <= high else { return (low + high) / 2 }
            return min(max(value, low), high)
        }
    }
#endif
