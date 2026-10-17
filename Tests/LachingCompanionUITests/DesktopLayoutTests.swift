#if os(macOS)
    import Foundation
    import LachingCompanion
    import LachingCompanionUI
    import XCTest

    final class DesktopLayoutTests: XCTestCase {
        /// sunnysays' sunflower (120 × 132, the bubble at the face, 44 down, tucked 12 %): the numbers its desktop used.
        private func sunflower() throws -> CharacterArt {
            let url = try XCTUnwrap(Bundle.module.url(forResource: "sunflower", withExtension: "json"))
            return try CharacterArtJSON.decode(try Data(contentsOf: url))
        }

        func testTheSunflowersNumbers() throws {
            let layout = DesktopLayout(art: try sunflower())
            XCTAssertEqual(layout.headHeightRatio, 44.0 / 132.0, accuracy: 1e-12)
            XCTAssertEqual(layout.bubbleTuck, 0.12)
            XCTAssertEqual(layout.aspect, 120.0 / 132.0, accuracy: 1e-12)
            XCTAssertEqual(layout.shadowHeight, 4)
            XCTAssertEqual(layout.bubbleMaxWidth, 240)
            XCTAssertEqual(layout.bubbleMargin, 4)
        }

        func testWindowSizesFollowTheArt() throws {
            let layout = DesktopLayout(art: try sunflower())
            XCTAssertEqual(layout.windowSize(for: .small), CGSize(width: 58, height: 68))
            XCTAssertEqual(layout.windowSize(for: .medium), CGSize(width: 87, height: 100))
            XCTAssertEqual(layout.windowSize(for: .large), CGSize(width: 127, height: 144))
        }

        func testTheBubbleMeetsTheHead() throws {
            let layout = DesktopLayout(art: try sunflower())
            let frame = CGRect(x: 100, y: 200, width: 87, height: 100)
            XCTAssertEqual(layout.headY(in: frame), 300 - 96 * 44 / 132, accuracy: 1e-9)
            XCTAssertEqual(layout.bubbleGap(width: 87), -87 * 0.12, accuracy: 1e-9)
            XCTAssertEqual(layout.bubbleTailY(height: 80), 4 + 25)
            XCTAssertEqual(layout.bubbleTailY(height: 40), 4 + 16)
        }

        func testAnotherArtGetsItsOwnNumbers() {
            let layout = DesktopLayout(art: FixtureArtForUI.square)
            XCTAssertEqual(layout.headHeightRatio, 0.3)
            XCTAssertEqual(layout.bubbleTuck, 0.1)
            XCTAssertEqual(layout.aspect, 1)
        }
    }
#endif
