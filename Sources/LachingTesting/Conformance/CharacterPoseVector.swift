import LachingCompanion
import Foundation

/// `characters/<skin>.json` (the art, `CharacterArtJSON`) and `characters/<skin>-poses.json`: what `RiggedCharacter`
/// makes of the art. The pose for every mood at a few moments, the still poses, every glyph frame and, for a few
/// poses, the resolved `shapes` (when the skin has vector parts) and the placed `layers` (when it has image parts:
/// name, the six affine numbers and the content kind, never pixels). Numbers are rounded to 6 decimals and each path
/// is one string, so the file stays small and identical whichever platform's math library made it. A skin whose talking
/// rig has a `restMouth` also pins the talking pose at every mouth level (`voiced`); the others omit it.
public enum CharacterPoseVector {
    public static let directory = "characters"

    public struct PoseJSON: Codable, Equatable, Sendable {
        public var headAngle: Double
        public var sink: Double
        public var pulse: Double
        public var eyes: String
        public var mouth: String
        public var headExtras: [String]
        public var overlays: [String]
        public var blinking: Bool
    }

    public struct ShapeJSON: Codable, Equatable, Sendable {
        public var d: String
        public var fill: String?
        public var stroke: String?
        public var strokeWidth: Double
        public var opacity: Double
        public var evenOdd: Bool
    }

    public struct LayerJSON: Codable, Equatable, Sendable {
        public var name: String
        /// a, b, c, d, tx, ty.
        public var transform: [Double]
        /// "shapes" or "image".
        public var kind: String
    }

    public struct PoseCase: Codable, Equatable, Sendable {
        public var mood: String
        public var time: Double
        public var pose: PoseJSON
    }

    public struct StillCase: Codable, Equatable, Sendable {
        public var mood: String
        public var pose: PoseJSON
    }

    public struct ShapesCase: Codable, Equatable, Sendable {
        public var mood: String
        public var time: Double
        public var shapes: [ShapeJSON]
    }

    public struct LayersCase: Codable, Equatable, Sendable {
        public var mood: String
        public var time: Double
        public var layers: [LayerJSON]
    }

    /// The talking pose with a recorded clip's mouth level (0–3).
    public struct VoicedCase: Codable, Equatable, Sendable {
        public var level: Int
        public var time: Double
        public var pose: PoseJSON
    }

    public struct GlyphCase: Codable, Equatable, Sendable {
        public var mood: String
        public var frame: Int
        public var shapes: [ShapeJSON]
    }

    public struct Vector: Codable, Equatable, Sendable {
        public var skin: String
        public var poses: [PoseCase]
        public var still: [StillCase]
        public var shapes: [ShapesCase]?
        public var layers: [LayersCase]?
        public var glyph: [GlyphCase]
        public var voiced: [VoicedCase]?
    }

    /// The moments every mood is posed at: rest, a sway, mid-blink and the second talking mouth.
    public static let times: [Double] = [0, 1, 4.15, 0.2]
    /// The moment the voiced poses are taken at (a sway, no blink).
    public static let voicedTime = 0.2
    /// The poses whose shapes or layers are pinned.
    public static let detailCases: [(Mood, Double)] = [
        (.awake, 0), (.talking, 0.2), (.shushed, 0), (.asleep, 1), (.offHours, 0), (.hushedForCall, 4.15),
    ]

    static func round6(_ value: Double) -> Double {
        let rounded = (value * 1_000_000).rounded() / 1_000_000
        return rounded == 0 ? 0 : rounded
    }

    static func format(_ value: Double) -> String {
        let text = String(format: "%.6f", round6(value))
        return text == "-0.000000" ? "0.000000" : text
    }

    static func json(_ pose: Pose) -> PoseJSON {
        PoseJSON(
            headAngle: round6(pose.headAngle), sink: round6(pose.sink), pulse: round6(pose.pulse), eyes: pose.eyes,
            mouth: pose.mouth, headExtras: pose.headExtras, overlays: pose.overlays, blinking: pose.blinking)
    }

    static func json(_ shape: PathShape) -> ShapeJSON {
        let d = shape.path.map { command -> String in
            switch command {
            case .move(let p): "M \(format(p.x)) \(format(p.y))"
            case .line(let p): "L \(format(p.x)) \(format(p.y))"
            case .cubic(let c1, let c2, let p):
                "C \([c1, c2, p].map { "\(format($0.x)) \(format($0.y))" }.joined(separator: " "))"
            case .close: "Z"
            }
        }.joined(separator: " ")
        return ShapeJSON(
            d: d, fill: shape.fill?.rawValue, stroke: shape.stroke?.rawValue, strokeWidth: round6(shape.strokeWidth),
            opacity: round6(shape.opacity), evenOdd: shape.evenOdd)
    }

