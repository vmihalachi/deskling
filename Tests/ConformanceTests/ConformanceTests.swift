import XCTest

final class ConformanceTests: XCTestCase {
    func testVectorsFolderIsFound() {
        XCTAssertTrue(FileManager.default.fileExists(atPath: ConformanceRoot.url.appendingPathComponent("README.md").path))
    }
}
