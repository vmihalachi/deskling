#if os(macOS)
    import Foundation
    import LachingCompanionUI
    import LachingShell

    /// A voice that records what it says and finishes when the test says so.
    @MainActor
    final class FakeSpeaker: Speaking {
        private(set) var spoken: [String] = []
        private(set) var clips: [URL?] = []
        private(set) var stops = 0
        var isSpeaking = false
        var onFinished: (() -> Void)?

        func speak(_ text: String, clip: URL?, voiceIdentifier: String?, rate: Double, volume: Double) {
            spoken.append(text)
            clips.append(clip)
            isSpeaking = true
        }

        func stop() {
            stops += 1
            finish()
        }

        /// The part ended.
        func finish() {
            guard isSpeaking else { return }
            isSpeaking = false
            onFinished?()
        }
    }

    /// Counts the sounds played; each finishes at once.
    @MainActor
    final class FakeChime: ChimePlaying {
        private(set) var played: [String] = []

        func play(_ sound: String, volume: Double, completion: (() -> Void)?) {
            played.append(sound)
            completion?()
        }

        func stop() {}
    }

    /// Counts permission requests and posted lines; grants by default.
    @MainActor
    final class FakeNotifier: CompanionNotifying {
        var onAction: ((String) -> Void)?
        private(set) var requests = 0
        private(set) var posted: [String] = []
        var granted = true

        func configure() {}

        func requestAuthorization(completion: @escaping @MainActor (Bool) -> Void) {
            requests += 1
            completion(granted)
        }

        func refreshAuthorization(completion: @escaping @MainActor (NotificationPermission) -> Void) {
            completion(granted ? .authorized : .notDetermined)
        }

        func post(line: String) {
            posted.append(line)
        }
    }
#endif
