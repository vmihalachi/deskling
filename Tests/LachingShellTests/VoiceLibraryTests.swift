#if os(macOS)
    import LachingShell
    import Foundation
    import XCTest

    /// Over `Resources/Voice`: Italian shipping with male (default) and female, English testing.
    @MainActor
    final class VoiceLibraryTests: XCTestCase {
        /// Found by name, as the Companion tests find theirs: the bundle's layout differs between toolchains.
        private var folder: URL {
            Bundle.module.url(forResource: "Resources/Voice", withExtension: nil)!
        }

        private func library(_ language: String = "it", allowTesting: Bool = false, fallback: Bool = false) -> VoiceLibrary {
            VoiceLibrary(folder: folder, language: language, allowTesting: allowTesting, fallbackToSystemVoice: fallback)
        }

        func testVoicesTheDefaultFirst() {
            let voices = library()
            XCTAssertTrue(voices.isVoiced)
            XCTAssertEqual(voices.availableVoices, ["male", "female"])
            XCTAssertEqual(voices.voice(preferred: "female"), "female")
            XCTAssertEqual(voices.voice(preferred: "robot"), "male", "a voice the language lacks falls back to its default")
            XCTAssertEqual(voices.voice(preferred: nil), "male")
            XCTAssertEqual(voices.mouthFPS, 12.5)
        }

        func testOnlyAClipOfTheCurrentTextCounts() {
            let voices = library()
            XCTAssertNotNil(voices.clip(for: "hello", text: "Ciao.", voice: "male"))
            XCTAssertNil(voices.clip(for: "hello", text: "Ciao!", voice: "male"), "the bubble and the audio must agree")
            XCTAssertNil(voices.clip(for: "stale", text: "Nuovo testo.", voice: "male"), "recorded from older text")
            XCTAssertNil(voices.clip(for: "nofile", text: "Senza file.", voice: "male"), "the file is missing")
            XCTAssertEqual(voices.mouth(for: "hello", voice: "male"), [0, 1, 2, 0])
            XCTAssertEqual(voices.mouth(for: "hello", voice: "female"), [0, 1, 1, 0])
        }

        func testAJokeHasAClipPerPart() {
            let voices = library()
            let parts = [(id: "joke.1", text: "Perché il pomodoro arrossisce?"), (id: "joke.2", text: "Ha visto l'insalata.")]
            XCTAssertEqual(voices.clip(for: "joke.1", text: parts[0].text, voice: "male")?.lastPathComponent, "joke.1.m4a")
            XCTAssertEqual(voices.clip(for: "joke.2", text: parts[1].text, voice: "male")?.lastPathComponent, "joke.2.m4a")
            XCTAssertEqual(voices.pendingClipIDs(parts, voice: "male"), [])
            XCTAssertEqual(voices.pendingClipIDs(parts, voice: "female"), ["joke.1", "joke.2"])
            XCTAssertEqual(voices.mouth(for: "joke.1", voice: "male"), [0, 2, 3, 1])
        }

        func testPendingClips() {
            let clips = [
                (id: "hello", text: "Ciao."), (id: "stale", text: "Nuovo testo."), (id: "nofile", text: "Senza file."),
                (id: "never", text: "Mai registrata."),
            ]
            XCTAssertEqual(library().pendingClipIDs(clips, voice: "male"), ["stale", "nofile", "never"])
            XCTAssertEqual(library(fallback: true).pendingClipIDs(clips, voice: "male"), [], "the system voice says them")
            XCTAssertNil(library(fallback: true).clip(for: "stale", text: "Nuovo testo.", voice: "male"))
        }

        func testATestingLanguageStaysOffInRelease() {
            XCTAssertFalse(library("en").isVoiced)
            XCTAssertTrue(library("en", allowTesting: true).isVoiced)
        }

        func testALanguageWithoutARecordedVoiceKeepsTheSystemVoice() {
            let voices = library("de", allowTesting: true)
            XCTAssertFalse(voices.isVoiced)
            XCTAssertNil(voices.voice(preferred: "male"))
            XCTAssertFalse(VoiceLibrary(folder: nil, language: "it", allowTesting: true).isVoiced)
        }

        func testTheHashIsSHA256Hex() {
            XCTAssertEqual(VoiceLibrary.hash(""), "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")
        }

        func testAClipThatWontPlayEndsTheLine() throws {
            let broken = FileManager.default.temporaryDirectory.appendingPathComponent("\(UUID().uuidString).m4a")
            try Data("not audio".utf8).write(to: broken)
            defer { try? FileManager.default.removeItem(at: broken) }
            let speaker = Speaker(language: "it")
            var finished = 0
            speaker.onFinished = { finished += 1 }
            speaker.speak("Ciao.", clip: broken, voiceIdentifier: nil, rate: 0.5, volume: 0)
            XCTAssertEqual(finished, 1)
            XCTAssertFalse(speaker.isSpeaking)
        }

        func testClipRateKeepsTheVoiceNatural() {
            XCTAssertEqual(Speaker.clipRate(from: 0.5), 1)
            XCTAssertEqual(Speaker.clipRate(from: 0), 0.85, accuracy: 0.0001)
            XCTAssertEqual(Speaker.clipRate(from: 1), 1.2, accuracy: 0.0001)
        }

        func testAMissingSoundCompletesAtOnce() {
            let chime = ChimePlayer(bundle: .module)
            var done = false
            chime.play("no-such-sound", volume: 1) { done = true }
            XCTAssertTrue(done)
        }
    }
#endif
