using Laching.Companion.Figures;
using Laching.Companion.Shell.Desktop;

namespace Laching.Windows.Companion.Views;

/// <summary>Which side of the speech bubble its tail comes out of, toward the character.</summary>
public enum BubbleTail
{
    Leading,
    Trailing,
    Top,
    Bottom,
}

/// <summary>
/// The bubble's outline (Swift's <c>BubbleShape</c>): a rounded card, drawn 1 point in so a 2-point stroke stays inside
/// its frame, with a triangular tail out of one side, all one path. A side tail is centered 25 points below the top, or
/// halfway down a shorter card; a top or bottom tail is centered. Corners next to the tail shrink to make room. The
/// numbers are <see cref="BubbleMetrics"/>'. Pure: corners are quarter circles as cubics.
/// </summary>
public static class BubbleShape
{
    /// <summary>A quarter circle's control points sit this far along its tangents, times the radius.</summary>
    private const double Kappa = 0.5522847498;

    /// <summary>The outline of a card filling <paramref name="frame"/>, its tail reaching <see cref="BubbleMetrics.TailLength"/> outside it.</summary>
    public static IReadOnlyList<PathCommand> Outline(Rect frame, BubbleTail tail)
    {
        var (minX, minY) = (frame.Origin.X + 1, frame.Origin.Y + 1);
        var (maxX, maxY) = (frame.Origin.X + frame.Size.Width - 1, frame.Origin.Y + frame.Size.Height - 1);
        var midX = (minX + maxX) / 2;
        var half = BubbleMetrics.TailBase / 2;
        var length = BubbleMetrics.TailLength - 1;
        var tailY = frame.Origin.Y + BubbleMetrics.TailY(frame.Size.Height);
        double Corner(double room) => Math.Max(0, Math.Min(BubbleMetrics.Radius, room));
        var (topLeft, topRight, bottomRight, bottomLeft) = (BubbleMetrics.Radius, BubbleMetrics.Radius, BubbleMetrics.Radius, BubbleMetrics.Radius);
        switch (tail)
        {
            case BubbleTail.Leading:
                topLeft = Corner(tailY - half - minY);
                bottomLeft = Corner(maxY - tailY - half);
                break;
            case BubbleTail.Trailing:
                topRight = Corner(tailY - half - minY);
                bottomRight = Corner(maxY - tailY - half);
                break;
            case BubbleTail.Top:
                topLeft = topRight = Corner(midX - half - minX);
                break;
            case BubbleTail.Bottom:
                bottomLeft = bottomRight = Corner(midX - half - minX);
                break;
        }

        var path = new List<PathCommand>();
        var current = new Point(minX + topLeft, minY);
        path.Add(new PathCommand.Move(current));
        void Line(double x, double y)
        {
            current = new Point(x, y);
            path.Add(new PathCommand.Line(current));
        }
        // Swift's addArc(tangent1End:tangent2End:radius:) at a right-angled corner: along the edge to the arc, then round.
        void Arc(Point corner, Point next, double radius)
        {
            var d1 = Unit(current, corner);
            var d2 = Unit(corner, next);
            var start = new Point(corner.X - d1.X * radius, corner.Y - d1.Y * radius);
            var end = new Point(corner.X + d2.X * radius, corner.Y + d2.Y * radius);
            Line(start.X, start.Y);
            if (radius > 0)
            {
                var c1 = new Point(start.X + d1.X * radius * Kappa, start.Y + d1.Y * radius * Kappa);
                var c2 = new Point(end.X - d2.X * radius * Kappa, end.Y - d2.Y * radius * Kappa);
                path.Add(new PathCommand.Cubic(c1, c2, end));
                current = end;
            }
        }

        if (tail == BubbleTail.Top)
        {
            Line(midX - half, minY);
            Line(midX, minY - length);
            Line(midX + half, minY);
        }
        Arc(new Point(maxX, minY), new Point(maxX, maxY), topRight);
        if (tail == BubbleTail.Trailing)
        {
            Line(maxX, tailY - half);
            Line(maxX + length, tailY);
            Line(maxX, tailY + half);
        }
        Arc(new Point(maxX, maxY), new Point(minX, maxY), bottomRight);
        if (tail == BubbleTail.Bottom)
        {
            Line(midX + half, maxY);
            Line(midX, maxY + length);
            Line(midX - half, maxY);
        }
        Arc(new Point(minX, maxY), new Point(minX, minY), bottomLeft);
        if (tail == BubbleTail.Leading)
        {
            Line(minX, tailY + half);
            Line(minX - length, tailY);
            Line(minX, tailY - half);
        }
        Arc(new Point(minX, minY), new Point(maxX, minY), topLeft);
        path.Add(new PathCommand.Close());
        return path;
    }

    private static Point Unit(Point from, Point to)
    {
        var (dx, dy) = (to.X - from.X, to.Y - from.Y);
        var length = Math.Sqrt(dx * dx + dy * dy);
        return length == 0 ? new Point(0, 0) : new Point(dx / length, dy / length);
    }
}