    static func json(_ layer: PlacedPart) -> LayerJSON {
        let t = layer.transform.affine
        return LayerJSON(name: layer.name, transform: [t.a, t.b, t.c, t.d, t.tx, t.ty].map(round6), kind: layer.content.kind)
    }

    public static func vector(_ character: RiggedCharacter) -> Vector {
        var poses: [PoseCase] = []
        for mood in Mood.allCases {
            for time in times {
                poses.append(PoseCase(mood: mood.rawValue, time: time, pose: json(character.pose(mood: mood, time: time))))
            }
        }
        poses.append(PoseCase(mood: Mood.talking.rawValue, time: 0.05, pose: json(character.pose(mood: .talking, time: 0.05))))
        let still = Mood.allCases.map { StillCase(mood: $0.rawValue, pose: json(character.stillPose(mood: $0))) }
        let parts = character.art.parts.values
        let hasShapes = parts.contains { $0.shapes != nil }
        let hasImages = parts.contains { $0.shapes == nil }
        let shapes = detailCases.map { mood, time in
            ShapesCase(
                mood: mood.rawValue, time: time,
                shapes: character.shapes(for: character.pose(mood: mood, time: time)).map(json))
        }
        let layers = detailCases.map { mood, time in
            LayersCase(
                mood: mood.rawValue, time: time, layers: character.layers(for: character.pose(mood: mood, time: time)).map(json))
        }
        var glyph: [GlyphCase] = []
        for mood in Mood.allCases {
            for frame in 0..<character.glyphFrameCount(mood: mood) {
                glyph.append(GlyphCase(mood: mood.rawValue, frame: frame, shapes: character.glyph(mood: mood, frame: frame).map(json)))
            }
        }
        let voiced = (0...3).map { level in
            VoicedCase(
                level: level, time: voicedTime, pose: json(character.pose(mood: .talking, time: voicedTime, mouthLevel: level)))
        }
        let hasRest = character.art.rig.moods[.talking]?.restMouth != nil
        return Vector(
            skin: character.skin.rawValue, poses: poses, still: still, shapes: hasShapes ? shapes : nil,
            layers: hasImages ? layers : nil, glyph: glyph, voiced: hasRest ? voiced : nil)
    }

    /// The art file and the poses file for `character`.
    public static func files(_ character: RiggedCharacter) throws -> [String: Data] {
        let skin = character.skin.rawValue
        return [
            "\(directory)/\(skin).json": try CharacterArtJSON.encode(character.art),
            "\(directory)/\(skin)-poses.json": try ConformanceSupport.encode(vector(character)),
        ]
    }

    /// Replays a committed art or poses file against `characters` (by skin).
    public static func verify(_ data: Data, file: String, characters: [RiggedCharacter]) throws -> [String] {
        let name = URL(fileURLWithPath: file).deletingPathExtension().lastPathComponent
        if name.hasSuffix("-poses") {
            let committed = try ConformanceSupport.decode(Vector.self, from: data)
            guard let character = characters.first(where: { $0.skin.rawValue == committed.skin }) else {
                return ["\(file): no character for skin \(committed.skin)"]
            }
            let current = vector(character)
            var problems: [String] = []
            for (a, b) in zip(committed.poses, current.poses) where a != b {
                problems.append("\(file): \(a.mood) at \(a.time) differs")
            }
            if committed.poses.count != current.poses.count { problems.append("\(file): pose counts differ") }
            if committed.still != current.still { problems.append("\(file): still poses differ") }
            if committed.shapes != current.shapes { problems.append("\(file): shapes differ") }
            if committed.layers != current.layers { problems.append("\(file): layers differ") }
            if committed.glyph != current.glyph { problems.append("\(file): glyph frames differ") }
            if committed.voiced != current.voiced { problems.append("\(file): voiced poses differ") }
            return problems
        }
        let art = try CharacterArtJSON.decode(data)
        guard let character = characters.first(where: { $0.skin == art.skin }) else {
            return ["\(file): no character for skin \(art.skin)"]
        }
        return art == character.art ? [] : ["\(file): the art differs from \(art.skin)'s"]
    }
}
