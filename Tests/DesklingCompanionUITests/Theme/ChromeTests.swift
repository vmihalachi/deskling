#if os(macOS)
    import AppKit
    import DesklingCompanion
    import DesklingCompanionUI
    import DesklingShell
    import DesklingTesting
    import SwiftUI
    import XCTest

    @MainActor
    final class ChromeTests: XCTestCase {
        func testTheStandFillsTheDesktopWindowAtEverySize() {
            let layout = DesktopLayout(art: FixtureArt.vector)
            for size in DesktopSize.allCases {
                let window = layout.windowSize(for: size)
                let stand = DesktopCompanion.stand(character: AnyView(Color.green), layout: layout)
                let fitted = NSHostingController(rootView: stand).sizeThatFits(in: window)
                XCTAssertEqual(fitted, window, "\(size)")
            }
            XCTAssertEqual(layout.shadowWidthRatio, 0.42)
            XCTAssertEqual(DesktopLayout(art: FixtureArt.vector, shadowWidthRatio: 0.5).shadowWidthRatio, 0.5)
        }

        func testTheWindowInsetsAreTheSpacingSteps() {
            XCTAssertEqual(WindowInset.side, Space.region)
            XCTAssertEqual(WindowInset.top, Space.section)
            XCTAssertEqual(WindowInset.bottom, Space.region)
            XCTAssertEqual(WindowInset.panel, Space.group)
        }

        func testFontAvailabilityNeedsEveryName() {
            FontRegistration.register(bundle: .main)
            FontRegistration.register(bundle: .main)
            XCTAssertTrue(FontRegistration.isAvailable(["Helvetica"]))
            XCTAssertFalse(FontRegistration.isAvailable(["Helvetica", "No-Such-Font"]))
        }

        func testAPhraseIsLocalizedPartByPartWithTheSourceAsFallback() {
            let joke = PhraseBank.fixture["joke-1"]!
            XCTAssertEqual(joke.localizedParts(), ["Fixture joke, setup", "Fixture joke, punchline"])
            XCTAssertEqual(joke.localizedText, "Fixture joke, setup Fixture joke, punchline")
            XCTAssertNil(PhraseBank.fixture["tune-1"]!.localizedText)
            XCTAssertEqual(Phrase.localize("no.such.key", "Source"), "Source")
        }
    }
#endif
