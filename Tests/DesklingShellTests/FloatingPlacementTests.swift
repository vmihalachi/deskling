#if os(macOS)
    import CoreGraphics
    import DesklingShell
    import XCTest

    final class FloatingPlacementTests: XCTestCase {
        /// A 1440 × 900 primary screen with a 25 pt menu bar and a 70 pt Dock, and a 1920 × 1080 screen to its right.
        private let primary = CGRect(x: 0, y: 70, width: 1440, height: 805)
        private let second = CGRect(x: 1440, y: -180, width: 1920, height: 1055)
        private let size = CGSize(width: 87, height: 96)

        func testFrameStandsOnTheAnchor() {
            let frame = FloatingPlacement.frame(anchor: CGPoint(x: 100, y: 200), size: size)
            XCTAssertEqual(frame, CGRect(x: 56.5, y: 200, width: 87, height: 96))
            XCTAssertEqual(FloatingPlacement.anchor(of: frame), CGPoint(x: 100, y: 200))
        }

        func testDefaultAnchorIsBottomRightAboveTheDock() {
            let anchor = FloatingPlacement.defaultAnchor(size: size, in: primary, inset: 24)
            XCTAssertEqual(anchor, CGPoint(x: 1440 - 24 - 43.5, y: 94))
            XCTAssertEqual(FloatingPlacement.clamped(anchor: anchor, size: size, into: [primary]), anchor)
        }

        func testAWindowThatFitsStaysPut() {
            let onSecond = CGPoint(x: 2000, y: 0)
            XCTAssertEqual(FloatingPlacement.clamped(anchor: onSecond, size: size, into: [primary, second]), onSecond)
        }

        func testAWindowHangingOffAnEdgeIsPulledIn() {
            let clamped = FloatingPlacement.clamped(anchor: CGPoint(x: 10, y: 850), size: size, into: [primary])
            XCTAssertEqual(clamped, CGPoint(x: 43.5, y: 875 - 96))
        }

        func testARemovedDisplaySendsTheWindowToTheNearestScreen() {
            // Saved on the second screen, which is gone now.
            let clamped = FloatingPlacement.clamped(anchor: CGPoint(x: 2600, y: 300), size: size, into: [primary])
            XCTAssertEqual(clamped, CGPoint(x: 1440 - 43.5, y: 300))
        }

        func testTheScreenHoldingTheAnchorWinsOverANearerOne() {
            // Anchor just inside the second screen, window straddling both.
            let clamped = FloatingPlacement.clamped(anchor: CGPoint(x: 1450, y: 300), size: size, into: [primary, second])
            XCTAssertEqual(clamped, CGPoint(x: 1440 + 43.5, y: 300))
        }

        func testNoScreensLeavesTheAnchorAlone() {
            XCTAssertEqual(FloatingPlacement.clamped(anchor: CGPoint(x: -500, y: 9000), size: size, into: []), CGPoint(x: -500, y: 9000))
        }

        func testAWindowBiggerThanTheScreenIsCentered() {
            let tiny = CGRect(x: 0, y: 0, width: 50, height: 50)
            let clamped = FloatingPlacement.clamped(anchor: CGPoint(x: 300, y: 300), size: size, into: [tiny])
            XCTAssertEqual(clamped.x, 25, accuracy: 1e-9)
        }

        func testTheSideFacesTheMiddleOfTheScreen() {
            let nearRight = FloatingPlacement.frame(anchor: CGPoint(x: 1300, y: 100), size: size)
            let nearLeft = FloatingPlacement.frame(anchor: CGPoint(x: 100, y: 100), size: size)
            XCTAssertEqual(FloatingPlacement.side(beside: nearRight, in: primary), .left)
            XCTAssertEqual(FloatingPlacement.side(beside: nearLeft, in: primary), .right)
        }

        func testABesideFrameSitsNextToTheParentAndOnScreen() {
            let parent = FloatingPlacement.frame(anchor: CGPoint(x: 1300, y: 100), size: size)
            let bubble = CGSize(width: 200, height: 60)
            let left = FloatingPlacement.frame(size: bubble, beside: parent, on: .left, centerY: 160, gap: -4, in: primary)
            XCTAssertEqual(left, CGRect(x: parent.minX + 4 - 200, y: 130, width: 200, height: 60))
            // Too close to the top: pushed down to stay under the menu bar.
            let high = FloatingPlacement.frame(size: bubble, beside: parent, on: .right, centerY: 870, gap: 0, in: primary)
            XCTAssertEqual(high.maxY, primary.maxY)
            // No room on the right: kept on screen.
            XCTAssertLessThanOrEqual(high.maxX, primary.maxX)
        }

        func testTopLeftOriginRoundTrips() {
            let point = CGPoint(x: 1300, y: 94)
            let stored = FloatingPlacement.topLeftOrigin(point, primaryHeight: 900)
            XCTAssertEqual(stored, CGPoint(x: 1300, y: 806))
            XCTAssertEqual(FloatingPlacement.fromTopLeftOrigin(stored, primaryHeight: 900), point)
        }
    }
#endif
