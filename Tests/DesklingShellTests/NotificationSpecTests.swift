#if os(macOS) || os(iOS)
    import DesklingShell
    import UserNotifications
    import XCTest

    final class NotificationSpecTests: XCTestCase {
        private let reminder = NotificationCategorySpec(
            id: "REMINDER",
            actions: [
                NotificationActionSpec(id: "START", title: "Start", options: [.foreground]),
                NotificationActionSpec(id: "SNOOZE", title: "Start in 10 min"),
                NotificationActionSpec(id: "SKIP", title: "Skip", options: [.destructive]),
            ],
            defaultActionID: "START")

        func testCategoryCarriesTheIdentifiersTitlesAndOptions() {
            let category = reminder.makeCategory()
            XCTAssertEqual(category.identifier, "REMINDER")
            XCTAssertEqual(category.actions.map(\.identifier), ["START", "SNOOZE", "SKIP"])
            XCTAssertEqual(category.actions.map(\.title), ["Start", "Start in 10 min", "Skip"])
            XCTAssertEqual(category.actions[0].options, [.foreground])
            XCTAssertEqual(category.actions[1].options, [])
            XCTAssertEqual(category.actions[2].options, [.destructive])
            XCTAssertTrue(category.intentIdentifiers.isEmpty)
        }

        func testActionMapsAlone() {
            let action = NotificationActionSpec(id: "A", title: "Go").makeAction()
            XCTAssertEqual(action.identifier, "A")
            XCTAssertEqual(action.title, "Go")
            XCTAssertEqual(action.options, [])
        }

        @MainActor
        func testPosterKeepsItsCategoriesAndDefaults() {
            let poster = NotificationPoster(categories: [reminder])
            XCTAssertEqual(poster.categories.map(\.id), ["REMINDER"])
            XCTAssertEqual(poster.categories[0].defaultActionID, "START")
            XCTAssertEqual(poster.presentationOptions, [.banner, .sound])
            XCTAssertNil(poster.onAction)
            XCTAssertNil(poster.onAnswer)
            XCTAssertNil(poster.onPresent)
        }

        func testScheduledNotificationTriggersOnTheLocalWallClockTime() throws {
            var calendar = Calendar(identifier: .gregorian)
            calendar.timeZone = try XCTUnwrap(TimeZone(identifier: "Europe/Rome"))
            let date = try XCTUnwrap(
                calendar.date(from: DateComponents(year: 2026, month: 9, day: 28, hour: 9, minute: 45, second: 30)))
            let notification = ScheduledNotification(
                id: "reminder-1", date: date, title: "Time to move", body: "Up next: Squats",
                categoryID: "REMINDER", sound: true, userInfo: ["exercises": "squat,lunge"])
            let request = notification.makeRequest(calendar: calendar)
            XCTAssertEqual(request.identifier, "reminder-1")
            XCTAssertEqual(request.content.title, "Time to move")
            XCTAssertEqual(request.content.body, "Up next: Squats")
            XCTAssertEqual(request.content.categoryIdentifier, "REMINDER")
            XCTAssertNotNil(request.content.sound)
            XCTAssertEqual(request.content.userInfo["exercises"] as? String, "squat,lunge")
            let trigger = try XCTUnwrap(request.trigger as? UNCalendarNotificationTrigger)
            XCTAssertFalse(trigger.repeats)
            let c = trigger.dateComponents
            XCTAssertEqual([c.year, c.month, c.day, c.hour, c.minute, c.second], [2026, 9, 28, 9, 45, 30])
            XCTAssertNil(c.timeZone)
        }

        func testSilentScheduledNotificationHasNoSound() {
            let notification = ScheduledNotification(
                id: "a", date: Date(timeIntervalSinceReferenceDate: 0), title: "T", body: "B", categoryID: "C",
                sound: false)
            XCTAssertNil(notification.makeRequest().content.sound)
            XCTAssertTrue(notification.makeRequest().content.userInfo.isEmpty)
        }
    }
#endif
