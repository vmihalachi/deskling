import DesklingCompanion
import XCTest

/// The pause before a sequence's next part: a beat after a spoken part, the bubble's time after a shown one.
final class PhraseTimingTests: XCTestCase {
    func testTheFirstPartHasNoPreviousOneSoItWaitsTheFloorOrTheBeat() {
        XCTAssertEqual(PhraseTiming.partDelay(spoken: false, previousPart: ""), 2)
        XCTAssertEqual(PhraseTiming.partDelay(spoken: true, previousPart: ""), PhraseTiming.partBeat)
    }

    func testASpokenPartWaitsTheBeat() {
        XCTAssertEqual(PhraseTiming.partBeat, 1.2)
        XCTAssertEqual(PhraseTiming.partDelay(spoken: true, previousPart: String(repeating: "a", count: 100)), 1.2)
    }

    func testAShownPartStaysAsLongAsItsBubble() {
        let setup = String(repeating: "a", count: 50)
        XCTAssertEqual(PhraseTiming.partDelay(spoken: false, previousPart: setup), DesktopPresence.bubbleSeconds(for: setup))
        XCTAssertEqual(PhraseTiming.partDelay(spoken: false, previousPart: setup), 2.5)
    }

    func testAShownPartFollowedByASpokenOneWaitsTheBubbleThenTheBeat() {
        let long = String(repeating: "a", count: 113)
        XCTAssertEqual(PhraseTiming.partDelay(spoken: false, previousPart: long), 5)
        XCTAssertEqual(PhraseTiming.partDelay(spoken: true, previousPart: long), PhraseTiming.partBeat)
    }

    func testAZeroLengthBubbleStillHoldsTheFloor() {
        XCTAssertEqual(DesktopPresence.bubbleSeconds(for: ""), 2)
        XCTAssertEqual(PhraseTiming.partDelay(spoken: false, previousPart: ""), DesktopPresence.bubbleSeconds(for: ""))
    }
}
