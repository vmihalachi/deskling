#if os(macOS)
    import AppKit
    import DesklingCompanion
    import DesklingCompanionUI
    import DesklingTesting
    import SwiftUI
    import XCTest

    final class CharacterViewTests: XCTestCase {
        private let character = RiggedCharacter(art: FixtureArt.vector)

        func testWakeSpringStartsAtZeroAndSettlesByItsDuration() {
            XCTAssertEqual(CharacterView.wakeProgress(elapsed: 0), 0)
            XCTAssertEqual(CharacterView.wakeProgress(elapsed: CharacterView.wakeDuration), 1)
            XCTAssertEqual(CharacterView.wakeProgress(elapsed: 5), 1)
            let half = CharacterView.wakeProgress(elapsed: 0.3)
            XCTAssertGreaterThan(half, 0.5)
            XCTAssertLessThan(half, 1)
            var last = -1.0
            for step in 0...60 {
                let progress = CharacterView.wakeProgress(elapsed: Double(step) / 100)
                XCTAssertGreaterThanOrEqual(progress, last)
                last = progress
            }
        }

        func testWakingBlendsFromTheShushedPose() {
            let shushed = character.stillPose(mood: .shushed)
            let awake = character.pose(mood: .awake, time: 2)
            XCTAssertEqual(CharacterView.pose(waking: shushed, to: awake, elapsed: 0), shushed)
            XCTAssertEqual(CharacterView.pose(waking: shushed, to: awake, elapsed: 0.6), awake)
            let early = CharacterView.pose(waking: shushed, to: awake, elapsed: 0.05)
            XCTAssertGreaterThan(early.headAngle, awake.headAngle)
            XCTAssertLessThan(early.headAngle, shushed.headAngle)
        }

        @MainActor
        func testGlyphImagesAreTemplates() {
            for mood in Mood.allCases {
                for frame in 0..<character.glyphFrameCount(mood: mood) {
                    let image = GlyphRenderer.image(character: character, mood: mood, frame: frame, accessibilityDescription: "Fixture")
                    XCTAssertTrue(image.isTemplate, "\(mood) \(frame)")
                    XCTAssertEqual(image.size, NSSize(width: 18, height: 18))
                    XCTAssertEqual(image.accessibilityDescription, "Fixture")
                }
            }
            let big = GlyphRenderer.image(character: character, mood: .awake, size: 36, accessibilityDescription: "Fixture")
            XCTAssertEqual(big.size, NSSize(width: 36, height: 36))
        }

        // MARK: Image parts

        /// The character drawn still in `mood` at 100 × 100 px (the view box at scale 1).
        @MainActor
        private func render(_ mood: Mood) throws -> NSBitmapImageRep {
            let view = CharacterView(
                character: RiggedCharacter(art: FixtureArtForUI.square), mood: mood, talking: false, line: nil, animated: false,
                artBundle: .module
            )
            .frame(width: 100, height: 100)
            let renderer = ImageRenderer(content: view)
            renderer.scale = 1
            return NSBitmapImageRep(cgImage: try XCTUnwrap(renderer.cgImage))
        }

        private func isBlue(_ bitmap: NSBitmapImageRep, _ x: Int, _ y: Int) -> Bool {
            guard let color = bitmap.colorAt(x: x, y: y)?.usingColorSpace(.sRGB) else { return false }
            return color.alphaComponent > 0.9 && color.blueComponent > 0.8 && color.redComponent < 0.2
        }

        private func isRed(_ bitmap: NSBitmapImageRep, _ x: Int, _ y: Int) -> Bool {
            guard let color = bitmap.colorAt(x: x, y: y)?.usingColorSpace(.sRGB) else { return false }
            return color.alphaComponent > 0.9 && color.redComponent > 0.8 && color.blueComponent < 0.2
        }

        @MainActor
        func testTheHeadLayerMovesWithTheHeadAngle() throws {
            let awake = try render(.awake)
            XCTAssertTrue(isBlue(awake, 50, 20), "the head stands above the neck")
            XCTAssertFalse(isBlue(awake, 75, 50))
            XCTAssertTrue(isRed(awake, 50, 80), "the body")
            // Shushed droops 90° clockwise around the neck: the head (30…70, 10…50) turns to (50…90, 30…70).
            let shushed = try render(.shushed)
            XCTAssertFalse(isBlue(shushed, 50, 20))
            XCTAssertTrue(isBlue(shushed, 75, 50))
            XCTAssertTrue(isRed(shushed, 50, 80), "the body doesn't follow (follow 0)")
        }
    }
#endif
