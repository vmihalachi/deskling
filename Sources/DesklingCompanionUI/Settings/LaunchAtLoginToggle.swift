#if os(macOS)
    import DesklingShell
    import SwiftUI

    /// The Launch at login toggle (`LoginItem`), with a line under it when macOS refuses the change. Two rows in a
    /// `Form`. The words and the error's color are the app's.
    public struct LaunchAtLoginToggle: View {
        let label: Text
        let errorText: (String) -> String
        let errorColor: Color
        @State private var isOn = LoginItem.isEnabled
        @State private var error: String?

        /// `errorText` turns macOS's reason into the line shown ("Couldn't change the login item: …").
        public init(label: Text, errorText: @escaping (String) -> String, errorColor: Color) {
            self.label = label
            self.errorText = errorText
            self.errorColor = errorColor
        }

        public var body: some View {
            Toggle(isOn: $isOn) { label }
                .onChange(of: isOn) { _, on in set(on) }
            if let error {
                Text(verbatim: error)
                    .font(.system(size: 12))
                    .foregroundStyle(errorColor)
            }
        }

        private func set(_ on: Bool) {
            guard on != LoginItem.isEnabled else { return }
            do {
                try LoginItem.set(on)
                error = nil
            } catch {
                self.error = errorText(error.localizedDescription)
                isOn = LoginItem.isEnabled
            }
        }
    }
#endif
