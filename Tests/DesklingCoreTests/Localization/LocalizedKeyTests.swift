import DesklingCore
import Foundation
import XCTest

final class LocalizedKeyTests: XCTestCase {
    func testBothArgumentKindsRoundTripThroughJSON() throws {
        let key = LocalizedKey(
            "stats.breaks",
            [
                LocalizedKey.Argument(type: .int, value: 3),
                LocalizedKey.Argument(type: .durationMinutes, value: 65),
            ]
        )

        let encoder = JSONEncoder()
        encoder.outputFormatting = [.sortedKeys]
        let json = try XCTUnwrap(String(data: try encoder.encode(key), encoding: .utf8))
        XCTAssertEqual(
            json,
            #"{"args":[{"type":"int","value":3},{"type":"durationMinutes","value":65}],"key":"stats.breaks"}"#
        )

        let decoded = try JSONDecoder().decode(LocalizedKey.self, from: Data(json.utf8))
        XCTAssertEqual(decoded, key)
    }

    func testAnUnknownKindFailsToDecode() {
        let json = Data(#"{"key":"stats.breaks","args":[{"type":"float","value":3}]}"#.utf8)
        XCTAssertThrowsError(try JSONDecoder().decode(LocalizedKey.self, from: json))
    }
}
