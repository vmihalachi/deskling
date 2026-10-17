#if os(macOS) || os(iOS)
    import CoreGraphics

    extension LayoutCheck {
        /// Content that needs `needed` points of height where it has `available`: the room of a Mac window below its
        /// title bar, or of an iPhone or iPad screen outside a scroll view.
        public struct Overflow: Equatable, Sendable, CustomStringConvertible {
            public let needed: CGFloat
            public let available: CGFloat

            public init(needed: CGFloat, available: CGFloat) {
                self.needed = needed
                self.available = available
            }

            public var description: String {
                "the content needs \(Int(needed.rounded(.up))) pt of height, the room has \(Int(available.rounded()))"
            }
        }

        /// Points of rounding allowed between the measurement and the room.
        public static let slack: CGFloat = 1
    }
#endif
