import DesklingCompanion
import DesklingTesting
import XCTest

/// The rig over the vector fixture (`Tests/Fixtures/characters/fixture-vector`).
final class RiggedCharacterTests: XCTestCase {
    let character = RiggedCharacter(art: FixtureArt.vector)

    func testEveryMoodPicksItsParts() {
        XCTAssertEqual(character.stillPose(mood: .awake).eyes, "eyes-open")
        XCTAssertEqual(character.stillPose(mood: .awake).mouth, "mouth-smile")
        XCTAssertEqual(character.stillPose(mood: .shushed).mouth, "mouth-flat")
        let asleep = character.stillPose(mood: .asleep)
        XCTAssertEqual(asleep.eyes, "eyes-closed")
        XCTAssertEqual(asleep.overlays, ["zz"])
        XCTAssertEqual(asleep.headExtras, ["cheeks"])
        XCTAssertEqual(character.stillPose(mood: .offHours).headExtras, [])
        XCTAssertEqual(character.stillPose(mood: .awake).headExtras, ["cheeks"])
        XCTAssertEqual(character.stillPose(mood: .awake).overlays, [])
        XCTAssertEqual(character.stillPose(mood: .hushedForCall).mouth, "mouth-flat")
    }

    func testMouthLevelsFollowTheVoice() {
        // The fixture talks with mouth-talk-1 and mouth-talk-2 and rests on mouth-flat between words.
        let expected = [(0, "mouth-flat"), (1, "mouth-talk-1"), (2, "mouth-talk-2"), (3, "mouth-talk-2")]
        for (level, mouth) in expected {
            let pose = character.pose(mood: .talking, time: 0.05, mouthLevel: level)
            XCTAssertEqual(pose.mouth, mouth, "level \(level)")
            XCTAssertEqual(pose.pulse, 0.03 * Double(level) / 3, accuracy: 1e-12, "level \(level)")
        }
        XCTAssertEqual(character.pose(mood: .talking, time: 0.2, mouthLevel: 9).mouth, "mouth-talk-2", "clamped")
        XCTAssertEqual(character.pose(mood: .talking, time: 0.2, mouthLevel: -1).mouth, "mouth-flat", "clamped")
        let timed = character.pose(mood: .talking, time: 0.2)
        XCTAssertEqual(character.pose(mood: .talking, time: 0.2, mouthLevel: nil), timed, "nil keeps the timer")
        let level = character.pose(mood: .talking, time: 0.2, mouthLevel: 2)
        XCTAssertEqual(level.headAngle, timed.headAngle, "the sway is the mood's either way")
    }

    func testVoicedMouthIndexSpreadsTheLevels() {
        XCTAssertEqual((1...3).map { RiggedCharacter.voicedMouthIndex(level: $0, count: 1) }, [0, 0, 0])
        XCTAssertEqual((1...3).map { RiggedCharacter.voicedMouthIndex(level: $0, count: 2) }, [0, 1, 1])
        XCTAssertEqual((1...3).map { RiggedCharacter.voicedMouthIndex(level: $0, count: 3) }, [0, 1, 2])
        XCTAssertEqual((1...3).map { RiggedCharacter.voicedMouthIndex(level: $0, count: 4) }, [0, 1, 3])
    }

    func testDroopAndSink() {
        let expected: [(Mood, Double, Double)] = [(.shushed, 14, 2), (.asleep, 24, 4), (.offHours, 10, 0), (.awake, 0, 0)]
        for (mood, angle, sink) in expected {
            let pose = character.pose(mood: mood, time: 0)
            XCTAssertEqual(pose.headAngle, angle, accuracy: 1e-9, "\(mood)")
            XCTAssertEqual(pose.sink, sink, accuracy: 1e-9, "\(mood)")
        }
    }

    func testBlinkSchedule() {
        // Intervals 4.2, 5.6, 3.8 (a 13.6 s cycle); each blink is the interval's last 0.12 s.
        XCTAssertTrue(character.pose(mood: .awake, time: 4.15).blinking)
        XCTAssertEqual(character.pose(mood: .awake, time: 4.15).eyes, "eyes-closed")
        XCTAssertFalse(character.pose(mood: .awake, time: 4.0).blinking)
        XCTAssertFalse(character.pose(mood: .awake, time: 4.2).blinking)
        XCTAssertTrue(character.pose(mood: .awake, time: 13.6 + 4.15).blinking)
        for time in stride(from: 0.0, to: 40, by: 0.01) {
            XCTAssertFalse(character.pose(mood: .shushed, time: time).blinking)
        }
    }

    func testLargeTimesStayExact() {
        XCTAssertTrue(character.pose(mood: .awake, time: 13.6 * 40_000 + 4.15).blinking)
        let far = character.pose(mood: .awake, time: 4 * 1_000_000 + 1).headAngle
        XCTAssertEqual(far, character.pose(mood: .awake, time: 1).headAngle, accuracy: 1e-9)
    }

    func testStillPoseNeverBlinksOrTalks() {
        for mood in Mood.allCases {
            let pose = character.stillPose(mood: mood)
            XCTAssertFalse(pose.blinking, "\(mood)")
            XCTAssertEqual(pose.mouth, character.art.rig.moods[mood]?.mouth.first, "\(mood)")
        }
    }

    func testTalkingAlternatesMouths() {
        XCTAssertEqual(character.pose(mood: .talking, time: 0.05).mouth, "mouth-talk-1")
        XCTAssertEqual(character.pose(mood: .talking, time: 0.2).mouth, "mouth-talk-2")
    }

