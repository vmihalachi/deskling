import Foundation

/// An affine transform: x' = a x + c y + tx, y' = b x + d y + ty. The rig only rotates, translates and scales
/// uniformly, so a raster part never shears.
public struct Transform: Equatable, Sendable {
    public var a, b, c, d, tx, ty: Double

    public init(a: Double, b: Double, c: Double, d: Double, tx: Double, ty: Double) {
        (self.a, self.b, self.c, self.d, self.tx, self.ty) = (a, b, c, d, tx, ty)
    }

    public static let identity = Transform(a: 1, b: 0, c: 0, d: 1, tx: 0, ty: 0)

    public static func translation(_ dx: Double, _ dy: Double) -> Transform {
        Transform(a: 1, b: 0, c: 0, d: 1, tx: dx, ty: dy)
    }

    /// Clockwise on screen for positive degrees (y points down).
    public static func rotation(degrees: Double, around center: Point) -> Transform {
        let r = degrees * .pi / 180
        let rotate = Transform(a: cos(r), b: sin(r), c: -sin(r), d: cos(r), tx: 0, ty: 0)
        return translation(-center.x, -center.y).concatenating(rotate).concatenating(translation(center.x, center.y))
    }

    public static func scale(_ s: Double, around center: Point) -> Transform {
        translation(-center.x, -center.y).concatenating(Transform(a: s, b: 0, c: 0, d: s, tx: 0, ty: 0))
            .concatenating(translation(center.x, center.y))
    }

    /// The six numbers in the order renderers take them (`CGAffineTransform(a:b:c:d:tx:ty:)`, `Matrix3x2`).
    public var affine: (a: Double, b: Double, c: Double, d: Double, tx: Double, ty: Double) {
        (a, b, c, d, tx, ty)
    }

    /// `self`, then `next`.
    public func concatenating(_ next: Transform) -> Transform {
        Transform(
            a: next.a * a + next.c * b, b: next.b * a + next.d * b,
            c: next.a * c + next.c * d, d: next.b * c + next.d * d,
            tx: next.a * tx + next.c * ty + next.tx, ty: next.b * tx + next.d * ty + next.ty)
    }

    public func apply(_ p: Point) -> Point {
        Point(a * p.x + c * p.y + tx, b * p.x + d * p.y + ty)
    }
}
