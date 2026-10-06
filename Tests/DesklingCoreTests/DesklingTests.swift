import DesklingCore
import XCTest

final class DesklingTests: XCTestCase {
    func testVersionLooksLikeSemVer() {
        let parts = Deskling.version.split(separator: ".")
        XCTAssertEqual(parts.count, 3)
        XCTAssertTrue(parts.allSatisfy { Int($0) != nil }, Deskling.version)
    }
}
