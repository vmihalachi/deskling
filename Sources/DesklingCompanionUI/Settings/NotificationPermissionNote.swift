#if os(macOS)
    import SwiftUI

    /// "Notifications are off for the app in System Settings" with a button to turn them on, shown only while the
    /// permission is denied. In the palette's accent text color; the words and the button style are the app's.
    public struct NotificationPermissionNote<Style: ButtonStyle>: View {
        let permission: NotificationPermission
        let text: Text
        let open: Text
        let buttonStyle: Style
        let action: () -> Void
        @Environment(\.companionPalette) private var palette

        public init(permission: NotificationPermission, text: Text, open: Text, buttonStyle: Style, action: @escaping () -> Void) {
            self.permission = permission
            self.text = text
            self.open = open
            self.buttonStyle = buttonStyle
            self.action = action
        }

        public var body: some View {
            if permission == .denied {
                HStack(spacing: Space.snug) {
                    Image(systemName: "bell.slash")
                    text
                        .fixedSize(horizontal: false, vertical: true)
                    Spacer()
                    Button(action: action) { open }
                        .buttonStyle(buttonStyle)
                }
                .foregroundStyle(palette.accentText)
            }
        }
    }
#endif
