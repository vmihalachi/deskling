import DesklingCompanion
import DesklingTesting
import Foundation
import XCTest

/// `CharacterArtJSON` writes what `tools/character.py` writes, byte for byte, and reads it back.
final class CharacterArtJSONTests: XCTestCase {
    /// `conformance/companion/characters/<skin>.json`, written by the compiler (`scripts/fixture-art.sh`).
    private func committed(_ skin: SkinID) throws -> Data {
        let root = try XCTUnwrap(ConformanceRoot.find(markerFile: "conformance/README.md", from: #filePath))
        return try Data(contentsOf: root.appendingPathComponent("companion/characters/\(skin.rawValue).json"))
    }

    /// A copy of sunnysays' `sunflower.json`, recompiled by `tools/character.py` (with `bubble` and the
    /// `headExtras`/`overlays` spelling): real art, a hundred-odd shapes.
    private func sunflower() throws -> Data {
        let url = try XCTUnwrap(Bundle.module.url(forResource: "Resources/sunflower", withExtension: "json"))
        return try Data(contentsOf: url)
    }

    func testEncodeWritesTheCompilersBytes() throws {
        for art in [FixtureArt.vector, FixtureArt.raster] {
            let encoded = String(decoding: try CharacterArtJSON.encode(art), as: UTF8.self)
            XCTAssertEqual(encoded, String(decoding: try committed(art.skin), as: UTF8.self), "\(art.skin)")
        }
    }

    func testFixturesRoundTrip() throws {
        for art in [FixtureArt.vector, FixtureArt.raster] {
            XCTAssertEqual(try CharacterArtJSON.decode(try CharacterArtJSON.encode(art)), art, "\(art.skin)")
        }
    }

    func testDecodingTheCompiledFilesGivesTheGeneratedSwift() throws {
        XCTAssertEqual(try CharacterArtJSON.decode(try committed(FixtureArt.vector.skin)), FixtureArt.vector)
        XCTAssertEqual(try CharacterArtJSON.decode(try committed(FixtureArt.raster.skin)), FixtureArt.raster)
    }

    func testSunflowerDecodesAndEncodesToTheSameBytes() throws {
        let data = try sunflower()
        let art = try CharacterArtJSON.decode(data)
        XCTAssertEqual(art.skin, "sunflower")
        XCTAssertEqual(art.viewBox, Size(120, 132))
        XCTAssertEqual(art.rig.bubble, BubbleAnchor(pivot: "face", tuck: 0.12))
        XCTAssertEqual(art.rig.moods[.asleep]?.overlays, ["zz"])
        XCTAssertEqual(art.rig.moods[.offHours]?.headExtras, [])
        XCTAssertEqual(art.parts.count, 17)
        XCTAssertEqual(art.parts["petals"]?.shapes?.isEmpty, false)
        XCTAssertEqual(try CharacterArtJSON.encode(art), data)
        let sunny = RiggedCharacter(art: art)
        XCTAssertEqual(sunny.stillPose(mood: .shushed).eyes, "eyes-lidded")
        XCTAssertEqual(sunny.stillPose(mood: .hushedForCall).mouth, "hush-leaf")
    }

    func testDecodeRefusesBrokenArt() throws {
        let base = try XCTUnwrap(JSONSerialization.jsonObject(with: try committed(FixtureArt.vector.skin)) as? [String: Any])
        func broken(_ change: (inout [String: Any]) -> Void) throws -> Data {
            var copy = base
            change(&copy)
            return try JSONSerialization.data(withJSONObject: copy)
        }
        func rig(_ change: @escaping (inout [String: Any]) -> Void) -> (inout [String: Any]) -> Void {
            { file in
                var rig = file["rig"] as! [String: Any]
                change(&rig)
                file["rig"] = rig
            }
        }
        func mood(_ name: String, _ change: @escaping (inout [String: Any]) -> Void) -> (inout [String: Any]) -> Void {
            rig { rig in
                var moods = rig["moods"] as! [String: Any]
                var entry = moods[name] as! [String: Any]
                change(&entry)
                moods[name] = entry
                rig["moods"] = moods
            }
        }
        let cases: [(String, (inout [String: Any]) -> Void, String)] = [
            ("missing mood", rig { rig in rig["moods"] = (rig["moods"] as! [String: Any]).filter { $0.key != "asleep" } }, "asleep"),
            ("unknown part", mood("asleep") { $0["overlays"] = ["snore"] }, "snore"),
            ("unknown pivot", rig { $0["bubble"] = ["pivot": "hat", "tuck": 0.1] }, "hat"),
            ("no bubble", rig { $0["bubble"] = nil }, "bubble"),
            ("bad frame", { $0["parts"] = ["head": ["image": "x", "frame": [1, 2, 3]]] }, "frame"),
            ("mouths without a rate", mood("talking") { $0["mouthRate"] = 0 }, "mouthRate"),
        ]
        for (name, change, needle) in cases {
            XCTAssertThrowsError(try CharacterArtJSON.decode(try broken(change)), name) { error in
                XCTAssertTrue("\(error)".contains(needle), "\(name): \(error)")
            }
        }
        let renamed = try broken(
            rig { rig in
                var moods = rig["moods"] as! [String: Any]
                moods["dancing"] = moods["awake"]
                rig["moods"] = moods
            })
        XCTAssertThrowsError(try CharacterArtJSON.decode(renamed)) { error in
            XCTAssertTrue("\(error)".contains("dancing"), "\(error)")
        }
    }

    func testNumbersAreWrittenLikeTheCompiler() throws {
        let art = FixtureArt.vector
        let text = String(decoding: try CharacterArtJSON.encode(art), as: UTF8.self)
        XCTAssertTrue(text.contains("\"tuck\": 0.1\n"))
        XCTAssertTrue(text.contains("\"viewBox\": [100, 100]"))
        XCTAssertTrue(text.contains("\"intervals\": [4.2, 5.6, 3.8]"))
        XCTAssertTrue(text.hasSuffix("}\n"))
    }
}
