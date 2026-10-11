#if os(macOS)
    import AppKit
    import DesklingCompanion
    import SwiftUI

    /// The app's colors for the companion's shared views (the speech bubble, the status pill, the character's paints).
    /// The UI roles follow the app's design (an accent, a secondary color, paper and ink); `paints` maps the paint roles
    /// its character art names to colors. Set it once with `.environment(\.companionPalette, …)`; the package ships a
    /// plain system palette as the default, with every paint in the primary label color.
    public struct CompanionPalette: Sendable {
        public var paper, paper2, card, ink, inkSoft, line, outline, shade: Color
        public var accent, accentText, accentSoft, secondary, secondaryText, secondarySoft: Color
        public var onInk, onAccent: Color
        /// The color of a character paint, such as "petal".
        public var paints: @Sendable (PaintRole) -> Color

        public init(
            paper: Color, paper2: Color, card: Color, ink: Color, inkSoft: Color, line: Color, outline: Color, shade: Color,
            accent: Color, accentText: Color, accentSoft: Color, secondary: Color, secondaryText: Color, secondarySoft: Color,
            onInk: Color, onAccent: Color, paints: @escaping @Sendable (PaintRole) -> Color
        ) {
            (self.paper, self.paper2, self.card, self.ink, self.inkSoft, self.line, self.outline, self.shade) =
                (paper, paper2, card, ink, inkSoft, line, outline, shade)
            (self.accent, self.accentText, self.accentSoft) = (accent, accentText, accentSoft)
            (self.secondary, self.secondaryText, self.secondarySoft) = (secondary, secondaryText, secondarySoft)
            (self.onInk, self.onAccent) = (onInk, onAccent)
            self.paints = paints
        }

        /// System colors: the default until the app sets its own.
        public static let system = CompanionPalette(
            paper: Color(nsColor: .windowBackgroundColor), paper2: Color(nsColor: .underPageBackgroundColor),
            card: Color(nsColor: .textBackgroundColor), ink: .primary, inkSoft: .secondary,
            line: Color(nsColor: .separatorColor), outline: Color(nsColor: .tertiaryLabelColor), shade: .black.opacity(0.15),
            accent: .accentColor, accentText: .accentColor, accentSoft: .accentColor.opacity(0.2), secondary: .green,
            secondaryText: .green, secondarySoft: .green.opacity(0.2), onInk: Color(nsColor: .windowBackgroundColor),
            onAccent: .white, paints: { _ in .primary })

        /// A color that follows the appearance: `light` (0xRRGGBB) in Aqua, `dark` in Dark Aqua.
        public static func dynamic(light: UInt32, dark: UInt32, lightAlpha: CGFloat = 1, darkAlpha: CGFloat = 1) -> Color {
            Color(
                nsColor: NSColor(name: nil) { appearance in
                    let isDark = appearance.bestMatch(from: [.aqua, .darkAqua]) == .darkAqua
                    return isDark ? rgb(dark, alpha: darkAlpha) : rgb(light, alpha: lightAlpha)
                })
        }

        /// One fixed sRGB color (0xRRGGBB), for paints that look the same in light and dark.
        public static func fixed(_ hex: UInt32, alpha: CGFloat = 1) -> Color {
            Color(nsColor: rgb(hex, alpha: alpha))
        }

        private static func rgb(_ hex: UInt32, alpha: CGFloat) -> NSColor {
            NSColor(
                srgbRed: CGFloat((hex >> 16) & 0xFF) / 255, green: CGFloat((hex >> 8) & 0xFF) / 255,
                blue: CGFloat(hex & 0xFF) / 255, alpha: alpha)
        }
    }

    private struct CompanionPaletteKey: EnvironmentKey {
        static let defaultValue = CompanionPalette.system
    }

    private struct CompanionDisplayFontKey: EnvironmentKey {
        static let defaultValue: @Sendable (CGFloat) -> Font = { .system(size: $0, weight: .medium, design: .rounded) }
    }

    extension EnvironmentValues {
        /// The colors the companion's shared views draw with.
        public var companionPalette: CompanionPalette {
            get { self[CompanionPaletteKey.self] }
            set { self[CompanionPaletteKey.self] = newValue }
        }

        /// The app's display face at a size, for the speech bubble; the system's rounded face by default.
        public var companionDisplayFont: @Sendable (CGFloat) -> Font {
            get { self[CompanionDisplayFontKey.self] }
            set { self[CompanionDisplayFontKey.self] = newValue }
        }
    }
#endif
