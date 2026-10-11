import DesklingCompanion
import XCTest

final class PhraseTimingTests: XCTestCase {
    func testASpokenPartWaitsTheBeat() {
        XCTAssertEqual(PhraseTiming.partBeat, 1.2)
        XCTAssertEqual(PhraseTiming.partDelay(spoken: true, previousPart: String(repeating: "a", count: 100)), 1.2)
    }

    func testAShownPartStaysAsLongAsItsBubble() {
        let setup = String(repeating: "a", count: 50)
        XCTAssertEqual(PhraseTiming.partDelay(spoken: false, previousPart: setup), DesktopPresence.bubbleSeconds(for: setup))
        XCTAssertEqual(PhraseTiming.partDelay(spoken: false, previousPart: ""), 2)
    }
}
