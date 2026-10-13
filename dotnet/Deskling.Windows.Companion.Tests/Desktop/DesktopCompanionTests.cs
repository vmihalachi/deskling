using Deskling.Companion.Figures;
using Deskling.Companion.Shell.Desktop;
using Deskling.Testing.Conformance;
using Deskling.Testing.Fixtures;
using Deskling.Windows.Companion.Desktop;

namespace Deskling.Windows.Companion.Tests.Desktop;

/// <summary>The bubble's place beside the desktop character: the layout's DIPs on a display's pixels.</summary>
public sealed class DesktopCompanionTests
{
    private static readonly DesktopLayout Layout = new(FixtureArt.Characters(ConformanceRoot.Find())[0].Art);
    private static readonly Size Bubble = new(180, 60);

    [Fact]
    public void ItOpensTowardTheMiddleOfTheDisplay()
    {
        var work = new Rect(0, 0, 1920, 1040);

        var right = DesktopCompanion.BubbleFrame(Layout, Bubble, new Rect(1700, 800, 100, 104), work, 1);
        var left = DesktopCompanion.BubbleFrame(Layout, Bubble, new Rect(100, 800, 100, 104), work, 1);

        Assert.True(right.Origin.X + right.Size.Width <= 1700 + 100 * Layout.BubbleTuck + 0.5, "beside the character, to its left");
        Assert.True(left.Origin.X >= 200 - 100 * Layout.BubbleTuck - 0.5, "beside the character, to its right");
    }

    [Fact]
    public void TheTailsTipIsLevelWithTheHead()
    {
        var parent = new Rect(1700, 800, 100, 104);

        var frame = DesktopCompanion.BubbleFrame(Layout, Bubble, parent, new Rect(0, 0, 1920, 1040), 1);

        Assert.Equal(Layout.HeadY(parent), frame.Origin.Y + Layout.BubbleTailY(Bubble.Height), 6);
    }

    [Fact]
    public void AtTwiceTheScaleEverythingIsTwiceAsFar()
    {
        var one = DesktopCompanion.BubbleFrame(Layout, Bubble, new Rect(1700, 800, 100, 104), new Rect(0, 0, 1920, 1040), 1);
        var two = DesktopCompanion.BubbleFrame(Layout, Bubble, new Rect(3400, 1600, 200, 208), new Rect(0, 0, 3840, 2080), 2);

        Assert.Equal(one.Origin.X * 2, two.Origin.X, 6);
        Assert.Equal(one.Origin.Y * 2, two.Origin.Y, 6);
        Assert.Equal((one.Size.Width * 2, one.Size.Height * 2), (two.Size.Width, two.Size.Height));
    }

    [Fact]
    public void ItStaysOnTheDisplay()
    {
        var work = new Rect(0, 0, 1920, 1040);

        var frame = DesktopCompanion.BubbleFrame(Layout, Bubble, new Rect(1810, 0, 100, 104), work, 1);

        Assert.True(frame.Origin.Y >= 0);
        Assert.True(frame.Origin.X >= 0 && frame.Origin.X + frame.Size.Width <= 1920);
    }
}
