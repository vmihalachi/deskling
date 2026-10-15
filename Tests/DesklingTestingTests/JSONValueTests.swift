import DesklingTesting
import Foundation
import XCTest

final class JSONValueTests: XCTestCase {
    func testEncodesLikeACodableStruct() throws {
        struct Plain: Encodable {
            var count = 30
            var rate = 0.5
            var name: String? = nil
            var tags = ["a"]
        }
        var tree = try JSONValue.from(Plain())
        tree["name"] = .null
        let text = String(decoding: try ConformanceSupport.encode(tree), as: UTF8.self)
        XCTAssertTrue(text.contains("\"count\" : 30"), text)
        XCTAssertTrue(text.contains("\"rate\" : 0.5"), text)
        XCTAssertTrue(text.contains("\"name\" : null"), text)
    }

    func testEditing() {
        var tree: JSONValue = .object(["order": .strings(["a", "c"])])
        tree["order"] = tree["order"]?.inserting(.string("b"), after: .string("a"))
        XCTAssertEqual(tree["order"], .strings(["a", "b", "c"]))
        XCTAssertEqual(JSONValue.strings(["x"]).inserting(.string("y"), after: .string("missing")), .strings(["x", "y"]))
        var scalar: JSONValue = .int(1)
        scalar["key"] = .bool(true)
        XCTAssertEqual(scalar, .int(1))
    }

    func testRoundTrip() throws {
        let tree: JSONValue = .object(["a": .array([.int(1), .double(1.5), .bool(false), .null, .string("s")])])
        XCTAssertEqual(try ConformanceSupport.decode(JSONValue.self, from: try ConformanceSupport.encode(tree)), tree)
    }

    /// The vectors are diffed as text, so the keys come out sorted at every level whatever order they were built in.
    func testEncodesKeysSortedAtEveryLevel() throws {
        let tree: JSONValue = .object(["zeta": .int(1), "alpha": .object(["y": .bool(true), "x": .int(3)]), "mid": .null])
        let text = String(decoding: try ConformanceSupport.encode(tree), as: UTF8.self)
        let keys = ["\"alpha\"", "\"x\"", "\"y\"", "\"mid\"", "\"zeta\""]
        let positions = try keys.map { try XCTUnwrap(text.range(of: $0)).lowerBound }
        XCTAssertEqual(positions, positions.sorted(), text)
    }

    func testAWholeNumberDecodesAsAnIntAndAFractionAsADouble() throws {
        let decoded = try ConformanceSupport.decode(JSONValue.self, from: Data(#"{"a": 2, "b": 2.5, "c": true, "d": null}"#.utf8))
        XCTAssertEqual(decoded, .object(["a": .int(2), "b": .double(2.5), "c": .bool(true), "d": .null]))
    }
}
