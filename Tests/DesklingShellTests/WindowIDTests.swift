#if os(macOS)
    import AppKit
    import DesklingShell
    import XCTest

    private enum PlainWindow: WindowID {
        case settings, help

        var title: String { "Plain" }
        var size: CGSize { CGSize(width: 400, height: 300) }
    }

    private enum CustomWindow: WindowID {
        case session

        var title: String { "Custom" }
        var size: CGSize { CGSize(width: 620, height: 700) }
        var isExclusive: Bool { true }
        var placement: WindowPlacement { .underMenuBar }
        var minSize: CGSize? { CGSize(width: 560, height: 620) }
        var level: NSWindow.Level { .floating }
        var frameAutosaveName: String? { "SessionWindow" }
    }

    final class WindowIDTests: XCTestCase {
        func testProtocolDefaults() {
            XCTAssertFalse(PlainWindow.settings.isExclusive)
            XCTAssertEqual(PlainWindow.settings.placement, .center)
            XCTAssertNil(PlainWindow.settings.minSize)
            XCTAssertEqual(PlainWindow.settings.level, .normal)
            XCTAssertNil(PlainWindow.help.frameAutosaveName)
            XCTAssertEqual(PlainWindow.help.size, CGSize(width: 400, height: 300))
        }

        func testOverridesWin() {
            XCTAssertTrue(CustomWindow.session.isExclusive)
            XCTAssertEqual(CustomWindow.session.placement, .underMenuBar)
            XCTAssertEqual(CustomWindow.session.minSize, CGSize(width: 560, height: 620))
            XCTAssertEqual(CustomWindow.session.level, .floating)
            XCTAssertEqual(CustomWindow.session.frameAutosaveName, "SessionWindow")
        }

        func testIDsAreHashable() {
            var open: [PlainWindow: Int] = [.settings: 1]
            open[.help] = 2
            XCTAssertEqual(open.count, 2)
            XCTAssertEqual(Set([PlainWindow.settings, .settings, .help]).count, 2)
        }
    }
#endif
