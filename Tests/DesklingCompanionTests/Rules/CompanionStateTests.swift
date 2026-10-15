import DesklingCompanion
import DesklingCore
import XCTest

/// One test per `Mood`, naming the status and snapshot that produce it, then the order the moods win in.
final class CompanionStateTests: XCTestCase {
    let scheduled = SchedulerStatus.scheduled(Date(timeIntervalSince1970: 0))
    let paused = SchedulerStatus.paused(until: Date(timeIntervalSince1970: 600))

    private func mood(_ status: SchedulerStatus, _ snapshot: Snapshot = .calm) -> Mood {
        CompanionState.mood(status: status, snapshot: snapshot)
    }

    func testAwakeIsScheduledWithACalmSnapshot() {
        XCTAssertEqual(mood(scheduled), .awake)
    }

    func testTalkingIsSpeaking() {
        XCTAssertEqual(mood(scheduled, Snapshot(isSpeaking: true)), .talking)
    }

    func testShushedIsPaused() {
        XCTAssertEqual(mood(paused), .shushed)
    }

    func testAsleepIsALockedScreenIdleAtTheThresholdOrAway() {
        XCTAssertEqual(mood(scheduled, Snapshot(isScreenLocked: true)), .asleep)
        XCTAssertEqual(mood(scheduled, Snapshot(idleSeconds: QuietRules.awayThreshold)), .asleep)
        XCTAssertEqual(mood(scheduled, Snapshot(idleSeconds: QuietRules.awayThreshold - 1)), .awake)
        XCTAssertEqual(mood(.away), .asleep)
    }

    func testHushedForCallIsABusyCallOrAPostponedOne() {
        XCTAssertEqual(mood(scheduled, Snapshot(busy: [.call])), .hushedForCall)
        XCTAssertEqual(mood(.postponed(.call)), .hushedForCall)
    }

    func testHushedFullScreenIsABusyFullScreenOrAPostponedOne() {
        XCTAssertEqual(mood(scheduled, Snapshot(busy: [.fullScreen])), .hushedFullScreen)
        XCTAssertEqual(mood(.postponed(.fullScreen)), .hushedFullScreen)
    }

    func testOffHoursIsOutsideActiveHours() {
        XCTAssertEqual(mood(.outsideActiveHours), .offHours)
    }

    /// A new mood needs a test above.
    func testTheMoodsAreTheSevenAbove() {
        XCTAssertEqual(Mood.allCases, [.awake, .talking, .shushed, .asleep, .hushedForCall, .hushedFullScreen, .offHours])
    }

    // Precedence.

    func testTalkingWinsOverEverything() {
        let everything = Snapshot(
            busy: [.call, .fullScreen], idleSeconds: QuietRules.awayThreshold, isScreenLocked: true, isSpeaking: true)
        XCTAssertEqual(mood(paused, everything), .talking)
        XCTAssertEqual(mood(.outsideActiveHours, everything), .talking)
    }

    func testPausedWinsOverACall() {
        XCTAssertEqual(mood(paused, Snapshot(busy: [.call])), .shushed)
    }

    func testACallWinsOverFullScreen() {
        XCTAssertEqual(mood(scheduled, Snapshot(busy: [.call, .fullScreen])), .hushedForCall)
    }

    func testALockedScreenWinsOverAPostponedStatus() {
        XCTAssertEqual(mood(.postponed(.call), Snapshot(isScreenLocked: true)), .asleep)
        XCTAssertEqual(mood(.postponed(.fullScreen), Snapshot(idleSeconds: QuietRules.awayThreshold)), .asleep)
    }

    func testOffHoursComesLastBeforeAwake() {
        XCTAssertEqual(mood(.outsideActiveHours, Snapshot(busy: [.fullScreen])), .hushedFullScreen)
        XCTAssertEqual(mood(.outsideActiveHours, Snapshot(isScreenLocked: true)), .asleep)
        XCTAssertEqual(mood(.outsideActiveHours), .offHours)
    }
}
