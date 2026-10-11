#if os(macOS)
    import Foundation

    /// One beat of a spoken joke: its text (shown and, without a clip, synthesized) and its recorded clip, if any.
    public struct SpeechPart: Equatable, Sendable {
        public var text: String
        public var clip: URL?

        public init(text: String, clip: URL? = nil) {
            self.text = text
            self.clip = clip
        }
    }

    /// Speaks a line in parts through a `Speaking` voice, with a silent beat between them: a setup, a pause, the
    /// punchline. It owns the voice's `onFinished` while it runs. From the first part until the last one finished it is
    /// `isActive`, beats included, so nothing else starts in the pause (the app's `Snapshot.isSpeaking`); `isVoicing` is
    /// whether audio is playing right now (the character's mouth), false during a beat. `stop()` cuts the part being
    /// spoken and cancels the one waiting. Needs the main actor; never sleeps a thread.
    @MainActor
    public final class SpeechSequencer {
        /// Runs `work` after `delay` seconds on the main actor; tests pass their own clock.
        public typealias Schedule = @MainActor (_ delay: TimeInterval, _ work: @escaping @MainActor @Sendable () -> Void) -> Void

        public let speaker: any Speaking
        /// From the first part until the last finished, beats included.
        public private(set) var isActive = false
        /// A part starts: its index, from 0. Show it in the bubble.
        public var onPartStarted: ((Int) -> Void)?
        /// The last part finished, or the sequence was stopped.
        public var onFinished: (() -> Void)?

        private let schedule: Schedule
        private var parts: [SpeechPart] = []
        private var index = 0
        private var beat: TimeInterval = 0
        private var voice: (identifier: String?, rate: Double, volume: Double) = (nil, 0.5, 1)
        /// Bumped by every `speak` and `stop`, so a beat or a finish that arrives late is ignored.
        private var generation = 0

        public init(speaker: any Speaking, schedule: Schedule? = nil) {
            self.speaker = speaker
            self.schedule =
                schedule ?? { delay, work in
                    Timer.scheduledTimer(withTimeInterval: delay, repeats: false) { _ in
                        MainActor.assumeIsolated { work() }
                    }
                }
        }

        /// Audio is playing right now.
        public var isVoicing: Bool { isActive && speaker.isSpeaking }

        /// Speaks `parts` in order, waiting `beat` seconds after each one before the next. Stops anything still running
        /// first. An empty list finishes at once.
        public func speak(_ parts: [SpeechPart], beat: TimeInterval, voiceIdentifier: String?, rate: Double, volume: Double) {
            stop()
            guard !parts.isEmpty else {
                onFinished?()
                return
            }
            generation += 1
            self.parts = parts
            self.beat = beat
            voice = (voiceIdentifier, rate, volume)
            index = 0
            isActive = true
            start(generation)
        }

        /// Cuts the part being spoken and cancels a pending one; reports `onFinished` once if anything was running.
        public func stop() {
            guard isActive else { return }
            generation += 1
            isActive = false
            parts = []
            speaker.onFinished = nil
            speaker.stop()
            onFinished?()
        }

        private func start(_ current: Int) {
            guard current == generation, isActive else { return }
            speaker.onFinished = { [weak self] in self?.partFinished(current) }
            onPartStarted?(index)
            let part = parts[index]
            speaker.speak(part.text, clip: part.clip, voiceIdentifier: voice.identifier, rate: voice.rate, volume: voice.volume)
        }

        private func partFinished(_ current: Int) {
            guard current == generation, isActive else { return }
            speaker.onFinished = nil
            index += 1
            guard index < parts.count else {
                isActive = false
                parts = []
                onFinished?()
                return
            }
            schedule(beat) { [weak self] in self?.start(current) }
        }
    }
#endif
