#if os(macOS)
    import DesklingShell
    import Foundation
    import XCTest

    /// A voice that speaks nothing: it records each line and finishes when the test says so.
    @MainActor
    private final class ScriptedSpeaker: Speaking {
        private(set) var spoken: [String] = []
        private(set) var stops = 0
        var isSpeaking = false
        var onFinished: (() -> Void)?

        func speak(_ text: String, clip: URL?, voiceIdentifier: String?, rate: Double, volume: Double) {
            spoken.append(text)
            isSpeaking = true
        }

        func stop() {
            stops += 1
            guard isSpeaking else { return }
            isSpeaking = false
            onFinished?()
        }

        /// The line ends by itself.
        func finish() {
            isSpeaking = false
            onFinished?()
        }
    }

    @MainActor
    final class SpeechSequencerTests: XCTestCase {
        private var speaker: ScriptedSpeaker!
        private var sequencer: SpeechSequencer!
        /// Beats waiting to run, with their delays.
        private var beats: [(TimeInterval, @MainActor @Sendable () -> Void)] = []
        private var events: [String] = []

        override func setUp() async throws {
            speaker = ScriptedSpeaker()
            beats = []
            events = []
            sequencer = SpeechSequencer(speaker: speaker) { [unowned self] delay, work in beats.append((delay, work)) }
            sequencer.onPartStarted = { [unowned self] in events.append("part \($0)") }
            sequencer.onFinished = { [unowned self] in events.append("finished") }
        }

        private func runBeat() {
            let (_, work) = beats.removeFirst()
            work()
        }

        private let joke = [SpeechPart(text: "Setup"), SpeechPart(text: "Punchline")]

        func testPartsPlayInOrderWithTheBeatBetween() {
            sequencer.speak(joke, beat: 1.2, voiceIdentifier: nil, rate: 0.5, volume: 1)
            XCTAssertEqual(speaker.spoken, ["Setup"])
            XCTAssertEqual(events, ["part 0"])
            speaker.finish()
            XCTAssertEqual(beats.map(\.0), [1.2])
            XCTAssertEqual(speaker.spoken, ["Setup"], "the punchline waits for the beat")
            runBeat()
            XCTAssertEqual(speaker.spoken, ["Setup", "Punchline"])
            speaker.finish()
            XCTAssertEqual(events, ["part 0", "part 1", "finished"])
            XCTAssertFalse(sequencer.isActive)
            XCTAssertTrue(beats.isEmpty)
        }

        func testActiveThroughTheBeatButNotVoicing() {
            sequencer.speak(joke, beat: 1.2, voiceIdentifier: nil, rate: 0.5, volume: 1)
            XCTAssertTrue(sequencer.isActive)
            XCTAssertTrue(sequencer.isVoicing)
            speaker.finish()
            XCTAssertTrue(sequencer.isActive, "nothing else may start in the pause")
            XCTAssertFalse(sequencer.isVoicing, "the mouth closes during the beat")
            runBeat()
            XCTAssertTrue(sequencer.isVoicing)
        }

        func testVoicingIsReportedAsAudioStartsAndStops() {
            var voicing: [Bool] = []
            sequencer.onVoicingChanged = { voicing.append($0) }
            sequencer.speak(joke, beat: 1.2, voiceIdentifier: nil, rate: 0.5, volume: 1)
            XCTAssertEqual(voicing, [true])
            speaker.finish()
            XCTAssertEqual(voicing, [true, false], "the beat is silent")
            runBeat()
            speaker.finish()
            XCTAssertEqual(voicing, [true, false, true, false])
            sequencer.speak(joke, beat: 1.2, voiceIdentifier: nil, rate: 0.5, volume: 1)
            sequencer.stop()
            XCTAssertEqual(voicing, [true, false, true, false, true, false], "a stop ends the audio")
            sequencer.stop()
            XCTAssertEqual(voicing.count, 6, "reported once")
        }

        func testStopCancelsThePendingPart() {
            sequencer.speak(joke, beat: 1.2, voiceIdentifier: nil, rate: 0.5, volume: 1)
            speaker.finish()
            sequencer.stop()
            XCTAssertFalse(sequencer.isActive)
            XCTAssertEqual(events, ["part 0", "finished"])
            runBeat()
            XCTAssertEqual(speaker.spoken, ["Setup"], "a cancelled beat says nothing")
            XCTAssertEqual(events, ["part 0", "finished"])
        }

        func testStopCutsThePartBeingSpoken() {
            sequencer.speak(joke, beat: 1.2, voiceIdentifier: nil, rate: 0.5, volume: 1)
            sequencer.stop()
            XCTAssertFalse(speaker.isSpeaking)
            XCTAssertEqual(speaker.stops, 1)
            XCTAssertEqual(events, ["part 0", "finished"], "finished once, not again from the voice")
            XCTAssertTrue(beats.isEmpty)
        }

        func testALineIsOnePartWithoutABeat() {
            sequencer.speak([SpeechPart(text: "Hello")], beat: 1.2, voiceIdentifier: nil, rate: 0.5, volume: 1)
            speaker.finish()
            XCTAssertEqual(events, ["part 0", "finished"])
            XCTAssertTrue(beats.isEmpty)
        }

        func testANewLineReplacesTheOldOne() {
            sequencer.speak(joke, beat: 1.2, voiceIdentifier: nil, rate: 0.5, volume: 1)
            speaker.finish()
            sequencer.speak([SpeechPart(text: "Hello")], beat: 1.2, voiceIdentifier: nil, rate: 0.5, volume: 1)
            runBeat()  // the old joke's beat
            XCTAssertEqual(speaker.spoken, ["Setup", "Hello"])
            XCTAssertEqual(events, ["part 0", "finished", "part 0"])
        }
    }
#endif
