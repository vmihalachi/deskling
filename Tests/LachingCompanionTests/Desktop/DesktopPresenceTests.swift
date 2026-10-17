import LachingCompanion
import LachingCore
import XCTest

final class DesktopPresenceTests: XCTestCase {
    private let start = Date(timeIntervalSince1970: 1_800_000_000)
    private let call = Snapshot(busy: [.call])
    private let fullScreen = Snapshot(busy: [.fullScreen])

    private func at(_ seconds: TimeInterval) -> Date { start.addingTimeInterval(seconds) }

    func testShownWhenEnabledAndCalm() {
        var presence = DesktopPresence()
        XCTAssertTrue(presence.update(enabled: true, snapshot: .calm, now: at(0)))
        XCTAssertFalse(presence.update(enabled: false, snapshot: .calm, now: at(1)))
        XCTAssertTrue(presence.update(enabled: true, snapshot: .calm, now: at(2)))
    }

    func testACallHidesTheCharacterUntilTenSecondsAfterItEnds() {
        var presence = DesktopPresence()
        XCTAssertTrue(presence.update(enabled: true, snapshot: .calm, now: at(0)))
        XCTAssertFalse(presence.update(enabled: true, snapshot: call, now: at(20)))
        XCTAssertFalse(presence.update(enabled: true, snapshot: call, now: at(25)))
        XCTAssertFalse(presence.update(enabled: true, snapshot: .calm, now: at(26)))
        XCTAssertFalse(presence.update(enabled: true, snapshot: .calm, now: at(34.9)))
        XCTAssertTrue(presence.update(enabled: true, snapshot: .calm, now: at(35)))
    }

    func testFullScreenHidesTheCharacterToo() {
        var presence = DesktopPresence()
        XCTAssertFalse(presence.update(enabled: true, snapshot: fullScreen, now: at(0)))
        XCTAssertTrue(presence.update(enabled: true, snapshot: .calm, now: at(10)))
    }

    func testABusyMomentWhileDisabledStillCounts() {
        var presence = DesktopPresence()
        XCTAssertFalse(presence.update(enabled: false, snapshot: call, now: at(0)))
        XCTAssertFalse(presence.update(enabled: true, snapshot: .calm, now: at(5)))
        XCTAssertTrue(presence.update(enabled: true, snapshot: .calm, now: at(10)))
    }

    func testHideForAWhileRunsOutByItself() {
        var presence = DesktopPresence()
        presence.hide(until: at(3600))
        XCTAssertFalse(presence.update(enabled: true, snapshot: .calm, now: at(0)))
        XCTAssertFalse(presence.update(enabled: true, snapshot: .calm, now: at(3599)))
        XCTAssertTrue(presence.update(enabled: true, snapshot: .calm, now: at(3600)))
        XCTAssertNil(presence.hiddenUntil)
    }

    func testShowNowEndsAHideEarly() {
        var presence = DesktopPresence()
        presence.hide(until: at(3600))
        XCTAssertFalse(presence.update(enabled: true, snapshot: .calm, now: at(0)))
        presence.showNow()
        XCTAssertTrue(presence.update(enabled: true, snapshot: .calm, now: at(60)))
    }

    func testACallAtTheEndOfAHideStillHoldsTheCharacterBack() {
        var presence = DesktopPresence()
        presence.hide(until: at(60))
        XCTAssertFalse(presence.update(enabled: true, snapshot: call, now: at(55)))
        XCTAssertFalse(presence.update(enabled: true, snapshot: .calm, now: at(60)))
        XCTAssertTrue(presence.update(enabled: true, snapshot: .calm, now: at(65)))
    }

    func testBubbleSecondsGrowWithTheLineBetweenTwoAndFive() {
        XCTAssertEqual(DesktopPresence.bubbleSeconds(for: ""), 2)
        XCTAssertEqual(DesktopPresence.bubbleSeconds(for: String(repeating: "a", count: 50)), 2.5, accuracy: 1e-9)
        XCTAssertEqual(DesktopPresence.bubbleSeconds(for: String(repeating: "a", count: 500)), 5)
        // Scalars, not grapheme clusters: the same count as Windows' runes.
        XCTAssertEqual(DesktopPresence.bubbleSeconds(for: String(repeating: "é", count: 100)), 4.5, accuracy: 1e-9)
    }

    func testSizesStepAndStopAtTheEnds() {
        XCTAssertEqual(DesktopSize.medium.stepped(by: 1), .large)
        XCTAssertEqual(DesktopSize.large.stepped(by: 1), .large)
        XCTAssertEqual(DesktopSize.medium.stepped(by: -1), .small)
        XCTAssertEqual(DesktopSize.small.stepped(by: -1), .small)
        XCTAssertEqual(DesktopSize.allCases.map(\.height), [64, 96, 140])
    }
}
