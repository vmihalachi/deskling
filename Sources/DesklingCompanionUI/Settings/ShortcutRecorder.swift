#if os(macOS)
    import DesklingCore
    import DesklingShell
    import SwiftUI

    /// Click, then press a key combo with ⌘, ⌃ or ⌥ (`ShortcutCapture`). Esc cancels, Delete clears, and the cross
    /// clears too. Every word is the app's; the button takes its button style. `onCaptureStarted` runs before recording
    /// (unregister the hotkey), `onCaptureEnded` whenever it ends, however it ends (register it again).
    public struct ShortcutRecorder<Style: ButtonStyle>: View {
        @Binding var shortcut: KeyShortcut?
        let pressKeys: Text
        let record: Text
        let clear: Text
        let hint: Text
        let buttonStyle: Style
        let onCaptureStarted: () -> Void
        let onCaptureEnded: () -> Void
        @StateObject private var capture = ShortcutCapture()
        @Environment(\.companionPalette) private var palette

        /// `pressKeys` shows while recording, `record` with no shortcut set; `clear` labels the cross for VoiceOver and
        /// `hint` explains the button.
        public init(
            shortcut: Binding<KeyShortcut?>, pressKeys: Text, record: Text, clear: Text, hint: Text, buttonStyle: Style,
            onCaptureStarted: @escaping () -> Void, onCaptureEnded: @escaping () -> Void
        ) {
            _shortcut = shortcut
            self.pressKeys = pressKeys
            self.record = record
            self.clear = clear
            self.hint = hint
            self.buttonStyle = buttonStyle
            self.onCaptureStarted = onCaptureStarted
            self.onCaptureEnded = onCaptureEnded
        }

        public var body: some View {
            HStack(spacing: 6) {
                Button {
                    if capture.isCapturing { capture.stop() } else { start() }
                } label: {
                    if capture.isCapturing {
                        pressKeys
                    } else if let shortcut {
                        Text(verbatim: shortcut.displayString).monospaced()
                    } else {
                        record
                    }
                }
                .buttonStyle(buttonStyle)
                .accessibilityHint(hint)
                if shortcut != nil, !capture.isCapturing {
                    Button {
                        shortcut = nil
                    } label: {
                        Image(systemName: "xmark.circle.fill")
                    }
                    .buttonStyle(.plain)
                    .foregroundStyle(palette.inkSoft)
                    .accessibilityLabel(clear)
                }
            }
            .onAppear {
                capture.onCaptured = { captured in shortcut = captured }
            }
            // Every way recording ends (a combo, Delete, Esc inside the capture, the button) hands the hotkey back.
            .onChange(of: capture.isCapturing) { _, capturing in
                if !capturing { onCaptureEnded() }
            }
            .onDisappear { if capture.isCapturing { capture.stop() } }
        }

        private func start() {
            onCaptureStarted()
            capture.start()
        }
    }
#endif
