#if os(macOS)
    import SwiftUI

    /// The bottom band of a window with buttons: a hairline in the palette's line color over a row inset like the
    /// content.
    public struct ActionBar<Content: View>: View {
        let content: Content
        @Environment(\.companionPalette) private var palette

        public init(@ViewBuilder content: () -> Content) {
            self.content = content()
        }

        public var body: some View {
            VStack(spacing: 0) {
                Rectangle().fill(palette.line).frame(height: 1).accessibilityHidden(true)
                HStack(spacing: Space.item) { content }
                    .padding(.horizontal, WindowInset.side)
                    .padding(.vertical, Space.group)
            }
        }
    }
#endif
