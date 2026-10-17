import Foundation

/// A point in a character's view box (origin top-left, y down), platform-neutral.
public struct Point: Hashable, Sendable, Codable {
    public var x: Double
    public var y: Double

    public init(_ x: Double, _ y: Double) {
        self.x = x
        self.y = y
    }
}

public struct Size: Hashable, Sendable, Codable {
    public var width: Double
    public var height: Double

    public init(_ width: Double, _ height: Double) {
        self.width = width
        self.height = height
    }
}

/// A rectangle in view-box units: where an image part sits before the rig moves it.
public struct Rect: Hashable, Sendable, Codable {
    public var origin: Point
    public var size: Size

    public init(origin: Point, size: Size) {
        self.origin = origin
        self.size = size
    }

    public init(_ x: Double, _ y: Double, _ width: Double, _ height: Double) {
        self.init(origin: Point(x, y), size: Size(width, height))
    }

    /// Top-left, top-right, bottom-right, bottom-left.
    public var corners: [Point] {
        let (x, y, w, h) = (origin.x, origin.y, size.width, size.height)
        return [Point(x, y), Point(x + w, y), Point(x + w, y + h), Point(x, y + h)]
    }
}

/// A paint a character's art names, such as "petal" or "ink": the app maps each role to a color (its palette). The
/// roles are the app's, listed in its `palette.json`; `ink` is the one every glyph uses.
public struct PaintRole: RawRepresentable, Hashable, Sendable, Codable, ExpressibleByStringLiteral, CustomStringConvertible {
    public let rawValue: String

    public init(rawValue: String) {
        self.rawValue = rawValue
    }

    public init(stringLiteral value: String) {
        self.init(rawValue: value)
    }

    public var description: String { rawValue }

    /// The glyph's only paint: the menu bar template image.
    public static let ink: PaintRole = "ink"
}

/// One step of a path: the compiler turns every curve into cubics, so these four are all a renderer needs.
public enum PathCommand: Equatable, Sendable {
    case move(Point)
    case line(Point)
    case cubic(Point, Point, Point)
    case close

    /// The command's points, control points included.
    public var points: [Point] {
        switch self {
        case .move(let p), .line(let p): [p]
        case .cubic(let c1, let c2, let p): [c1, c2, p]
        case .close: []
        }
    }

    func transformed(by transform: Transform) -> PathCommand {
        switch self {
        case .move(let p): .move(transform.apply(p))
        case .line(let p): .line(transform.apply(p))
        case .cubic(let c1, let c2, let p): .cubic(transform.apply(c1), transform.apply(c2), transform.apply(p))
        case .close: .close
        }
    }
}

/// A filled and/or stroked path with its paint roles.
public struct PathShape: Equatable, Sendable {
    public var path: [PathCommand]
    public var fill: PaintRole?
    public var stroke: PaintRole?
    public var strokeWidth: Double
    public var opacity: Double
    public var evenOdd: Bool

    public init(
        _ path: [PathCommand], fill: PaintRole? = nil, stroke: PaintRole? = nil, strokeWidth: Double = 0,
        opacity: Double = 1, evenOdd: Bool = false
    ) {
        self.path = path
        self.fill = fill
        self.stroke = stroke
        self.strokeWidth = strokeWidth
        self.opacity = opacity
        self.evenOdd = evenOdd
    }

    /// The shape moved by `transform`. Stroke widths stay as they are: the rig only rotates, moves and pulses a few
    /// percent.
    public func transformed(by transform: Transform) -> PathShape {
        var copy = self
        copy.path = path.map { $0.transformed(by: transform) }
        return copy
    }
}

/// What a part draws: vector shapes, or an image from the app's assets placed in `frame` (view-box units). The asset
/// name is `<skin>.<part>`; the app's asset catalog (macOS) or its `scale-200`/`scale-300` PNGs (Windows) hold it.
public enum PartContent: Equatable, Sendable {
    case shapes([PathShape])
    case image(asset: String, frame: Rect)

    /// The shapes of a vector part; nil for an image.
    public var shapes: [PathShape]? {
        if case .shapes(let shapes) = self { return shapes }
        return nil
    }

    /// "shapes" or "image", as the pose vectors spell the kind.
    public var kind: String {
        switch self {
        case .shapes: "shapes"
        case .image: "image"
        }
    }
}
