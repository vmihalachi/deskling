#if os(macOS)
    import SwiftUI

    /// A short status in capitals on an `accentSoft` capsule ("Next in about 20 min", "Quiet: you're on a call"), drawn
    /// with the `companionPalette`. The text is the app's, already localized.
    public struct StatusPill: View {
        public let text: String
        @Environment(\.companionPalette) private var palette

        public init(_ text: String) {
            self.text = text
        }

        public var body: some View {
            Text(verbatim: text.localizedUppercase)
                .font(.system(size: 11, weight: .semibold))
                .tracking(0.8)
                .foregroundStyle(palette.accentText)
                .lineLimit(1)
                .padding(.horizontal, Space.item)
                .padding(.vertical, Space.tight + 1)
                .background(Capsule().fill(palette.accentSoft))
        }
    }
#endif
