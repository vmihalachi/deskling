using Laching.Companion.Figures;

namespace Laching.Companion.Shell.Desktop;

/// <summary>
/// Where the desktop character's window and a window beside it (its bubble) go: plain rectangle math in screen
/// coordinates with the origin at the primary screen's top left and y down (Windows' virtual screen, and the form the
/// settings store, so a position saved on one platform means the same on the other). No window state, so it is
/// unit-tested. The Mac's twin works in AppKit's y-up coordinates; the cases are the same, mirrored.
///
/// A floating window stands on its anchor, the middle of its bottom edge, so a size change grows it upward in place.
/// </summary>
public static class FloatingPlacement
{
    /// <summary>Which side of a window another one sits on.</summary>
    public enum Side
    {
        Left,
        Right,
    }

    /// <summary>Half a point of slack, so a frame computed from a screen's own edges counts as inside it.</summary>
    private const double Slack = 0.5;

    /// <summary>The frame of a window of <paramref name="size"/> standing on <paramref name="anchor"/>.</summary>
    public static Rect Frame(Point anchor, Size size) => new(anchor.X - size.Width / 2, anchor.Y - size.Height, size.Width, size.Height);

    /// <summary>The middle of <paramref name="frame"/>'s bottom edge.</summary>
    public static Point Anchor(Rect frame) => new(MidX(frame), MaxY(frame));

    /// <summary>Bottom right of <paramref name="workArea"/> (above the taskbar when it sits at the bottom), <paramref name="inset"/> in from both edges.</summary>
    public static Point DefaultAnchor(Size size, Rect workArea, double inset) =>
        new(MaxX(workArea) - inset - size.Width / 2, MaxY(workArea) - inset);

    /// <summary>
    /// <paramref name="anchor"/>, moved as little as needed for a window of <paramref name="size"/> to sit wholly inside
    /// one of <paramref name="workAreas"/>: the one holding the anchor, or else the nearest. Unchanged when the window
    /// already fits on a screen, or when there are no screens.
    /// </summary>
    public static Point Clamped(Point anchor, Size size, IReadOnlyList<Rect> workAreas)
    {
        if (workAreas.Count == 0)
            return anchor;
        var window = Frame(anchor, size);
        if (workAreas.Any(screen => Holds(screen, window)))
            return anchor;
        var screen = workAreas.FirstOrDefault(s => Holds(s, anchor), workAreas.MinBy(s => Distance(anchor, s)));
        return new Point(
            Clamp(anchor.X, screen.Origin.X + size.Width / 2, MaxX(screen) - size.Width / 2),
            Clamp(anchor.Y, screen.Origin.Y + size.Height, MaxY(screen)));
    }

    /// <summary>The side of <paramref name="parent"/> with more room on its screen, so a window beside it opens toward the middle.</summary>
    public static Side SideBeside(Rect parent, Rect workArea) => MidX(parent) > MidX(workArea) ? Side.Left : Side.Right;

    /// <summary>
    /// A window of <paramref name="size"/> on <paramref name="side"/> of <paramref name="parent"/>, <paramref name="gap"/>
    /// away (negative tucks it in), its middle at <paramref name="centerY"/>, kept inside <paramref name="workArea"/>.
    /// </summary>
    public static Rect FrameBeside(Size size, Rect parent, Side side, double centerY, double gap, Rect workArea)
    {
        var x = side == Side.Left ? parent.Origin.X - gap - size.Width : MaxX(parent) + gap;
        return new Rect(
            Clamp(x, workArea.Origin.X, MaxX(workArea) - size.Width),
            Clamp(centerY - size.Height / 2, workArea.Origin.Y, MaxY(workArea) - size.Height),
            size.Width,
            size.Height);
    }

    private static double MaxX(Rect r) => r.Origin.X + r.Size.Width;

    private static double MaxY(Rect r) => r.Origin.Y + r.Size.Height;

    private static double MidX(Rect r) => r.Origin.X + r.Size.Width / 2;

    private static bool Holds(Rect screen, Rect window) =>
        window.Origin.X >= screen.Origin.X - Slack && MaxX(window) <= MaxX(screen) + Slack
        && window.Origin.Y >= screen.Origin.Y - Slack && MaxY(window) <= MaxY(screen) + Slack;

    private static bool Holds(Rect screen, Point point) =>
        point.X >= screen.Origin.X && point.X <= MaxX(screen) && point.Y >= screen.Origin.Y && point.Y <= MaxY(screen);

    private static double Distance(Point point, Rect rect)
    {
        var dx = Math.Max(Math.Max(rect.Origin.X - point.X, 0), point.X - MaxX(rect));
        var dy = Math.Max(Math.Max(rect.Origin.Y - point.Y, 0), point.Y - MaxY(rect));
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary><paramref name="value"/> within <paramref name="low"/>…<paramref name="high"/>; the middle when the range is empty (a window bigger than the screen).</summary>
    private static double Clamp(double value, double low, double high) => low <= high ? Math.Min(Math.Max(value, low), high) : (low + high) / 2;
}
