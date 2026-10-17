namespace Laching.Companion.Figures;

/// <summary>A point in a character's view box (origin top-left, y down), platform-neutral.</summary>
public readonly record struct Point(double X, double Y);

public readonly record struct Size(double Width, double Height);

/// <summary>A rectangle in view-box units: where an image part sits before the rig moves it.</summary>
public readonly record struct Rect(Point Origin, Size Size)
{
    public Rect(double x, double y, double width, double height)
        : this(new Point(x, y), new Size(width, height)) { }

    /// <summary>Top-left, top-right, bottom-right, bottom-left.</summary>
    public IReadOnlyList<Point> Corners
    {
        get
        {
            var (x, y, w, h) = (Origin.X, Origin.Y, Size.Width, Size.Height);
            return [new(x, y), new(x + w, y), new(x + w, y + h), new(x, y + h)];
        }
    }
}

/// <summary>
/// A paint a character's art names, such as "petal" or "ink": the app maps each role to a color (its palette). The
/// roles are the app's, listed in its palette.json; <see cref="Ink"/> is the one every glyph uses.
/// </summary>
public readonly record struct PaintRole(string RawValue)
{
    /// <summary>The glyph's only paint: the tray or menu bar template image.</summary>
    public static PaintRole Ink { get; } = new("ink");

    public static implicit operator PaintRole(string rawValue) => new(rawValue);

    public override string ToString() => RawValue;
}

/// <summary>One step of a path: the compiler turns every curve into cubics, so these four are all a renderer needs.</summary>
public abstract record PathCommand
{
    private PathCommand() { }

    /// <summary>The command's points, control points included.</summary>
    public abstract IReadOnlyList<Point> Points { get; }

    public abstract PathCommand Transformed(Transform transform);

    public sealed record Move(Point To) : PathCommand
    {
        public override IReadOnlyList<Point> Points => [To];

        public override PathCommand Transformed(Transform transform) => new Move(transform.Apply(To));
    }

    public sealed record Line(Point To) : PathCommand
    {
        public override IReadOnlyList<Point> Points => [To];

        public override PathCommand Transformed(Transform transform) => new Line(transform.Apply(To));
    }

    public sealed record Cubic(Point Control1, Point Control2, Point To) : PathCommand
    {
        public override IReadOnlyList<Point> Points => [Control1, Control2, To];

        public override PathCommand Transformed(Transform transform) =>
            new Cubic(transform.Apply(Control1), transform.Apply(Control2), transform.Apply(To));
    }

    public sealed record Close : PathCommand
    {
        public override IReadOnlyList<Point> Points => [];

        public override PathCommand Transformed(Transform transform) => this;
    }
}

/// <summary>A filled and/or stroked path with its paint roles. Equal when every command and style is.</summary>
public sealed record PathShape(
    IReadOnlyList<PathCommand> Path,
    PaintRole? Fill = null,
    PaintRole? Stroke = null,
    double StrokeWidth = 0,
    double Opacity = 1,
    bool EvenOdd = false
)
{
    /// <summary>
    /// The shape moved by <paramref name="transform"/>. Stroke widths stay as they are: the rig only rotates, moves and
    /// pulses a few percent.
    /// </summary>
    public PathShape Transformed(Transform transform) => this with { Path = Path.Select(c => c.Transformed(transform)).ToArray() };

    public bool Equals(PathShape? other) =>
        other is not null
        && Fill == other.Fill
        && Stroke == other.Stroke
        && StrokeWidth.Equals(other.StrokeWidth)
        && Opacity.Equals(other.Opacity)
        && EvenOdd == other.EvenOdd
        && Path.SequenceEqual(other.Path);

    public override int GetHashCode() => HashCode.Combine(Path.Count, Fill, Stroke, StrokeWidth, Opacity, EvenOdd);
}

/// <summary>
/// What a part draws (Swift's <c>PartContent</c>): vector shapes, or an image from the app's assets placed in
/// <see cref="Image.Frame"/> (view-box units). The asset name is <c>&lt;skin&gt;.&lt;part&gt;</c>; the app's asset
/// catalog (macOS) or its <c>scale-200</c>/<c>scale-300</c> PNGs (Windows) hold it.
/// </summary>
public abstract record PartContent
{
    private PartContent() { }

    /// <summary>"shapes" or "image", as the pose vectors spell the kind.</summary>
    public abstract string Kind { get; }

    /// <summary>A vector part (Swift's <c>.shapes</c>). Equal when every shape is.</summary>
    public sealed record Vector(IReadOnlyList<PathShape> Shapes) : PartContent
    {
        public override string Kind => "shapes";

        public bool Equals(Vector? other) => other is not null && Shapes.SequenceEqual(other.Shapes);

        public override int GetHashCode() => Shapes.Count;
    }

    /// <summary>A raster part (Swift's <c>.image</c>): the asset <see cref="Asset"/> drawn into <see cref="Frame"/>.</summary>
    public sealed record Image(string Asset, Rect Frame) : PartContent
    {
        public override string Kind => "image";
    }
}
