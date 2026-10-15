import DesklingCore
import DesklingTesting
import XCTest

/// Each mock's contract: a clock moved by hand, settable idle and busy values, a scripted and a counting random source.
final class MocksTests: XCTestCase {
    func testMockClockAdvancesByMinutes() {
        let clock = MockClock(Date(timeIntervalSince1970: 0))
        clock.advance(minutes: 1.5)
        XCTAssertEqual(clock.now, Date(timeIntervalSince1970: 90))
        clock.advance(minutes: -1)
        XCTAssertEqual(clock.now, Date(timeIntervalSince1970: 30))
    }

    func testMockIdleAnswersItsValue() {
        let idle = MockIdle()
        XCTAssertEqual(idle.secondsSinceLastInput(), 0)
        idle.idle = 42
        XCTAssertEqual(idle.secondsSinceLastInput(), 42)
    }

    func testMockBusyAnswersItsReasons() {
        let busy = MockBusy()
        XCTAssertEqual(busy.currentBusyReasons(), [])
        busy.reasons = [.call, .fullScreen]
        XCTAssertEqual(busy.currentBusyReasons(), [.call, .fullScreen])
    }

    func testScriptedRandomPlaysItsValuesThenRepeatsTheLast() {
        var random = ScriptedRandom([0.1, 0.2, 0.3])
        XCTAssertEqual([random.nextUnit(), random.nextUnit(), random.nextUnit()], [0.1, 0.2, 0.3])
        XCTAssertEqual(random.nextUnit(), 0.3)
        XCTAssertEqual(random.nextUnit(), 0.3)
        XCTAssertEqual(random.reads, 5)
    }

    func testCountingRandomCountsAndReportsEveryRead() {
        var reported = 0
        let random = CountingRandom(onRead: { reported += 1 })
        XCTAssertEqual(random.reads, 0)
        XCTAssertEqual(random.nextUnit(), 0.5)
        XCTAssertEqual(random.nextUnit(), 0.5)
        XCTAssertEqual(random.reads, 2)
        XCTAssertEqual(reported, 2)
    }

    func testCountingRandomCountsWithoutAListener() {
        let random = CountingRandom()
        _ = random.nextUnit()
        XCTAssertEqual(random.reads, 1)
    }
}
