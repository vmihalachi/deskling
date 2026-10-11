#if os(macOS)
    import CryptoKit
    import Foundation

    /// A recorded voice of the app's character, such as "male" or "female": the app names its own. Raw values are
    /// stored in settings and name the folders under `Voice/<lang>/`; never rename one.
    public struct VoiceID: RawRepresentable, Hashable, Codable, Sendable, ExpressibleByStringLiteral {
        public let rawValue: String

        public init(rawValue: String) {
            self.rawValue = rawValue
        }

        public init(stringLiteral value: String) {
            self.init(rawValue: value)
        }

        public init(from decoder: Decoder) throws {
            self.init(rawValue: try decoder.singleValueContainer().decode(String.self))
        }

        public func encode(to encoder: Encoder) throws {
            var container = encoder.singleValueContainer()
            try container.encode(rawValue)
        }
    }

    /// A character's recorded voices: `Voice/<lang>/<voice>/<clip id>.m4a` and `Voice/manifest.json` (format 1, made by
    /// `tools/voice.py`). Says which clip speaks a text in the app's language and a voice, and which clips are pending
    /// there (none, or one of older text), so the app never picks a line it can't say. A clip counts only when its
    /// manifest `textHash` matches the current text, so the bubble and the audio never disagree. A language marked
    /// `testing` plays only when `allowTesting` (Debug builds); a language without a recorded voice keeps the system
    /// voice. With `fallbackToSystemVoice`, nothing is pending: a missing clip is spoken by the system voice instead.
    /// Clip ids are the phrase's (`<id>`, or `<id>.1`, `<id>.2` for the parts of a joke); the library knows no bank.
    /// Reads files only; never touches the network.
    @MainActor
    public final class VoiceLibrary {
        struct Manifest: Decodable {
            struct Language: Decodable {
                var status: String
                var `default`: String
                var voices: [String: Voice]
            }
            struct Voice: Decodable {
                var clips: [String: Clip]
            }
            struct Clip: Decodable {
                var textHash: String
                var duration: Double
                /// One mouth opening level, 0–3, per 1/`mouthFPS` s.
                var mouth: String
            }
            var format: Int
            var mouthFPS: Double
            var languages: [String: Language]
        }

        public static var isDebugBuild: Bool {
            #if DEBUG
                true
            #else
                false
            #endif
        }

        public let languageCode: String
        /// Mouth levels per second in the manifest (12.5 when there is none).
        public let mouthFPS: Double
        /// A missing clip is spoken by the system voice instead of keeping its line back.
        public let fallbackToSystemVoice: Bool
        private let folder: URL?
        private let language: Manifest.Language?

        /// The `Voice` folder in `bundle`'s resources.
        public convenience init(
            bundle: Bundle = .main, language code: String = Speaker.appLanguage(), allowTesting: Bool = VoiceLibrary.isDebugBuild,
            fallbackToSystemVoice: Bool = false
        ) {
            self.init(
                folder: bundle.resourceURL?.appendingPathComponent("Voice", isDirectory: true), language: code,
                allowTesting: allowTesting, fallbackToSystemVoice: fallbackToSystemVoice)
        }

        /// `folder` holds `manifest.json` and `<lang>/<voice>/<id>.m4a`; nil (or no manifest) means no recorded voice.
        public init(folder: URL?, language code: String, allowTesting: Bool, fallbackToSystemVoice: Bool = false) {
            let manifest =
                folder
                .flatMap { try? Data(contentsOf: $0.appendingPathComponent("manifest.json")) }
                .flatMap { try? JSONDecoder().decode(Manifest.self, from: $0) }
            let language = manifest?.languages[code]
            languageCode = code
            mouthFPS = manifest?.mouthFPS ?? 12.5
            self.fallbackToSystemVoice = fallbackToSystemVoice
            self.folder = folder
            if let language, language.status == "shipping" || (allowTesting && language.status == "testing") {
                self.language = language
            } else {
                self.language = nil
            }
        }

        /// The voices with clips in this language, the language's default first, then by name. Empty when the language
        /// has no recorded voice.
        public var availableVoices: [VoiceID] {
            guard let language else { return [] }
            let names = language.voices.filter { !$0.value.clips.isEmpty }.keys.sorted()
            return names.sorted { a, b in a == language.default && b != language.default }.map(VoiceID.init(rawValue:))
        }

        /// The language speaks with a recorded voice.
        public var isVoiced: Bool { !availableVoices.isEmpty }

        /// The voice that speaks: `preferred` when this language has it, else the language's default.
        public func voice(preferred: VoiceID?) -> VoiceID? {
            if let preferred, availableVoices.contains(preferred) { return preferred }
            return availableVoices.first
        }

        /// The clip that says `text` as clip `id` in `voice`, or nil when there's none for this exact text.
        public func clip(for id: String, text: String, voice: VoiceID) -> URL? {
            guard let entry = language?.voices[voice.rawValue]?.clips[id], entry.textHash == Self.hash(text),
                let url = folder?.appendingPathComponent("\(languageCode)/\(voice.rawValue)/\(id).m4a"),
                FileManager.default.fileExists(atPath: url.path)
            else { return nil }
            return url
        }

        /// The clip's mouth levels (0–3), one per 1/`mouthFPS` s.
        public func mouth(for id: String, voice: VoiceID) -> [Int] {
            language?.voices[voice.rawValue]?.clips[id]?.mouth.compactMap(\.wholeNumberValue) ?? []
        }

        /// The ids among `clips` that `voice` can't say yet in this language; none with `fallbackToSystemVoice`.
        public func pendingClipIDs(_ clips: [(id: String, text: String)], voice: VoiceID) -> Set<String> {
            guard !fallbackToSystemVoice else { return [] }
            return Set(clips.filter { clip(for: $0.id, text: $0.text, voice: voice) == nil }.map(\.id))
        }

        /// The manifest's `textHash`: SHA-256 of the UTF-8 text, lowercase hex.
        public nonisolated static func hash(_ text: String) -> String {
            SHA256.hash(data: Data(text.utf8)).map { String(format: "%02x", $0) }.joined()
        }
    }
#endif
