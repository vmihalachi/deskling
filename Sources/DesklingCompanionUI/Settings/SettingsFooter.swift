#if os(macOS)
    import SwiftUI

    /// A Settings section's footer: 12 pt in the palette's soft ink, wrapping.
    public struct SettingsFooter: View {
        let text: Text
        @Environment(\.companionPalette) private var palette

        public init(_ text: Text) {
            self.text = text
        }

        public var body: some View {
            text
                .font(.system(size: 12))
                .foregroundStyle(palette.inkSoft)
                .fixedSize(horizontal: false, vertical: true)
        }
    }
#endif
