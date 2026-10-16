using Deskling.Windows.Companion.Desktop;
using Windows.Graphics;
using FigureRect = Deskling.Companion.Figures.Rect;

namespace Deskling.Windows.Companion.Tests.Desktop;

/// <summary>Where a finger reaches the desktop window while the mouse is away: the drawing, grown, held and cut to the window.</summary>
public sealed class TouchAreaTests
{
    [Fact]
    public void ABoxGrowsByTheMarginAtTheScale()
    {
        var area = new TouchArea();
        var boxes = area.Update(0, [new FigureRect(20, 30, 40, 10)], scale: 2, width: 400, height: 400);
        // (20 - 8) × 2 = 24 to (60 + 8) × 2 = 136; (30 - 8) × 2 = 44 to (40 + 8) × 2 = 96.
        Assert.Equal([new RectInt32(24, 44, 112, 52)], boxes);
    }

    [Fact]
    public void BoxesAreCutToTheWindow()
    {
        var area = new TouchArea();
        var boxes = area.Update(0, [new FigureRect(2, 90, 30, 20), new FigureRect(500, 500, 10, 10)], scale: 1, width: 100, height: 100);
        Assert.Equal([new RectInt32(0, 82, 40, 18)], boxes);
    }

    [Fact]
    public void APartsBoxHoldsWhereItWasForTheHoldTime()
    {
        var area = new TouchArea();
        area.Update(0, [new FigureRect(10, 10, 10, 10)], 1, 200, 200);
        var swayed = area.Update(1, [new FigureRect(30, 10, 10, 10)], 1, 200, 200);
        Assert.Equal([new RectInt32(2, 2, 46, 26)], swayed);
        var later = area.Update(TouchArea.HoldSeconds + 0.5, [new FigureRect(30, 10, 10, 10)], 1, 200, 200);
        Assert.Equal([new RectInt32(22, 2, 26, 26)], later);
    }

    [Fact]
    public void EachPartIsHeldOnItsOwnAndANewSetStartsOver()
    {
        var area = new TouchArea();
        area.Update(0, [new FigureRect(10, 10, 10, 10), new FigureRect(100, 100, 10, 10)], 1, 200, 200);
        var two = area.Update(1, [new FigureRect(12, 10, 10, 10), new FigureRect(100, 102, 10, 10)], 1, 200, 200);
        Assert.Equal([new RectInt32(2, 2, 28, 26), new RectInt32(92, 92, 26, 28)], two);
        var one = area.Update(2, [new FigureRect(50, 50, 10, 10)], 1, 200, 200);
        Assert.Equal([new RectInt32(42, 42, 26, 26)], one);
        area.Clear();
        Assert.Empty(area.Update(3, [], 1, 200, 200));
    }
}
