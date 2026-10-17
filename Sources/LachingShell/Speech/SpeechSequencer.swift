#if os(macOS)
    import Foundation

    /// One beat of a spoken joke: its text (shown and, without a clip, synthesized), its recorded clip, if any, and the
    /// clip's mouth envelope (`VoiceLibrary.mouth(for:voice:)`: a level 0–3 per 1/`mouthFPS` s; empty for none).
    public struct SpeechPart: Equatable, Sendable {
        public var text: String
        public var clip: URL?
        public var mouth: [Int]
        public var mouthFPS: Double

        public init(text: String, clip: URL? = nil, mouth: [Int] = [], mouthFPS: Double = 12.5) {
            self.text = text
            self.clip = clip
            self.mouth = mouth
            self.mouthFPS = mouthFPS
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
        /// Audio started (true, just before a part is spoken) or ended (false: the part finished, so a beat or the end
        /// follows, or the sequence was stopped). Moves the character's mouth only while words are heard.
        public var onVoicingChanged: ((Bool) -> Void)?

        private let schedule: Schedule
        private var parts: [SpeechPart] = []
        private var index = 0
        private var beat: TimeInterval = 0
        private var voice: (identifier: String?, rate: Double, volume: Double) = (nil, 0.5, 1)
        /// Bumped by every `speak` and `stop`, so a beat or a finish that arrives late is ignored.
        private var generation = 0
        /// What `onVoicingChanged` last reported.
        private var voicing = false {
            didSet {
                if voicing != oldValue { onVoicingChanged?(voicing) }
            }
        }

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

        /// The playing clip's mouth level (0–3) right now, from its envelope; nil without one (the system voice, or a
        /// manifest without an envelope), so the mouth keeps its own timer. Past the envelope's end the mouth rests (0).
        public var mouthLevel: Int? {
            guard isVoicing, index < parts.count else { return nil }
            let part = parts[index]
            guard !part.mouth.isEmpty, part.mouthFPS > 0, let time = speaker.clipTime else { return nil }
            let frame = max(0, Int(time * part.mouthFPS))
            return frame < part.mouth.count ? part.mouth[frame] : 0
        }

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
            voicing = false
            onFinished?()
        }

        private func start(_ current: Int) {
            guard current == generation, isActive else { return }
            speaker.onFinished = { [weak self] in self?.partFinished(current) }
            onPartStarted?(index)
            let part = parts[index]
            voicing = true  // first: a voice may finish at once
            speaker.speak(part.text, clip: part.clip, voiceIdentifier: voice.identifier, rate: voice.rate, volume: voice.volume)
        }

        private func partFinished(_ current: Int) {
            guard current == generation, isActive else { return }
            speaker.onFinished = nil
            voicing = false
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
