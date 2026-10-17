import Foundation

/// The `characters/<skin>.json` format: what `tools/character.py --out` writes and the C# core loads. `encode` writes
/// the compiler's exact bytes (sorted keys, two-space indent, numbers to 3 decimals, paths and scalar lists on one
/// line), so a committed file can be checked against the Swift art; `decode` reads it back and refuses art whose rig
/// names a part, pivot, glyph frame or mood that isn't there. Pure; never touches the file system.
public enum CharacterArtJSON {
    public struct Failure: Error, CustomStringConvertible, Sendable {
        public let description: String

        init(_ description: String) {
            self.description = description
        }
    }

    // MARK: Encode

    public static func encode(_ art: CharacterArt) throws -> Data {
        var text = ""
        try render(node(art), indent: 0, into: &text)
        text += "\n"
        return Data(text.utf8)
    }

    private indirect enum Node {
        case string(String)
        case number(Double)
        case bool(Bool)
        case null
        case array([Node])
        case object([String: Node])
    }

    private static func node(_ art: CharacterArt) -> Node {
        .object([
            "skin": .string(art.skin.rawValue),
            "viewBox": .array([.number(art.viewBox.width), .number(art.viewBox.height)]),
            "pivots": .object(art.pivots.mapValues(node)),
            "parts": .object(art.parts.mapValues(node)),
            "rig": node(art.rig),
            "glyph": .object([
                "size": .number(art.glyphSize),
                "frames": .object(art.glyphFrames.mapValues { .array($0.map(node)) }),
            ]),
        ])
    }

    private static func node(_ point: Point) -> Node {
        .array([.number(point.x), .number(point.y)])
    }

    private static func node(_ content: PartContent) -> Node {
        switch content {
        case .shapes(let shapes):
            return .array(shapes.map(node))
        case .image(let asset, let frame):
            let numbers = [frame.origin.x, frame.origin.y, frame.size.width, frame.size.height]
            return .object(["image": .string(asset), "frame": .array(numbers.map(Node.number))])
        }
    }

    private static func node(_ shape: PathShape) -> Node {
        let d: [Node] = shape.path.map { command in
            switch command {
            case .move(let p): .array([.string("M"), .number(p.x), .number(p.y)])
            case .line(let p): .array([.string("L"), .number(p.x), .number(p.y)])
            case .cubic(let c1, let c2, let p):
                .array([.string("C")] + [c1, c2, p].flatMap { [Node.number($0.x), .number($0.y)] })
            case .close: .array([.string("Z")])
            }
        }
        return .object([
            "d": .array(d),
            "fill": shape.fill.map { .string($0.rawValue) } ?? .null,
            "stroke": shape.stroke.map { .string($0.rawValue) } ?? .null,
            "strokeWidth": .number(shape.strokeWidth),
            "opacity": .number(shape.opacity),
            "evenOdd": .bool(shape.evenOdd),
        ])
    }

    private static func node(_ rig: Rig) -> Node {
        func group(_ g: RigGroup) -> Node {
            .object(["pivot": .string(g.pivot), "parts": strings(g.parts), "follow": .number(g.follow)])
        }
        var moods: [String: Node] = [:]
        for (mood, m) in rig.moods {
            var fields: [String: Node] = [
                "eyes": .string(m.eyes), "mouth": strings(m.mouth), "mouthRate": .number(m.mouthRate),
                "headExtras": strings(m.headExtras), "overlays": strings(m.overlays), "blink": .bool(m.blink),
                "droop": .number(m.droop), "sink": .number(m.sink), "sway": .number(m.sway),
                "swayPeriod": .number(m.swayPeriod), "pulse": .number(m.pulse),
            ]
            if let rest = m.restMouth { fields["restMouth"] = .string(rest) }  // omitted at its default: older art stays byte-identical
            moods[mood.rawValue] = .object(fields)
        }
        var glyph: [String: Node] = [:]
        for (mood, frames) in rig.glyph { glyph[mood.rawValue] = strings(frames) }
        return .object([
            "period": .number(rig.period), "stillTime": .number(rig.stillTime), "always": strings(rig.always),
            "head": group(rig.head), "stem": group(rig.stem), "pulse": group(rig.pulse),
            "blink": .object([
                "duration": .number(rig.blink.duration), "intervals": .array(rig.blink.intervals.map(Node.number)),
                "closedEyes": .string(rig.blink.closedEyes),
            ]),
            "moods": .object(moods), "glyph": .object(glyph),
            "bubble": .object(["pivot": .string(rig.bubble.pivot), "tuck": .number(rig.bubble.tuck)]),
        ])
    }

    private static func strings(_ values: [String]) -> Node {
        .array(values.map(Node.string))
    }

