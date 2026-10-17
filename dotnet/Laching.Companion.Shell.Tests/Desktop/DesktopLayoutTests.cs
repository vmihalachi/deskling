using Laching.Companion.Figures;
using Laching.Companion.Settings;
using Laching.Companion.Shell.Desktop;

namespace Laching.Companion.Shell.Tests.Desktop;

/// <summary>Ported from the Swift <c>DesktopLayoutTests</c>; heights are y down here (the frame's top plus the drop).</summary>
public sealed class DesktopLayoutTests
{
    /// <summary>sunnysays' sunflower (120 × 132, the bubble at the face, 44 down, tucked 12 %): the numbers its desktop used.</summary>
    private static readonly DesktopLayout Sunflower = new(Fixture.Sunflower());

    [Fact]
    public void TheSunflowersNumbers()
    {
        Assert.Equal(44.0 / 132.0, Sunflower.HeadHeightRatio, 12);
        Assert.Equal(0.12, Sunflower.BubbleTuck);
        Assert.Equal(120.0 / 132.0, Sunflower.Aspect, 12);
        Assert.Equal(4, Sunflower.ShadowHeight);
        Assert.Equal(240, Sunflower.BubbleMaxWidth);
        Assert.Equal(4, Sunflower.BubbleMargin);
    }

    [Fact]
    public void WindowSizesFollowTheArt()
    {
        Assert.Equal(new Size(58, 68), Sunflower.WindowSize(DesktopSize.Small));
        Assert.Equal(new Size(87, 100), Sunflower.WindowSize(DesktopSize.Medium));
        Assert.Equal(new Size(127, 144), Sunflower.WindowSize(DesktopSize.Large));
    }

    [Fact]
    public void TheBubbleMeetsTheHead()
    {
        var frame = new Rect(100, 200, 87, 100);
        Assert.Equal(200 + 96.0 * 44 / 132, Sunflower.HeadY(frame), 9);
        Assert.Equal(-87 * 0.12, Sunflower.BubbleGap(87), 9);
        Assert.Equal(4 + 25, Sunflower.BubbleTailY(80));
        Assert.Equal(4 + 16, Sunflower.BubbleTailY(40));
    }

    [Fact]
    public void AnotherArtGetsItsOwnNumbers()
    {
        var art = Fixture.Vector;
        var layout = new DesktopLayout(art);
        Assert.Equal(art.BubblePoint.Y / art.ViewBox.Height, layout.HeadHeightRatio);
        Assert.Equal(art.Rig.Bubble.Tuck, layout.BubbleTuck);
        Assert.Equal(art.ViewBox.Width / art.ViewBox.Height, layout.Aspect);
        Assert.NotEqual(Sunflower.HeadHeightRatio, layout.HeadHeightRatio);
    }
}
