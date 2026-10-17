import LachingCompanion
import LachingTesting
import XCTest

/// The rig over the raster fixture (`Tests/Fixtures/characters/fixture-raster`): image parts are placed, never drawn.
final class RasterCharacterTests: XCTestCase {
    let character = RiggedCharacter(art: FixtureArt.raster)

    func testEveryPartIsAnImageNamedForTheSkin() {
        for (name, content) in FixtureArt.raster.parts {
            guard case .image(let asset, let frame) = content else { return XCTFail("\(name) isn't an image") }
            XCTAssertEqual(asset, "fixture-raster.\(name)")
            XCTAssertGreaterThan(frame.size.width, 0)
        }
        XCTAssertEqual(FixtureArt.raster.parts["head"], .image(asset: "fixture-raster.head", frame: Rect(24, 14, 52, 52)))
    }

    func testLayersPlaceTheImages() {
        let layers = character.layers(for: character.stillPose(mood: .asleep))
        XCTAssertEqual(layers.map(\.name), ["stem", "head", "cheeks", "eyes-closed", "mouth-talk-2", "zz"])
        XCTAssertTrue(layers.allSatisfy { $0.content.kind == "image" })
        XCTAssertEqual(layers.last?.transform, .identity)
    }

    func testHeadTransformIsDroopThenSink() {
        let pose = character.pose(mood: .shushed, time: 0)
        let head = character.layers(for: pose).first { $0.name == "eyes-open" }!.transform
        let expected = Transform.rotation(degrees: 14, around: Point(50, 62)).concatenating(.translation(0, 2))
        XCTAssertEqual(head, expected)
        // Rotation and translation only: the 2×2 part stays orthonormal, so an image never shears or stretches.
        let a = head.affine
        XCTAssertEqual(a.a * a.a + a.b * a.b, 1, accuracy: 1e-12)
        XCTAssertEqual(a.a * a.c + a.b * a.d, 0, accuracy: 1e-12)
    }

    func testPulseScalesTheHeadUniformly() {
        let pose = character.pose(mood: .talking, time: 1.0 / 12)
        XCTAssertGreaterThan(pose.pulse, 0)
        let head = character.layers(for: pose).first { $0.name == "head" }!.transform
        XCTAssertEqual(head.a, head.d, accuracy: 1e-12)
        XCTAssertEqual(head.b, -head.c, accuracy: 1e-12)
        XCTAssertEqual((head.a * head.a + head.b * head.b).squareRoot(), 1 + pose.pulse, accuracy: 1e-12)
    }

    func testShapesSkipImageParts() {
        for mood in Mood.allCases {
            XCTAssertEqual(character.shapes(for: character.stillPose(mood: mood)), [], "\(mood)")
        }
    }

    func testTheGlyphStaysVector() {
        XCTAssertFalse(character.glyph(mood: .awake, frame: 0).isEmpty)
        XCTAssertEqual(character.glyph(mood: .talking, frame: 1), RiggedCharacter(art: FixtureArt.vector).glyph(mood: .talking, frame: 1))
    }
}
