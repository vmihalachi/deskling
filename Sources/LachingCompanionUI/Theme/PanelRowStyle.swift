#if os(macOS)
    import SwiftUI

    /// A row in the menu bar panel: plain 13 pt text in ink that fills the palette's `accentSoft` under the pointer.
    public struct PanelRowStyle: ButtonStyle {
        public init() {}

        public func makeBody(configuration: Configuration) -> some View {
            PanelRow(configuration: configuration)
        }

        private struct PanelRow: View {
            let configuration: ButtonStyleConfiguration
            @State private var hovering = false
            @Environment(\.companionPalette) private var palette

            var body: some View {
                configuration.label
                    .font(.system(size: 13))
                    .foregroundStyle(palette.ink)
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(.horizontal, Space.snug)
                    .padding(.vertical, Space.tight + 1)
                    .background(
                        RoundedRectangle(cornerRadius: 6, style: .continuous)
                            .fill(hovering || configuration.isPressed ? palette.accentSoft : Color.clear)
                    )
                    .contentShape(Rectangle())
                    .onHover { hovering = $0 }
            }
        }
    }
#endif
