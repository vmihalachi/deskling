import DesklingCompanion
import DesklingCore
import XCTest

final class CompanionStateTests: XCTestCase {
    let scheduled = SchedulerStatus.scheduled(Date(timeIntervalSince1970: 0))

    func testPriorities() {
        XCTAssertEqual(CompanionState.mood(status: scheduled, snapshot: .calm), .awake)
        XCTAssertEqual(CompanionState.mood(status: scheduled, snapshot: Snapshot(isSpeaking: true)), .talking)
        XCTAssertEqual(CompanionState.mood(status: .paused(until: Date()), snapshot: .calm), .shushed)
        XCTAssertEqual(CompanionState.mood(status: .paused(until: Date()), snapshot: Snapshot(isSpeaking: true)), .talking)
        XCTAssertEqual(CompanionState.mood(status: scheduled, snapshot: Snapshot(busy: [.call, .fullScreen])), .hushedForCall)
        XCTAssertEqual(CompanionState.mood(status: scheduled, snapshot: Snapshot(busy: [.fullScreen])), .hushedFullScreen)
        XCTAssertEqual(CompanionState.mood(status: scheduled, snapshot: Snapshot(idleSeconds: 900)), .asleep)
        XCTAssertEqual(CompanionState.mood(status: scheduled, snapshot: Snapshot(isScreenLocked: true)), .asleep)
        XCTAssertEqual(CompanionState.mood(status: .away, snapshot: .calm), .asleep)
        XCTAssertEqual(CompanionState.mood(status: .postponed(.call), snapshot: .calm), .hushedForCall)
        XCTAssertEqual(CompanionState.mood(status: .outsideActiveHours, snapshot: .calm), .offHours)
    }
}
