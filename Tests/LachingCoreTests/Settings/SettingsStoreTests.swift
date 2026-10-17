import Foundation
import LachingCore
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

    /// Another type under the key reads as nil, the numbers included: a boolean is only a `Bool`, 1 only an `Int`
    /// and 3.0 only a `Double`, where `UserDefaults`' own getters would coerce them to 1, `true` and 3.
    private func readsOtherTypesAsNil(_ store: SettingsStore) {
        store.set(bool: true, forKey: "b")
        store.set(integer: 1, forKey: "i")
        store.set(double: 3, forKey: "d")
        store.set(string: "42", forKey: "s")
        store.set(strings: ["1"], forKey: "ss")
        store.set(integers: [1, 0], forKey: "is")
        XCTAssertNil(store.integer(forKey: "b"))
        XCTAssertNil(store.double(forKey: "b"))
        XCTAssertNil(store.string(forKey: "b"))
        XCTAssertNil(store.bool(forKey: "i"))
        XCTAssertNil(store.double(forKey: "i"))
        XCTAssertNil(store.string(forKey: "i"))
        XCTAssertNil(store.integers(forKey: "i"))
        XCTAssertNil(store.bool(forKey: "d"))
        XCTAssertNil(store.integer(forKey: "d"))
        XCTAssertNil(store.string(forKey: "d"))
        XCTAssertNil(store.bool(forKey: "s"))
        XCTAssertNil(store.integer(forKey: "s"))
        XCTAssertNil(store.double(forKey: "s"))
        XCTAssertNil(store.strings(forKey: "s"))
        XCTAssertNil(store.integers(forKey: "ss"))
        XCTAssertNil(store.strings(forKey: "is"))
        XCTAssertNil(store.integer(forKey: "is"))
        // The matching getter still reads each.
        XCTAssertEqual(store.bool(forKey: "b"), true)
        XCTAssertEqual(store.integer(forKey: "i"), 1)
        XCTAssertEqual(store.double(forKey: "d"), 3)
        XCTAssertEqual(store.string(forKey: "s"), "42")
        XCTAssertEqual(store.strings(forKey: "ss"), ["1"])
        XCTAssertEqual(store.integers(forKey: "is"), [1, 0])
    }

    func testInMemoryRoundTripsAndReadsMissingAsNil() {
        let store = InMemorySettingsStore()
        roundTrip(store)
        XCTAssertEqual(store.keys, ["i", "d", "s", "ss", "is"])
        readsOtherTypesAsNil(store)
    }

    func testUserDefaultsRoundTripsAndReadsMissingAsNil() {
        let suite = "laching.tests.\(UUID().uuidString)"
        let defaults = UserDefaults(suiteName: suite)!
        defer { defaults.removePersistentDomain(forName: suite) }
        let store = UserDefaultsSettingsStore(defaults: defaults)
        roundTrip(store)
        readsOtherTypesAsNil(store)
        // A list written past the store reads only when every element has the list's type.
        defaults.set([true, false], forKey: "bs")
        defaults.set([1, 2.5] as [Any], forKey: "mixed")
        XCTAssertNil(store.integers(forKey: "bs"))
        XCTAssertNil(store.integers(forKey: "mixed"))
        XCTAssertNil(store.strings(forKey: "bs"))
    }
}