    /// The compiler's layout: objects one key per line, sorted; arrays of scalars (or of such arrays) on one line.
    private static func render(_ node: Node, indent: Int, into text: inout String) throws {
        let pad = String(repeating: "  ", count: indent + 1)
        switch node {
        case .object(let members):
            guard !members.isEmpty else {
                text += "{}"
                return
            }
            text += "{\n"
            for (i, key) in members.keys.sorted().enumerated() {
                text += pad + quoted(key) + ": "
                try render(members[key]!, indent: indent + 1, into: &text)
                text += i == members.count - 1 ? "\n" : ",\n"
            }
            text += String(repeating: "  ", count: indent) + "}"
        case .array(let items) where !isInline(node):
            text += "[\n"
            for (i, item) in items.enumerated() {
                text += pad
                try render(item, indent: indent + 1, into: &text)
                text += i == items.count - 1 ? "\n" : ",\n"
            }
            text += String(repeating: "  ", count: indent) + "]"
        default:
            text += try inline(node)
        }
    }

    private static func isInline(_ node: Node) -> Bool {
        guard case .array(let items) = node else { return false }
        return items.allSatisfy { item in
            switch item {
            case .object: false
            case .array: isInline(item)
            default: true
            }
        }
    }

    private static func inline(_ node: Node) throws -> String {
        switch node {
        case .string(let s): return quoted(s)
        case .number(let n): return try number(n)
        case .bool(let b): return b ? "true" : "false"
        case .null: return "null"
        case .array(let items): return "[" + (try items.map(inline)).joined(separator: ", ") + "]"
        case .object: throw Failure("an object can't be written inline")
        }
    }

    /// To 3 decimals; whole numbers without a fraction, and never "-0".
    private static func number(_ value: Double) throws -> String {
        guard value.isFinite else { throw Failure("\(value) can't be written as JSON") }
        let rounded = (value * 1000).rounded() / 1000
        if rounded == 0 { return "0" }
        if rounded == rounded.rounded(), abs(rounded) < 1e15 { return String(Int(rounded)) }
        return "\(rounded)"
    }

    /// A JSON string as Python's `json.dumps(…, ensure_ascii=False)` writes it.
    private static func quoted(_ s: String) -> String {
        var out = "\""
        for scalar in s.unicodeScalars {
            switch scalar {
            case "\"": out += "\\\""
            case "\\": out += "\\\\"
            case "\n": out += "\\n"
            case "\r": out += "\\r"
            case "\t": out += "\\t"
            case "\u{08}": out += "\\b"
            case "\u{0C}": out += "\\f"
            case _ where scalar.value < 0x20: out += String(format: "\\u%04x", scalar.value)
            default: out.unicodeScalars.append(scalar)
            }
        }
        return out + "\""
    }

    // MARK: Decode

    public static func decode(_ data: Data) throws -> CharacterArt {
        let file: ArtFile
        do {
            file = try JSONDecoder().decode(ArtFile.self, from: data)
        } catch let error as Failure {
            throw error
        } catch {
            throw Failure("not character art JSON: \(error)")
        }
        let art = try file.art()
        try validate(art)
        return art
    }

    private static func point(_ numbers: [Double], _ what: String) throws -> Point {
        guard numbers.count == 2 else { throw Failure("\(what) needs 2 numbers") }
        return Point(numbers[0], numbers[1])
    }

    private static func mood(_ name: String) throws -> Mood {
        guard let mood = Mood(rawValue: name) else { throw Failure("unknown mood '\(name)'") }
        return mood
    }

    private struct ArtFile: Decodable {
        var skin: String
        var viewBox: [Double]
        var pivots: [String: [Double]]
        var parts: [String: PartFile]
        var rig: RigFile
        var glyph: GlyphFile

        func art() throws -> CharacterArt {
            guard viewBox.count == 2 else { throw Failure("viewBox needs 2 numbers") }
            var pivots: [String: Point] = [:]
            for (name, numbers) in self.pivots { pivots[name] = try point(numbers, "pivot '\(name)'") }
            var parts: [String: PartContent] = [:]
            for (name, part) in self.parts { parts[name] = try part.content(name) }
            return CharacterArt(
                skin: SkinID(rawValue: skin), viewBox: Size(viewBox[0], viewBox[1]), pivots: pivots, parts: parts,
                rig: try rig.rig(), glyphSize: glyph.size, glyphFrames: glyph.frames.mapValues { $0.map(\.shape) })
        }
    }

    private enum PartFile: Decodable {
        case shapes([ShapeFile])
        case image(ImageFile)

        init(from decoder: Decoder) throws {
            let container = try decoder.singleValueContainer()
            if let shapes = try? container.decode([ShapeFile].self) {
                self = .shapes(shapes)
            } else {
                self = .image(try container.decode(ImageFile.self))
            }
        }

