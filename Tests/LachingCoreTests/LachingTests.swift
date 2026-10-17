import LachingCore
import XCTest

final class LachingTests: XCTestCase {
    func testVersionLooksLikeSemVer() {
        let parts = Laching.version.split(separator: ".")
        XCTAssertEqual(parts.count, 3)
        XCTAssertTrue(parts.allSatisfy { Int($0) != nil }, Laching.version)
    }
}