    func testBlendSwitchesPartsAtHalf() {
        let from = character.stillPose(mood: .shushed)
        let to = character.stillPose(mood: .awake)
        let quarter = from.blended(to: to, progress: 0.25)
        XCTAssertEqual(quarter.headExtras, ["cheeks"])
        XCTAssertEqual(quarter.headAngle, 11.5, accuracy: 1e-9)
        XCTAssertEqual(from.blended(to: to, progress: 0.5).mouth, "mouth-smile")
        XCTAssertEqual(from.blended(to: to, progress: 0.25).mouth, "mouth-flat")
        XCTAssertEqual(from.blended(to: to, progress: 1), to)
    }

    func testBlendEndsExactlyOnTheTarget() {
        let from = character.stillPose(mood: .shushed)
        let to = character.pose(mood: .awake, time: 2)  // sin(π): a headAngle of about 5e-16
        XCTAssertEqual(from.blended(to: to, progress: 1), to)
        XCTAssertEqual(from.blended(to: to, progress: 0), from)
    }

    func testShapesStayInsideTheViewBox() {
        let box = character.viewBox
        for mood in Mood.allCases {
            for time in [0, 1, 4.15, 0.2] {
                for shape in character.shapes(for: character.pose(mood: mood, time: time)) {
                    for point in shape.path.flatMap(\.points) {
                        XCTAssertTrue(
                            (-2...box.width + 2).contains(point.x) && (-2...box.height + 2).contains(point.y),
                            "\(mood) at \(time): \(point)")
                    }
                }
            }
        }
    }

    func testShapesCarryTheMoodsParts() {
        let awake = character.shapes(for: character.stillPose(mood: .awake))
        let offHours = character.shapes(for: character.stillPose(mood: .offHours))
        XCTAssertTrue(awake.contains { $0.fill == "blush" })
        XCTAssertFalse(offHours.contains { $0.fill == "blush" })
        XCTAssertTrue(character.shapes(for: character.stillPose(mood: .asleep)).contains { $0.stroke == "inkSoft" })
    }

    func testLayersFollowTheDrawingOrder() {
        let names = character.layers(for: character.stillPose(mood: .asleep)).map(\.name)
        XCTAssertEqual(names, ["stem", "head", "face", "cheeks", "eyes-closed", "mouth-flat", "zz"])
        let layers = character.layers(for: character.pose(mood: .asleep, time: 0))
        XCTAssertEqual(layers.last?.transform, .identity, "overlays are never moved")
        XCTAssertEqual(layers[3].transform, layers[4].transform, "head extras ride on the head")
    }

    func testShapesAreTheLayersFlattened() {
        let pose = character.pose(mood: .talking, time: 0.2)
        let flattened = character.layers(for: pose).flatMap { layer in
            (layer.content.shapes ?? []).map { $0.transformed(by: layer.transform) }
        }
        XCTAssertEqual(character.shapes(for: pose), flattened)
    }

    func testDroopRotatesTheHeadAroundTheNeck() {
        let upright = character.shapes(for: character.pose(mood: .awake, time: 0))
        let drooped = character.shapes(for: character.pose(mood: .offHours, time: 0))
        func center(_ shapes: [PathShape], _ fill: PaintRole) -> Point {
            let points = shapes.first { $0.fill == fill }!.path.flatMap(\.points)
            return Point(points.map(\.x).reduce(0, +) / Double(points.count), points.map(\.y).reduce(0, +) / Double(points.count))
        }
        // The face moves right and down (clockwise in y-down coordinates); the stem follows a quarter as far.
        XCTAssertGreaterThan(center(drooped, "edge").x, center(upright, "edge").x + 3)
        XCTAssertGreaterThan(center(drooped, "edge").y, center(upright, "edge").y)
        let stemShift = center(drooped, "stem").x - center(upright, "stem").x
        XCTAssertLessThan(abs(stemShift), center(drooped, "edge").x - center(upright, "edge").x)
    }

    func testGlyphIsTemplateInk() {
        for mood in Mood.allCases {
            for frame in 0..<character.glyphFrameCount(mood: mood) {
                let shapes = character.glyph(mood: mood, frame: frame)
                XCTAssertFalse(shapes.isEmpty, "\(mood) \(frame)")
                for shape in shapes {
                    XCTAssertTrue([nil, .ink].contains(shape.fill) && [nil, .ink].contains(shape.stroke), "\(mood) \(frame)")
                }
            }
        }
        XCTAssertEqual(character.glyphFrameCount(mood: .talking), 2)
        XCTAssertNotEqual(character.glyph(mood: .talking, frame: 0), character.glyph(mood: .talking, frame: 1))
        XCTAssertEqual(character.glyph(mood: .talking, frame: 2), character.glyph(mood: .talking, frame: 0))
        XCTAssertEqual(character.glyphSize, 18)
    }

    func testBubblePointIsTheBubblePivot() {
        XCTAssertEqual(FixtureArt.vector.rig.bubble, BubbleAnchor(pivot: "face", tuck: 0.1))
        XCTAssertEqual(FixtureArt.vector.bubblePoint, Point(50, 40))
    }

    func testTransformsComposeInOrder() {
        let t = Transform.rotation(degrees: 90, around: Point(0, 0)).concatenating(.translation(10, 0))
        let p = t.apply(Point(1, 0))
        XCTAssertEqual(p.x, 10, accuracy: 1e-12)
        XCTAssertEqual(p.y, 1, accuracy: 1e-12)
        let s = Transform.scale(2, around: Point(1, 1)).apply(Point(2, 1))
        XCTAssertEqual(s, Point(3, 1))
        let affine = Transform.translation(3, 4).affine
        XCTAssertEqual([affine.a, affine.b, affine.c, affine.d, affine.tx, affine.ty], [1, 0, 0, 1, 3, 4])
    }
}
