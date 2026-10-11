#if os(macOS)
    import DesklingCompanion
    import DesklingShell
    import Foundation

    /// Turns a `Delivery` into what the user sees and hears: the bubble text (part by part for a joke), the blip and the
    /// words through a `SpeechSequencer`, a tune on the chime, and one notification when the punchline lands (both parts
    /// on two lines). A line is said with its recorded clip in `voice` when the language has a recorded voice (a line
    /// without a current recording stays unsaid unless the library falls back to the system voice), else with the system
    /// voice. Without speech the parts follow each other after `PhraseTiming.partDelay`. `isSpeaking` holds from the
    /// first part until the last finished, beats included, so the app's `Snapshot.isSpeaking` keeps other lines out; a
    /// call or Shh cuts the whole joke with `stop()`. The app supplies the words (`localize`, from its String Catalog)
    /// and the tune's bubble text; the package ships none. Needs the main actor; never touches the network.
    @MainActor
    public final class DeliveryPerformer {
        public let bank: PhraseBank
        public let speaker: any Speaking
        public let chime: any ChimePlaying
        public let voices: VoiceLibrary
        public let sequencer: SpeechSequencer
        /// The text the user sees for a catalog key; the source text by default.
        public var localize: (_ catalogKey: String, _ source: String) -> String = { _, source in source }
        /// The bubble while a tune plays, such as "♪ hm hm hmm ♪".
        public var tuneBubbleText: () -> String = { "♪" }
        /// The sound played before the words when the chime channel is on; nil plays none.
        public var blipSound: String?
        /// The bubble shows this text: the line, or the part of a joke being told.
        public var onBubble: ((String) -> Void)?
        /// Speech started (after the blip): show the talking character, watch for calls.
        public var onSpeechStarted: (() -> Void)?
        /// The last part was said, or speech was stopped.
        public var onSpeechFinished: (() -> Void)?

        private let post: ((String) -> Void)?
        private let schedule: SpeechSequencer.Schedule
        /// Bumped by every delivery and `stop()`, so a blip or a silent part that ends late does nothing.
        private var generation = 0

        /// `post` shows a notification body (nil: the app shows none); `schedule` runs the silent parts' timers (tests
        /// pass their own clock).
        public init(
            bank: PhraseBank, speaker: any Speaking, chime: any ChimePlaying, voices: VoiceLibrary, post: ((String) -> Void)?,
            schedule: SpeechSequencer.Schedule? = nil
        ) {
            self.bank = bank
            self.speaker = speaker
            self.chime = chime
            self.voices = voices
            self.post = post
            let schedule =
                schedule ?? { delay, work in
                    Timer.scheduledTimer(withTimeInterval: delay, repeats: false) { _ in
                        MainActor.assumeIsolated { work() }
                    }
                }
            self.schedule = schedule
            sequencer = SpeechSequencer(speaker: speaker, schedule: schedule)
            sequencer.onFinished = { [weak self] in self?.onSpeechFinished?() }
        }

        /// From the first part until the last finished, beats included.
        public var isSpeaking: Bool { sequencer.isActive }

        /// Audio is playing right now (the character's mouth).
        public var isVoicing: Bool { sequencer.isVoicing }

        /// Performs `delivery` as its plan says, with `settings`' voice, rate and volume and the recorded `voice`.
        public func perform(_ delivery: Delivery, settings: CompanionSettings, voice: VoiceID?) {
            generation += 1
            let current = generation
            let phrase = delivery.phrase
            let plan = delivery.plan
            if case .tune(let sound) = phrase.kind {
                onBubble?(tuneBubbleText())
                chime.play(sound, volume: settings.speechVolume, completion: nil)
                return
            }
            let parts = texts(of: phrase)
            guard !parts.isEmpty else { return }
            let clips = plan.speak ? self.clips(of: phrase, texts: parts, voice: voice) : nil
            let notify = plan.notify ? post : nil
            onBubble?(parts[0])
            if parts.count == 1 { notify?(parts[0]) }
            guard let clips else {
                showSilently(parts, from: 0, notify: notify, generation: current)
                return
            }
            let speak = { [weak self] in
                guard let self, self.generation == current else { return }
                self.say(parts, clips: clips, settings: settings, notify: notify)
            }
            if plan.chime, let blipSound {
                chime.play(blipSound, volume: settings.speechVolume) { speak() }
            } else {
                speak()
            }
        }

        /// Says `phrase` now, the way Settings plays a sample: spoken, no chime, no notification.
        public func preview(_ phrase: Phrase, settings: CompanionSettings, voice: VoiceID?) {
            stop()
            perform(
                Delivery(phrase: phrase, trigger: .buttonPress, plan: ChannelPlan(speak: true, chime: false, notify: false)),
                settings: settings, voice: voice)
        }

        /// The phrases `voice` can't say yet in this language: give them to `CompanionCoordinator.unavailableIDs`. None
        /// without a recorded voice.
        public func unavailableIDs(voice: VoiceID?) -> Set<String> {
            guard let voice else { return [] }
            return voices.pendingIDs(in: bank, voice: voice, text: localize)
        }

        /// Cuts the line or joke being told, and the parts still waiting.
        public func stop() {
            generation += 1
            sequencer.stop()
        }

        // MARK: Private

        private func texts(of phrase: Phrase) -> [String] {
            switch phrase.kind {
            case .line(let source): [localize(phrase.catalogKey, source)]
            case .sequence(let sources): zip(phrase.catalogKeys, sources).map { localize($0, $1) }
            case .tune: []
            }
        }

        /// The clip for each part; nil when the line can't be said (a recorded voice lacks one and doesn't fall back).
        private func clips(of phrase: Phrase, texts: [String], voice: VoiceID?) -> [URL?]? {
            guard let voice, voices.isVoiced else { return texts.map { _ in nil } }
            let clips = zip(phrase.clipIDs, texts).map { voices.clip(for: $0, text: $1, voice: voice) }
            if clips.contains(nil) && !voices.fallbackToSystemVoice { return nil }
            return clips
        }

        private func say(_ parts: [String], clips: [URL?], settings: CompanionSettings, notify: ((String) -> Void)?) {
            let last = parts.count - 1
            sequencer.onPartStarted = { [weak self] index in
                guard let self else { return }
                if index > 0 { self.onBubble?(parts[index]) }
                if index == last, last > 0 { notify?(parts.joined(separator: "\n")) }
            }
            onSpeechStarted?()
            sequencer.speak(
                zip(parts, clips).map { SpeechPart(text: $0, clip: $1) }, beat: PhraseTiming.partBeat,
                voiceIdentifier: settings.voiceIdentifier, rate: settings.speechRate, volume: settings.speechVolume)
        }

        /// Shows the parts one after the other, each as long as its bubble would stay.
        private func showSilently(_ parts: [String], from index: Int, notify: ((String) -> Void)?, generation current: Int) {
            guard index + 1 < parts.count else { return }
            schedule(PhraseTiming.partDelay(spoken: false, previousPart: parts[index])) { [weak self] in
                guard let self, self.generation == current else { return }
                self.onBubble?(parts[index + 1])
                if index + 2 == parts.count { notify?(parts.joined(separator: "\n")) }
                self.showSilently(parts, from: index + 1, notify: notify, generation: current)
            }
        }
    }
#endif