        func content(_ name: String) throws -> PartContent {
            switch self {
            case .shapes(let shapes): return .shapes(shapes.map(\.shape))
            case .image(let image):
                guard image.frame.count == 4 else { throw Failure("part '\(name)': frame needs 4 numbers") }
                let f = image.frame
                return .image(asset: image.image, frame: Rect(f[0], f[1], f[2], f[3]))
            }
        }
    }

    private struct ImageFile: Decodable {
        var image: String
        var frame: [Double]
    }

    private struct ShapeFile: Decodable {
        var d: [CommandFile]
        var fill: String?
        var stroke: String?
        var strokeWidth: Double
        var opacity: Double
        var evenOdd: Bool

        var shape: PathShape {
            PathShape(
                d.map(\.command), fill: fill.map(PaintRole.init(rawValue:)), stroke: stroke.map(PaintRole.init(rawValue:)),
                strokeWidth: strokeWidth, opacity: opacity, evenOdd: evenOdd)
        }
    }

    private struct CommandFile: Decodable {
        var command: PathCommand

        init(from decoder: Decoder) throws {
            var container = try decoder.unkeyedContainer()
            let op = try container.decode(String.self)
            var numbers: [Double] = []
            while !container.isAtEnd { numbers.append(try container.decode(Double.self)) }
            func points(_ count: Int) throws -> [Point] {
                guard numbers.count == 2 * count else { throw Failure("path command \(op) needs \(2 * count) numbers") }
                return (0..<count).map { Point(numbers[2 * $0], numbers[2 * $0 + 1]) }
            }
            switch op {
            case "M": command = .move(try points(1)[0])
            case "L": command = .line(try points(1)[0])
            case "C":
                let p = try points(3)
                command = .cubic(p[0], p[1], p[2])
            case "Z":
                _ = try points(0)
                command = .close
            default: throw Failure("unknown path command '\(op)'")
            }
        }
    }

    private struct RigFile: Decodable {
        var period: Double
        var stillTime: Double
        var always: [String]
        var head: RigGroup
        var stem: RigGroup
        var pulse: RigGroup
        var blink: Blink
        var moods: [String: MoodRig]
        var glyph: [String: [String]]
        var bubble: BubbleAnchor

        func rig() throws -> Rig {
            var moods: [Mood: MoodRig] = [:]
            for (name, entry) in self.moods { moods[try mood(name)] = entry }
            var glyph: [Mood: [String]] = [:]
            for (name, frames) in self.glyph { glyph[try mood(name)] = frames }
            return Rig(
                period: period, stillTime: stillTime, always: always, head: head, stem: stem, pulse: pulse, blink: blink, moods: moods,
                glyph: glyph, bubble: bubble)
        }
    }

    private struct GlyphFile: Decodable {
        var size: Double
        var frames: [String: [ShapeFile]]
    }

    /// Everything the rig names must exist, so `RiggedCharacter` never meets a missing part.
    private static func validate(_ art: CharacterArt) throws {
        let rig = art.rig
        func part(_ name: String, _ where: String) throws {
            guard art.parts[name] != nil else { throw Failure("\(`where`) names unknown part '\(name)'") }
        }
        func pivot(_ name: String, _ where: String) throws {
            guard art.pivots[name] != nil else { throw Failure("\(`where`) names unknown pivot '\(name)'") }
        }
        for name in rig.always { try part(name, "always") }
        for (key, group) in [("head", rig.head), ("stem", rig.stem), ("pulse", rig.pulse)] {
            try pivot(group.pivot, key)
            for name in group.parts { try part(name, key) }
        }
        try part(rig.blink.closedEyes, "blink")
        try pivot(rig.bubble.pivot, "bubble")
        for mood in Mood.allCases {
            guard let m = rig.moods[mood] else { throw Failure("mood '\(mood.rawValue)' is missing") }
            let label = "mood '\(mood.rawValue)'"
            guard !m.mouth.isEmpty else { throw Failure("\(label) needs at least one mouth") }
            guard m.mouth.count == 1 || m.mouthRate > 0 else { throw Failure("\(label) alternates mouths without a mouthRate") }
            for name in [m.eyes] + m.mouth + m.headExtras + m.overlays + (m.restMouth.map { [$0] } ?? []) { try part(name, label) }
            let frames = rig.glyph[mood] ?? []
            guard !frames.isEmpty else { throw Failure("\(label) has no glyph frames") }
            for frame in frames where art.glyphFrames[frame] == nil {
                throw Failure("\(label) names unknown glyph frame '\(frame)'")
            }
        }
    }
}
