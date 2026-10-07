#if os(macOS)
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
        }
    }
#endif
