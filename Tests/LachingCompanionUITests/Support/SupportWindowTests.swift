#if os(macOS)
    import AppKit
    import LachingCompanionUI
    import LachingStore
    import LachingTesting
    import SwiftUI
    import XCTest

    /// A button style that draws just the label.
    struct LabelOnly: ButtonStyle {
        func makeBody(configuration: Configuration) -> some View { configuration.label }
    }

    @MainActor
    final class SupportWindowTests: XCTestCase {
        private typealias Window = SupportWindow<LabelOnly, LabelOnly>

        func testOneAskAtATimeAndOnlyThanksRightAfterAPurchase() {
            XCTAssertEqual(Window.stage(justSupported: false, justTipped: false, isSupporter: false), .pitch)
            XCTAssertEqual(Window.stage(justSupported: false, justTipped: false, isSupporter: true), .supporter)
            XCTAssertEqual(Window.stage(justSupported: true, justTipped: false, isSupporter: true), .welcome)
            XCTAssertEqual(Window.stage(justSupported: false, justTipped: true, isSupporter: true), .tipped)
            XCTAssertEqual(Window.stage(justSupported: true, justTipped: true, isSupporter: true), .welcome, "the purchase thanks first")
        }

        func testTheWindowDrawsWithEveryWordFromTheApp() {
            let catalog = ProductCatalog(unlockables: ["fixture.supporter"], tips: ["fixture.tip.small", "fixture.tip.large"])
            let defaults = UserDefaults(suiteName: "laching.tests.\(UUID().uuidString)")!
            let purchases = PurchaseStore(catalog: catalog, backend: MockStoreBackend(catalog: catalog), defaults: defaults)
            let copy = SupportCopy(
                pitch: Text(verbatim: "Pitch"), becomeSupporter: { Text(verbatim: "Become a supporter \($0 ?? "")") },
                supporterNote: Text(verbatim: "Pay once"), supporterThanks: Text(verbatim: "Thanks"),
                tipTitle: { $0 }, tipsNote: Text(verbatim: "Tips unlock nothing"), welcomeLine: "Welcome", welcomeNote: nil,
                tipThanksLine: "Thank you", error: { "\($0)" }, askToBuy: Text(verbatim: "Waiting"),
                pricesFailed: Text(verbatim: "No prices"), tryAgain: Text(verbatim: "Try again"), restore: Text(verbatim: "Restore"),
                done: Text(verbatim: "Done"))
            let look = SupportLook(
                title: AnyView(Text(verbatim: "Support")), tipsHeader: AnyView(Text(verbatim: "Tips")),
                character: { AnyView(Color.green.frame(width: $0 ? 150 : 96, height: $0 ? 150 : 96)) },
                bubble: { AnyView(SpeechBubble(text: $0, tail: .bottom, size: 18, fillsWidth: false)) }, primary: LabelOnly(),
                largePrimary: LabelOnly(), outline: LabelOnly(), tipPrice: { AnyView($0.opacity(0.8)) })
            let window = SupportWindow(
                purchases: purchases, products: SupportProducts(supporter: "fixture.supporter", tips: catalog.tips), copy: copy,
                look: look, onDone: {})
            let size = NSHostingController(rootView: window).sizeThatFits(in: CGSize(width: 460, height: 580))
            XCTAssertGreaterThan(size.height, 100)
        }
    }
#endif
