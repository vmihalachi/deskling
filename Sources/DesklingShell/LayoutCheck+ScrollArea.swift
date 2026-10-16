#if os(macOS) || os(iOS)
    import CoreGraphics

    extension LayoutCheck {
        /// Where a scroll view's content lies, in the window's coordinates: `content` (its content and its own frame),
        /// open-ended along each way it scrolls. A lazy stack reports frames past the content's end for rows it hasn't
        /// laid out yet, so along the way it scrolls anything counts as scrolled to; across it, only `content` does.
        public struct ScrollArea: Equatable, Sendable {
            public let content: CGRect
            public let vertical: Bool
            public let horizontal: Bool

            public init(content: CGRect, vertical: Bool = false, horizontal: Bool = false) {
                self.content = content
                self.vertical = vertical
                self.horizontal = horizontal
            }

            /// Whether `point` lies in the content, or anywhere along a way it scrolls.
            public func contains(_ point: CGPoint) -> Bool {
                (horizontal || (content.minX...content.maxX).contains(point.x))
                    && (vertical || (content.minY...content.maxY).contains(point.y))
            }
        }
    }
#endif
