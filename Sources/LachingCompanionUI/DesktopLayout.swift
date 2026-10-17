#if os(macOS)
    import CoreGraphics
    import LachingCompanion

    /// Where things go when a character stands on the desktop, from its art: the window size per `DesktopSize` (the
    /// width follows the view box, with room under it for a contact shadow), and where the speech bubble's tail meets
    /// the character (`headHeightRatio`, the bubble pivot's height in the view box; `bubbleTuck`, how far the bubble
    /// tucks in past the character's empty sides). Pure numbers.
    public struct DesktopLayout: Equatable, Sendable {
        /// Width over height of the art's view box.
        public let aspect: Double
        /// The bubble pivot's distance from the top, as a fraction of the character's height.
        public let headHeightRatio: Double
        /// A fraction of the window's width the bubble overlaps it by.
        public let bubbleTuck: Double
        /// Room under the character for the shadow.
        public let shadowHeight: CGFloat
        /// The widest a bubble gets, tail included; longer lines wrap.
        public let bubbleMaxWidth: CGFloat
        /// Around the bubble's card, so its shadow isn't clipped by the window.
        public let bubbleMargin: CGFloat
        /// The contact shadow's width, as a share of the window's.
        public let shadowWidthRatio: CGFloat

        public init(
            art: CharacterArt, shadowHeight: CGFloat = 4, bubbleMaxWidth: CGFloat = 240, bubbleMargin: CGFloat = 4,
            shadowWidthRatio: CGFloat = 0.42
        ) {
            aspect = art.viewBox.width / art.viewBox.height
            headHeightRatio = art.bubblePoint.y / art.viewBox.height
            bubbleTuck = art.rig.bubble.tuck
            self.shadowHeight = shadowHeight
            self.bubbleMaxWidth = bubbleMaxWidth
            self.bubbleMargin = bubbleMargin
            self.shadowWidthRatio = shadowWidthRatio
        }

        /// The desktop window's size for `size`.
        public func windowSize(for size: DesktopSize) -> CGSize {
            CGSize(width: (size.height * aspect).rounded(), height: size.height + shadowHeight)
        }

        /// The height (AppKit, y up) of the bubble pivot in a character window at `frame`.
        public func headY(in frame: CGRect) -> CGFloat {
            frame.maxY - (frame.height - shadowHeight) * headHeightRatio
        }

        /// The gap between the bubble and the character window `width` wide: negative, so the bubble tucks in.
        public func bubbleGap(width: CGFloat) -> CGFloat {
            -width * bubbleTuck
        }

        /// The tail's tip below the top of a bubble window `height` tall.
        public func bubbleTailY(height: CGFloat) -> CGFloat {
            bubbleMargin + BubbleShape.tailY(cardHeight: height - 2 * bubbleMargin)
        }
    }
#endif
