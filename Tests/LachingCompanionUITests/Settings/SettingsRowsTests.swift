#if os(macOS)
    import LachingCompanion
    import LachingCompanionUI
    import LachingCore
    import LachingTesting
    import SwiftUI
    import XCTest

    @MainActor
    final class SettingsRowsTests: XCTestCase {
        private let settings = CompanionSettings(store: InMemorySettingsStore(), defaults: .fixture)

        func testATimePickerRoundTripsTheMinuteOfTheDay() {
            var calendar = Calendar(identifier: .gregorian)
            calendar.timeZone = TimeZone(identifier: "Europe/Rome")!
            let start = ActiveHoursEditor<DefaultToggleStyle>.minuteBinding(settings, \.activeStartMinute, calendar: calendar)
            settings.activeStartMinute = 9 * 60 + 30
            XCTAssertEqual(calendar.dateComponents([.hour, .minute], from: start.wrappedValue), DateComponents(hour: 9, minute: 30))
            start.wrappedValue = calendar.date(bySettingHour: 17, minute: 45, second: 0, of: Date())!
            XCTAssertEqual(settings.activeStartMinute, 17 * 60 + 45)
        }

        func testTheWeekdaysStartWhereTheCalendarStartsItsWeek() {
            XCTAssertEqual(ActiveHoursEditor<DefaultToggleStyle>.orderedWeekdays(firstWeekday: 1), [1, 2, 3, 4, 5, 6, 7])
            XCTAssertEqual(ActiveHoursEditor<DefaultToggleStyle>.orderedWeekdays(firstWeekday: 2), [2, 3, 4, 5, 6, 7, 1])
            XCTAssertEqual(ActiveHoursEditor<DefaultToggleStyle>.orderedWeekdays(firstWeekday: 7), [7, 1, 2, 3, 4, 5, 6])
        }

        func testQuietHoursAreLabelledTheWayTheLocaleWritesHours() {
            var calendar = Calendar(identifier: .gregorian)
            calendar.timeZone = TimeZone(identifier: "UTC")!
            let american = QuietHoursEditor<DefaultToggleStyle>.label(15, locale: Locale(identifier: "en_US"), calendar: calendar)
            let german = QuietHoursEditor<DefaultToggleStyle>.label(15, locale: Locale(identifier: "de_DE"), calendar: calendar)
            XCTAssertTrue(american.contains("3") && american.contains("PM"), american)
            XCTAssertTrue(german.contains("15") && !german.contains("PM"), german)
        }

        func testChannelAndFlavorBindingsWriteThrough() {
            var turned: [(Channels, Bool)] = []
            let chime = settings.binding(for: .chime) { channel, on in turned.append((channel, on)) }
            XCTAssertFalse(chime.wrappedValue)
            chime.wrappedValue = true
            XCTAssertEqual(turned.map(\.0), [.chime])
            XCTAssertEqual(turned.map(\.1), [true])

            let flavor = settings.binding(for: FixtureIDs.flavorB)
            XCTAssertTrue(flavor.wrappedValue)
            flavor.wrappedValue = false
            XCTAssertEqual(settings.phraseFlavors, [FixtureIDs.flavorA, FixtureIDs.flavorC])
            flavor.wrappedValue = true
            XCTAssertEqual(settings.phraseFlavors, FixtureIDs.flavors)
        }

        func testMutedCountIgnoresRetiredLines() {
            settings.mutedPhraseIDs = ["general-a1", "morning-1", "retired-line"]
            XCTAssertEqual(settings.mutedCount(in: .fixture), 2)
        }
    }
#endif
