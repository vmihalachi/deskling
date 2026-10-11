import DesklingCompanion
import DesklingCore
import DesklingTesting
import XCTest

final class ChatterCadenceTests: XCTestCase {
    func testPresets() {
        XCTAssertEqual(Frequency.rarely.interval, 3600)
        XCTAssertEqual(Frequency.rarely.jitter, 900)
        XCTAssertEqual(Frequency.sometimes.interval, 1800)
        XCTAssertEqual(Frequency.sometimes.jitter, 600)
        XCTAssertEqual(Frequency.chatty.interval, 900)
        XCTAssertEqual(Frequency.chatty.jitter, 300)
    }

    func testConfigMapsSettingsOntoTheScheduler() {
        let config = ChatterCadence.config(
            frequency: .chatty, activeStartMinute: 8 * 60, activeEndMinute: 20 * 60, activeWeekdays: [1, 7],
            quietHours: [13], hushInFullScreen: false)
        XCTAssertEqual(config.interval, 900)
        XCTAssertEqual(config.intervalJitter, 300)
        XCTAssertEqual(config.activeStartMinute, 480)
        XCTAssertEqual(config.activeEndMinute, 1200)
        XCTAssertEqual(config.activeWeekdays, [1, 7])
        XCTAssertEqual(config.quietHours, [13])
        XCTAssertEqual(config.idleThreshold, 300)
        XCTAssertTrue(config.postponeDuringCalls)
        XCTAssertFalse(config.postponeDuringFullScreen)
        XCTAssertEqual(config.busyGrace, 30)
    }

    func testDefaultSettingsGiveTheDefaultConfig() {
        let settings = CompanionSettings(store: InMemorySettingsStore(), defaults: .fixture)
        let config = ChatterCadence.config(for: settings)
        XCTAssertEqual(config.interval, 1800)
        XCTAssertEqual(config.intervalJitter, 600)
        XCTAssertEqual(config.activeWeekdays, [2, 3, 4, 5, 6])
        XCTAssertTrue(config.postponeDuringFullScreen)
    }
}
