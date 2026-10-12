#if os(macOS)
    import AppKit
    import DesklingShell
    import SwiftUI
    import XCTest

    @MainActor
    final class LayoutCheckTests: XCTestCase {
        /// A page: a block of the given height, then a button row at the bottom, as a window lays one out.
        private func page(_ height: CGFloat) -> some View {
            VStack(spacing: 0) {
                Color.clear.frame(height: height)
                Spacer(minLength: 16)
                Text("Done").padding(8)
            }
            .padding(24)
            .frame(maxWidth: .infinity, maxHeight: .infinity)
        }

        private func window(_ content: some View, size: CGSize) -> NSWindow {
            let window = NSWindow(
                contentRect: NSRect(origin: .zero, size: size), styleMask: [.borderless], backing: .buffered, defer: false)
            window.isReleasedWhenClosed = false
            window.contentViewController = NSHostingController(rootView: content)
            window.setContentSize(size)
            window.contentView?.layoutSubtreeIfNeeded()
            addTeardownBlock { window.close() }
            return window
        }

        func testContentThatFitsHasNoOverflow() {
            XCTAssertNil(LayoutCheck.overflow(of: page(200), in: CGSize(width: 300, height: 400)))
            XCTAssertNil(LayoutCheck.overflow(in: window(page(200), size: CGSize(width: 300, height: 400))))
        }

        func testTallerContentReportsWhatItNeeds() throws {
            let overflow = try XCTUnwrap(LayoutCheck.overflow(of: page(400), in: CGSize(width: 300, height: 400)))
            XCTAssertEqual(overflow.available, 400)
            XCTAssertGreaterThan(overflow.needed, 400 + 48)  // the block, the spacer's minimum, the button, the padding
            XCTAssertTrue(overflow.description.hasPrefix("the content needs"))
            XCTAssertNotNil(LayoutCheck.overflow(in: window(page(400), size: CGSize(width: 300, height: 400))))
        }

        func testTextWrapsToTheWidthBeforeItIsMeasured() {
            let text = Text(String(repeating: "Words that wrap onto many lines. ", count: 40))
            XCTAssertGreaterThan(LayoutCheck.neededHeight(of: text, width: 200), LayoutCheck.neededHeight(of: text, width: 600))
            XCTAssertNotNil(LayoutCheck.overflow(of: text, in: CGSize(width: 200, height: 100)))
        }

        func testWindowsThatScrollAreSkipped() {
            let scrolling = ScrollView { page(1000) }
            XCTAssertNil(LayoutCheck.overflow(in: window(scrolling, size: CGSize(width: 300, height: 400))))
        }

        func testWindowsWithoutSwiftUIAreSkipped() {
            let window = NSWindow(
                contentRect: NSRect(x: 0, y: 0, width: 100, height: 100), styleMask: [.borderless], backing: .buffered,
                defer: false)
            window.isReleasedWhenClosed = false
            XCTAssertNil(LayoutCheck.overflow(in: window))
        }

        func testContentRoomLeavesTheTitleBarOut() {
            let room = LayoutCheck.contentRoom(forWindowOfSize: CGSize(width: 400, height: 500))
            XCTAssertEqual(room.width, 400)
            XCTAssertLessThan(room.height, 500)
            XCTAssertGreaterThan(room.height, 440)
        }
    }
#endif
