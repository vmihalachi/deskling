#if os(macOS)
    import AppKit
    import SwiftUI

    /// The language the app speaks, by name in that language's own words, and a button that opens System Settings'
    /// per-app language list. The words and the button style are the app's.
    public struct LanguageRow<Style: ButtonStyle>: View {
        let label: Text
        let change: Text
        let buttonStyle: Style
        @Environment(\.companionPalette) private var palette

        public init(label: Text, change: Text, buttonStyle: Style) {
            self.label = label
            self.change = change
            self.buttonStyle = buttonStyle
        }

        public var body: some View {
            HStack {
                label
                Spacer()
                Text(verbatim: Self.currentLanguageName()).foregroundStyle(palette.inkSoft)
                Button {
                    Self.openLanguageSettings()
                } label: {
                    change
                }
                .buttonStyle(buttonStyle)
            }
        }

        /// The app's language (the bundle's first preferred localization) named in the current locale.
        public static func currentLanguageName(bundle: Bundle = .main, locale: Locale = .current) -> String {
            let code = bundle.preferredLocalizations.first ?? "en"
            return locale.localizedString(forIdentifier: code)?.localizedCapitalized ?? code
        }

        /// System Settings → General → Language & Region, where Applications picks one per app.
        public static func openLanguageSettings() {
            if let url = URL(string: "x-apple.systempreferences:com.apple.Localization-Settings.extension") {
                NSWorkspace.shared.open(url)
            }
        }
    }
#endif
