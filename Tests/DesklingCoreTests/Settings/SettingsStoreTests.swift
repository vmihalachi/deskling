import DesklingCore
import Foundation
import XCTest

final class SettingsStoreTests: XCTestCase {
    private func roundTrip(_ store: SettingsStore) {
        XCTAssertNil(store.bool(forKey: "b"))
        XCTAssertNil(store.integer(forKey: "i"))
        XCTAssertNil(store.double(forKey: "d"))
        XCTAssertNil(store.string(forKey: "s"))
        XCTAssertNil(store.strings(forKey: "ss"))
        XCTAssertNil(store.integers(forKey: "is"))

        store.set(bool: false, forKey: "b")
        store.set(integer: 0, forKey: "i")
        store.set(double: 0.25, forKey: "d")
        store.set(string: "", forKey: "s")
        store.set(strings: ["a", "b"], forKey: "ss")
        store.set(integers: [3, 1], forKey: "is")
        XCTAssertEqual(store.bool(forKey: "b"), false)
        XCTAssertEqual(store.integer(forKey: "i"), 0)
        XCTAssertEqual(store.double(forKey: "d"), 0.25)
        XCTAssertEqual(store.string(forKey: "s"), "")
        XCTAssertEqual(store.strings(forKey: "ss"), ["a", "b"])
        XCTAssertEqual(store.integers(forKey: "is"), [3, 1])

        store.remove(forKey: "b")
        XCTAssertNil(store.bool(forKey: "b"))
    }

    func testInMemoryRoundTripsAndReadsMissingAsNil() {
        let store = InMemorySettingsStore()
        roundTrip(store)
        XCTAssertEqual(store.keys, ["i", "d", "s", "ss", "is"])
        // Another type under the key reads as nil.
        XCTAssertNil(store.integer(forKey: "s"))
        XCTAssertNil(store.strings(forKey: "is"))
    }

    func testUserDefaultsRoundTripsAndReadsMissingAsNil() {
        let suite = "deskling.tests.\(UUID().uuidString)"
        let defaults = UserDefaults(suiteName: suite)!
        defer { defaults.removePersistentDomain(forName: suite) }
        roundTrip(UserDefaultsSettingsStore(defaults: defaults))
    }
}
