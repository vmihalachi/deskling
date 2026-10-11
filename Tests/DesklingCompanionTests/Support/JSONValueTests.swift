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
}
