using Laching.Companion.Figures;
using Laching.Windows.Companion.Views;

namespace Laching.Windows.Companion.Tests.Views;

/// <summary>The speech bubble's outline, as Swift's <c>BubbleShape</c> draws it.</summary>
public sealed class BubbleShapeTests
{
    private static readonly Rect Frame = new(10, 10, 200, 80);

    private static Point[] Points(IReadOnlyList<PathCommand> path) => path.SelectMany(c => c.Points).ToArray();

    [Theory]
    [InlineData(BubbleTail.Leading, 10 + 1 - 9, 10 + 25)]
    [InlineData(BubbleTail.Trailing, 210 - 1 + 9, 10 + 25)]
    [InlineData(BubbleTail.Top, 110, 10 + 1 - 9)]
    [InlineData(BubbleTail.Bottom, 110, 90 - 1 + 9)]
    public void TheTailsTipReachesOutOfItsSide(BubbleTail tail, double x, double y)
    {
        var points = Points(BubbleShape.Outline(Frame, tail));

        Assert.Contains(new Point(x, y), points);
    }

    [Theory]
    [InlineData(BubbleTail.Leading)]
    [InlineData(BubbleTail.Trailing)]
    [InlineData(BubbleTail.Top)]
    [InlineData(BubbleTail.Bottom)]
    public void TheCardSitsOnePointInsideItsFrameAndTheTailOutside(BubbleTail tail)
    {
        var path = BubbleShape.Outline(Frame, tail);
        var points = Points(path);
        var (minX, maxX) = (points.Min(p => p.X), points.Max(p => p.X));
        var (minY, maxY) = (points.Min(p => p.Y), points.Max(p => p.Y));

        Assert.IsType<PathCommand.Move>(path[0]);
        Assert.IsType<PathCommand.Close>(path[^1]);
        Assert.Equal(tail == BubbleTail.Leading ? 2 : 11, minX, 9);
        Assert.Equal(tail == BubbleTail.Trailing ? 218 : 209, maxX, 9);
        Assert.Equal(tail == BubbleTail.Top ? 2 : 11, minY, 9);
        Assert.Equal(tail == BubbleTail.Bottom ? 98 : 89, maxY, 9);
    }

    [Fact]
    public void ASideTailSitsHalfwayDownAShortCardAndItsCornersShrink()
    {
        // 30 tall: the tail centers at 15, and the leading corners keep clear of its 14-point base.
        var points = Points(BubbleShape.Outline(new Rect(0, 0, 100, 30), BubbleTail.Leading));

        Assert.Contains(new Point(-8, 15), points);
        Assert.Contains(new Point(1, 15 - 7), points);
        Assert.Contains(new Point(1, 15 + 7), points);
        // The corner by the tail is 7 at most (from 1 to the tail's base at 8), the far ones the full 14.
        Assert.Contains(new Point(1 + 7, 1), points);
        Assert.Contains(new Point(99 - 14, 1), points);
    }
}
