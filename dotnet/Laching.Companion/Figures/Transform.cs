namespace Laching.Companion.Figures;

/// <summary>
/// An affine transform: x' = a x + c y + tx, y' = b x + d y + ty. The rig only rotates, translates and scales
/// uniformly, so a raster part never shears.
/// </summary>
public readonly record struct Transform(double A, double B, double C, double D, double Tx, double Ty)
{
    public static Transform Identity { get; } = new(1, 0, 0, 1, 0, 0);

    public static Transform Translation(double dx, double dy) => new(1, 0, 0, 1, dx, dy);

    /// <summary>Clockwise on screen for positive degrees (y points down).</summary>
    public static Transform Rotation(double degrees, Point center)
    {
        var r = degrees * Math.PI / 180;
        var rotate = new Transform(Math.Cos(r), Math.Sin(r), -Math.Sin(r), Math.Cos(r), 0, 0);
        return Translation(-center.X, -center.Y).Then(rotate).Then(Translation(center.X, center.Y));
    }

    public static Transform Scale(double s, Point center) =>
        Translation(-center.X, -center.Y).Then(new Transform(s, 0, 0, s, 0, 0)).Then(Translation(center.X, center.Y));

    /// <summary>The six numbers in the order renderers take them (<c>Matrix3x2(m11 = a, m12 = b, m21 = c, m22 = d, m31 = tx, m32 = ty)</c>).</summary>
    public (double A, double B, double C, double D, double Tx, double Ty) Affine => (A, B, C, D, Tx, Ty);

    /// <summary>This transform, then <paramref name="next"/> (Swift's <c>concatenating</c>).</summary>
    public Transform Then(Transform next) =>
        new(
            next.A * A + next.C * B,
            next.B * A + next.D * B,
            next.A * C + next.C * D,
            next.B * C + next.D * D,
            next.A * Tx + next.C * Ty + next.Tx,
            next.B * Tx + next.D * Ty + next.Ty
        );

    public Point Apply(Point p) => new(A * p.X + C * p.Y + Tx, B * p.X + D * p.Y + Ty);
}
