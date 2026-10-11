#if os(macOS)
    import AVFoundation
    import Foundation

    /// What an app needs from a voice, so tests can speak silently.
    @MainActor
    public protocol Speaking: AnyObject {
        var isSpeaking: Bool { get }
        /// Called once a line finished or was stopped.
        var onFinished: (() -> Void)? { get set }
        /// Plays `clip` (a recorded voice) when given, else says `text` with the system voice.
        func speak(_ text: String, clip: URL?, voiceIdentifier: String?, rate: Double, volume: Double)
        func stop()
    }

    /// A character's voice: a recorded clip (`VoiceLibrary`) when there is one, else the system speech synthesizer with
    /// the user's voice. The user's rate and volume apply to both. One line at a time; `stop()` cuts a line mid-word (a
    /// call started). Never records anything and asks for no permission.
    @MainActor
    public final class Speaker: NSObject, Speaking, AVSpeechSynthesizerDelegate, AVAudioPlayerDelegate {
        private let synthesizer = AVSpeechSynthesizer()
        /// The line being spoken; a delegate callback for any other utterance is stale and ignored.
        private var current: AVSpeechUtterance?
        /// The clip being played, the same way.
        private var player: AVAudioPlayer?
        /// The language whose best voice speaks when the user chose none.
        public let language: String
        public private(set) var isSpeaking = false
        public var onFinished: (() -> Void)?

        public init(language: String = Speaker.appLanguage()) {
            self.language = language
            super.init()
            synthesizer.delegate = self
        }

        public func speak(_ text: String, clip: URL?, voiceIdentifier: String?, rate: Double, volume: Double) {
            stop()
            if let clip {
                play(clip, rate: rate, volume: volume)
                return
            }
            let utterance = AVSpeechUtterance(string: text)
            utterance.voice = Self.voice(identifier: voiceIdentifier, language: language)
            utterance.rate = Self.speechRate(from: rate)
            utterance.volume = Float(min(max(volume, 0), 1))
            current = utterance
            isSpeaking = true
            synthesizer.speak(utterance)
        }

        public func stop() {
            guard isSpeaking else { return }
            current = nil
            isSpeaking = false
            synthesizer.stopSpeaking(at: .immediate)
            player?.stop()
            player = nil
            onFinished?()
        }

        /// A clip that can't be read or played ends the line at once, so nobody waits for it.
        private func play(_ clip: URL, rate: Double, volume: Double) {
            guard let player = try? AVAudioPlayer(contentsOf: clip) else {
                onFinished?()
                return
            }
            player.delegate = self
            player.enableRate = true
            player.rate = Self.clipRate(from: rate)
            player.volume = Float(min(max(volume, 0), 1))
            self.player = player
            isSpeaking = player.play()
            if !isSpeaking {
                self.player = nil
                onFinished?()
            }
        }

        /// The settings' 0…1 scale onto a recorded clip's playback rate: 0.5 plays it as recorded, and the ends stay
        /// close (0.85×–1.2×) so the time stretch keeps the voice natural (AVAudioPlayer keeps the pitch).
        public nonisolated static func clipRate(from unit: Double) -> Float {
            let unit = Float(min(max(unit, 0), 1))
            return unit <= 0.5 ? 0.85 + 0.15 * (unit / 0.5) : 1 + 0.2 * ((unit - 0.5) / 0.5)
        }

        /// The settings' 0…1 scale onto the synthesizer's: 0.5 is the platform's normal rate.
        public nonisolated static func speechRate(from unit: Double) -> Float {
            let unit = Float(min(max(unit, 0), 1))
            if unit <= 0.5 {
                return AVSpeechUtteranceMinimumSpeechRate
                    + (AVSpeechUtteranceDefaultSpeechRate - AVSpeechUtteranceMinimumSpeechRate) * (unit / 0.5)
            }
            return AVSpeechUtteranceDefaultSpeechRate
                + (AVSpeechUtteranceMaximumSpeechRate - AVSpeechUtteranceDefaultSpeechRate) * ((unit - 0.5) / 0.5)
        }

        /// The chosen voice, or the best installed voice for `language` (nil leaves it to the system).
        public static func voice(identifier: String?, language: String) -> AVSpeechSynthesisVoice? {
            if let identifier, let voice = AVSpeechSynthesisVoice(identifier: identifier) { return voice }
            return voices(forLanguage: language).first
        }

        /// The app's language code, e.g. "en", "pt-BR": the bundle's first preferred localization.
        public nonisolated static func appLanguage(bundle: Bundle = .main) -> String {
            bundle.preferredLocalizations.first ?? "en"
        }

        /// Installed voices for a language, best quality first, then by name. "pt-BR" matches "pt-BR" voices first and
        /// other Portuguese voices after.
        public static func voices(forLanguage code: String) -> [AVSpeechSynthesisVoice] {
            let base = String(code.split(separator: "-").first ?? Substring(code)).lowercased()
            let wanted = code.lowercased()
            let matching = AVSpeechSynthesisVoice.speechVoices().filter { voice in
                voice.language.lowercased().split(separator: "-").first.map(String.init) == base
            }
            return matching.sorted { a, b in
                let aExact = a.language.lowercased() == wanted
                let bExact = b.language.lowercased() == wanted
                if aExact != bExact { return aExact }
                if a.quality != b.quality { return a.quality.rawValue > b.quality.rawValue }
                return a.name.localizedStandardCompare(b.name) == .orderedAscending
            }
        }

        // MARK: AVAudioPlayerDelegate (called off the main thread)

        public nonisolated func audioPlayerDidFinishPlaying(_ player: AVAudioPlayer, successfully flag: Bool) {
            let id = ObjectIdentifier(player)
            DispatchQueue.main.async {
                MainActor.assumeIsolated { self.clipFinished(id) }
            }
        }

        private func clipFinished(_ id: ObjectIdentifier) {
            guard isSpeaking, player.map(ObjectIdentifier.init) == id else { return }
            player = nil
            isSpeaking = false
            onFinished?()
        }

        // MARK: AVSpeechSynthesizerDelegate (called off the main thread)

        public nonisolated func speechSynthesizer(_ synthesizer: AVSpeechSynthesizer, didFinish utterance: AVSpeechUtterance) {
            finished(utterance)
        }

        public nonisolated func speechSynthesizer(_ synthesizer: AVSpeechSynthesizer, didCancel utterance: AVSpeechUtterance) {
            finished(utterance)
        }

        /// Hops to the main actor with the utterance's identity (Sendable), not the utterance itself.
        private nonisolated func finished(_ utterance: AVSpeechUtterance) {
            let id = ObjectIdentifier(utterance)
            DispatchQueue.main.async {
                MainActor.assumeIsolated { self.didFinish(id) }
            }
        }

        private func didFinish(_ id: ObjectIdentifier) {
            guard isSpeaking, current.map(ObjectIdentifier.init) == id else { return }
            current = nil
            isSpeaking = false
            onFinished?()
        }
    }
#endif
