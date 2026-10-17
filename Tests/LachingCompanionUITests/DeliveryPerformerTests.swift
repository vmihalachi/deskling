#if os(macOS)
    import Foundation
    import LachingCompanion
    import LachingCompanionUI
    import LachingCore
    import LachingShell
    import LachingTesting
    import XCTest

    @MainActor
    final class DeliveryPerformerTests: XCTestCase {
        private let settings = CompanionSettings(store: InMemorySettingsStore(), defaults: .fixture)
        private var bubbles: [String] = []
        private var posts: [String] = []
        private var events: [String] = []
        private var timers: [(TimeInterval, @MainActor @Sendable () -> Void)] = []

        private func performer(
            speaker: any Speaking, chime: any ChimePlaying = SilentChime(), voices: VoiceLibrary? = nil
        )
            -> DeliveryPerformer
        {
            let performer = DeliveryPerformer(
                bank: .fixture, speaker: speaker, chime: chime,
                voices: voices ?? VoiceLibrary(folder: nil, language: "en", allowTesting: true),
                post: { [unowned self] in posts.append($0) }, schedule: { [unowned self] delay, work in timers.append((delay, work)) })
            performer.localize = { key, source in "\(source) [\(key)]" }
            performer.tuneBubbleText = { "♪ hm ♪" }
            performer.onBubble = { [unowned self] in bubbles.append($0) }
            performer.onSpeechStarted = { [unowned self] in events.append("started") }
            performer.onSpeechFinished = { [unowned self] in events.append("finished") }
            return performer
        }

        private func runTimer() {
            let (_, work) = timers.removeFirst()
            work()
        }

        private func delivery(_ id: String, speak: Bool = true, chime: Bool = false, notify: Bool = false) -> Delivery {
            Delivery(
                phrase: PhraseBank.fixture[id]!, trigger: .buttonPress, plan: ChannelPlan(speak: speak, chime: chime, notify: notify))
        }

        private let setup = "Fixture joke, setup [phrase.joke-1.1]"
        private let punchline = "Fixture joke, punchline [phrase.joke-1.2]"

        func testAJokesPartsBubbleInOrderWithTheBeatBetween() {
            let speaker = FakeSpeaker()
            let performer = performer(speaker: speaker)
            performer.perform(delivery("joke-1", notify: true), settings: settings, voice: nil)
            XCTAssertEqual(bubbles, [setup])
            XCTAssertEqual(speaker.spoken, [setup])
            XCTAssertEqual(events, ["started"])
            speaker.finish()
            XCTAssertEqual(timers.map(\.0), [PhraseTiming.partBeat])
            XCTAssertTrue(performer.isSpeaking, "the beat keeps other lines out")
            XCTAssertFalse(performer.isVoicing, "the mouth is closed during the beat")
            XCTAssertTrue(posts.isEmpty, "the banner waits for the punchline")
            runTimer()
            XCTAssertEqual(bubbles, [setup, punchline])
            XCTAssertEqual(speaker.spoken, [setup, punchline])
            XCTAssertEqual(posts, ["\(setup)\n\(punchline)"], "one banner, both parts on two lines")
            speaker.finish()
            XCTAssertFalse(performer.isSpeaking)
            XCTAssertEqual(events, ["started", "finished"])
        }

        func testWithTheSilentSpeakerTheBeatStillRuns() {
            let performer = performer(speaker: SilentSpeaker())
            performer.perform(delivery("joke-1"), settings: settings, voice: nil)
            XCTAssertTrue(performer.isSpeaking)
            runTimer()
            XCTAssertFalse(performer.isSpeaking)
            XCTAssertEqual(bubbles, [setup, punchline])
        }

        func testStopMidBeatCancelsThePunchline() {
            let speaker = FakeSpeaker()
            let performer = performer(speaker: speaker)
            performer.perform(delivery("joke-1"), settings: settings, voice: nil)
            speaker.finish()
            performer.stop()
            XCTAssertFalse(performer.isSpeaking)
            runTimer()
            XCTAssertEqual(speaker.spoken, [setup])
            XCTAssertEqual(bubbles, [setup])
            XCTAssertEqual(events, ["started", "finished"])
        }

        func testWithoutSpeechThePartsFollowTheirBubbleTime() {
            let performer = performer(speaker: FakeSpeaker())
            performer.perform(delivery("joke-1", speak: false, notify: true), settings: settings, voice: nil)
            XCTAssertEqual(bubbles, [setup])
            XCTAssertEqual(timers.map(\.0), [DesktopPresence.bubbleSeconds(for: setup)])
            XCTAssertFalse(performer.isSpeaking)
            runTimer()
            XCTAssertEqual(bubbles, [setup, punchline])
            XCTAssertEqual(posts, ["\(setup)\n\(punchline)"])
            XCTAssertTrue(events.isEmpty)
        }

        func testALineNotifiesAtOnceAndTheBlipComesFirst() {
            let speaker = FakeSpeaker()
            let chime = FakeChime()
            let performer = performer(speaker: speaker, chime: chime)
            performer.blipSound = "blip"
            performer.perform(delivery("general-a1", chime: true, notify: true), settings: settings, voice: nil)
            let line = "Fixture line, general, flavor a, weight 1 [phrase.general-a1]"
            XCTAssertEqual(chime.played, ["blip"])
            XCTAssertEqual(speaker.spoken, [line])
            XCTAssertEqual(posts, [line])
            XCTAssertTrue(timers.isEmpty)
        }

        func testATunePlaysOnTheChimeWithItsBubble() {
            let speaker = FakeSpeaker()
            let chime = FakeChime()
            let performer = performer(speaker: speaker, chime: chime)
            performer.perform(delivery("tune-1", chime: true), settings: settings, voice: nil)
            XCTAssertEqual(chime.played, ["tune-1"])
            XCTAssertEqual(bubbles, ["♪ hm ♪"])
            XCTAssertTrue(speaker.spoken.isEmpty)
        }

        // MARK: Recorded voices

        private var folder: URL!

        override func tearDown() async throws {
            if let folder { try? FileManager.default.removeItem(at: folder) }
        }

        /// A voice folder for "en" where `male` recorded the given clips (texts as the performer localizes them).
        private func voices(_ clips: [String: String], fallback: Bool = false) throws -> VoiceLibrary {
            folder = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString, isDirectory: true)
            var entries: [String: Any] = [:]
            for (id, text) in clips {
                entries[id] = ["textHash": VoiceLibrary.hash(text), "duration": 1.0, "mouth": "0120"]
                let file = folder.appendingPathComponent("en/male/\(id).m4a")
                try FileManager.default.createDirectory(at: file.deletingLastPathComponent(), withIntermediateDirectories: true)
                try Data().write(to: file)
            }
            let manifest: [String: Any] = [
                "format": 1, "mouthFPS": 12.5,
                "languages": ["en": ["status": "shipping", "default": "male", "voices": ["male": ["clips": entries]]]],
            ]
            try JSONSerialization.data(withJSONObject: manifest).write(to: folder.appendingPathComponent("manifest.json"))
            return VoiceLibrary(folder: folder, language: "en", allowTesting: false, fallbackToSystemVoice: fallback)
        }

        func testEachPartPlaysItsOwnClip() throws {
            let speaker = FakeSpeaker()
            let performer = performer(speaker: speaker, voices: try voices(["joke-1.1": setup, "joke-1.2": punchline]))
            performer.perform(delivery("joke-1"), settings: settings, voice: "male")
            speaker.finish()
            runTimer()
            XCTAssertEqual(speaker.clips.map { $0?.lastPathComponent }, ["joke-1.1.m4a", "joke-1.2.m4a"])
            XCTAssertFalse(performer.unavailableIDs(voice: "male").contains("joke-1"))
        }

        func testAJokeMissingAPartStaysUnsaid() throws {
            let speaker = FakeSpeaker()
            let performer = performer(speaker: speaker, voices: try voices(["joke-1.1": setup]))
            XCTAssertTrue(performer.unavailableIDs(voice: "male").contains("joke-1"))
            XCTAssertFalse(performer.unavailableIDs(voice: "male").contains("tune-1"), "tunes are sounds")
            XCTAssertFalse(performer.unavailableIDs(voice: "male").contains("it-only"), "not in this language")
            XCTAssertEqual(performer.unavailableIDs(voice: nil), [])
            performer.perform(delivery("joke-1"), settings: settings, voice: "male")
            XCTAssertTrue(speaker.spoken.isEmpty)
            XCTAssertEqual(bubbles, [setup], "shown, not said")
        }

        func testTheFallbackSaysAMissingPartWithTheSystemVoice() throws {
            let speaker = FakeSpeaker()
            let performer = performer(speaker: speaker, voices: try voices(["joke-1.1": setup], fallback: true))
            XCTAssertEqual(performer.unavailableIDs(voice: "male"), [])
            performer.perform(delivery("joke-1"), settings: settings, voice: "male")
            speaker.finish()
            runTimer()
            XCTAssertEqual(speaker.clips.map { $0?.lastPathComponent }, ["joke-1.1.m4a", nil])
        }

        func testTheMouthFollowsTheRecordedPartsEnvelope() throws {
            let speaker = FakeSpeaker()
            let performer = performer(speaker: speaker, voices: try voices(["joke-1.1": setup], fallback: true))
            performer.perform(delivery("joke-1"), settings: settings, voice: "male")
            speaker.clipTime = 0.1  // the manifest's "0120" at 12.5 a second: frame 1
            XCTAssertEqual(performer.mouthLevel, 1)
            speaker.clipTime = 0.17
            XCTAssertEqual(performer.mouthLevel, 2)
            speaker.finish()
            runTimer()
            XCTAssertNil(performer.mouthLevel, "the system voice says the punchline: the mouth keeps its timer")
        }

        func testPreviewSpeaksWithoutChimeOrBanner() {
            let speaker = FakeSpeaker()
            let chime = FakeChime()
            let performer = performer(speaker: speaker, chime: chime)
            performer.blipSound = "blip"
            performer.preview(PhraseBank.fixture["general-b1"]!, settings: settings, voice: nil)
            XCTAssertEqual(speaker.spoken.count, 1)
            XCTAssertTrue(chime.played.isEmpty)
            XCTAssertTrue(posts.isEmpty)
        }
    }
#endif
