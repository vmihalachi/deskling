import DesklingCompanion
import DesklingTesting
import XCTest

/// The standard lists are what every companion app's vectors are named after: renaming or reordering one changes the
/// apps' committed files.
final class StandardCasesTests: XCTestCase {
    func testTheStandardQuietTimelines() {
        var base = QuietTimeline.Settings(.fixture)
        base.frequency = "sometimes"
        let cases = QuietTimeline.standardCases(base: base, dst: ("evening-rome-dst-end", "Europe/Rome"))
        XCTAssertEqual(
            cases.map(\.name),
            [
                "ambient-monday-morning", "chatty-with-chime", "shush-hour-then-wake-line", "shush-restored-from-settings",
                "shush-expired-in-settings", "call-holds-then-after-call", "full-screen-hushed", "full-screen-ignored-by-setting",
                "away-welcome-back-cooldown", "muted-falls-back-to-notification", "notification-channel", "quietly", "comments-off",
                "off-hours", "weekend-off", "frequency-change-reschedules", "evening-rome-dst-end",
            ])
        XCTAssertEqual(cases.last?.timeZone, "Europe/Rome")
        XCTAssertTrue(cases.allSatisfy { $0.settings.frequency == "sometimes" })
    }

    func testTheStandardPickerCases() {
        let cases = PickerVector.standardCases(
            bank: .fixture, skin: FixtureIDs.vectorSkin, single: ("one-flavor", [FixtureIDs.flavorC]),
            pair: ("two-flavors", [FixtureIDs.flavorA, FixtureIDs.flavorB]), leftEmpty: FixtureIDs.flavorA, excluded: 2)
        XCTAssertEqual(
            cases.map(\.name),
            [
                "wednesday-afternoon", "friday-evening", "saturday-night", "sunday-dawn", "recent-skipped", "event-welcome-back",
                "event-after-call", "event-battery-low", "event-went-dark", "one-flavor", "two-flavors",
                "event-flavor-left-empty-falls-back", "excluded-ids", "event-all-excluded-falls-back", "event-recent-exhausted",
            ])
        XCTAssertEqual(cases.first { $0.name == "excluded-ids" }?.context.excludedIDs.count, 2)
        XCTAssertTrue(cases.allSatisfy { $0.context.skin == FixtureIDs.vectorSkin && $0.context.language == "en" })
    }
}
