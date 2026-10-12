#if os(macOS)
    import SwiftUI

    /// The display face's two weights an app ships.
    public enum DisplayWeight: Sendable {
        case medium, semiBold
    }

    extension View {
        /// The root of every window and the panel: `palette`'s paper behind, its ink for text at `bodySize`, `tint` for
        /// native controls (the palette's accent by default), and the palette and display face in the environment for
        /// the package's views.
        public func companionWindowRoot(
            palette: CompanionPalette, displayFont: @escaping @Sendable (CGFloat) -> Font, tint: Color? = nil, bodySize: CGFloat = 13
        ) -> some View {
            font(.system(size: bodySize))
                .foregroundStyle(palette.ink)
                .tint(tint ?? palette.accent)
                .background(palette.paper.ignoresSafeArea())
                .environment(\.companionPalette, palette)
                .environment(\.companionDisplayFont, displayFont)
        }
    }
#endif
