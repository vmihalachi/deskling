using Deskling.Companion.Figures;
using Deskling.Companion.Shell.Desktop;

namespace Deskling.Companion.Shell.Tests.Desktop;

/// <summary>
/// Ported from the Swift <c>FloatingPlacementTests</c>, case for case. The Swift cases are in AppKit's y-up coordinates;
/// <see cref="Down(double, double)"/> flips each one onto a 900 px tall primary screen, so the numbers read as they do
/// there: a 1440 × 900 primary screen with a 70 px band at the bottom and 25 px at the top, and a 1920 × 1080 screen to
/// its right.
/// </summary>
public sealed class FloatingPlacementTests
{
    private const double PrimaryHeight = 900;

    /// <summary>A Swift (y-up) rectangle in this file's y-down coordinates.</summary>
    private static Rect Down(double x, double y, double width, double height) => new(x, PrimaryHeight - y - height, width, height);

    /// <summary>A Swift (y-up) point in y-down coordinates.</summary>
    private static Point Down(double x, double y) => new(x, PrimaryHeight - y);

    private static readonly Rect Primary = Down(0, 70, 1440, 805);
    private static readonly Rect Second = Down(1440, -180, 1920, 1055);
    private static readonly Size Size = new(87, 96);

    [Fact]
    public void FrameStandsOnTheAnchor()
    {
        var frame = FloatingPlacement.Frame(Down(100, 200), Size);
        Assert.Equal(Down(56.5, 200, 87, 96), frame);
        Assert.Equal(Down(100, 200), FloatingPlacement.Anchor(frame));
    }

    [Fact]
    public void DefaultAnchorIsBottomRightAboveTheTaskbar()
    {
        var anchor = FloatingPlacement.DefaultAnchor(Size, Primary, 24);
        Assert.Equal(Down(1440 - 24 - 43.5, 94), anchor);
        Assert.Equal(anchor, FloatingPlacement.Clamped(anchor, Size, [Primary]));
    }

    [Fact]
    public void AWindowThatFitsStaysPut()
    {
        var onSecond = Down(2000, 0);
        Assert.Equal(onSecond, FloatingPlacement.Clamped(onSecond, Size, [Primary, Second]));
    }

    [Fact]
    public void AWindowHangingOffAnEdgeIsPulledIn() =>
        Assert.Equal(Down(43.5, 875 - 96), FloatingPlacement.Clamped(Down(10, 850), Size, [Primary]));

    [Fact]
    public void ARemovedDisplaySendsTheWindowToTheNearestScreen() =>
        // Saved on the second screen, which is gone now.
        Assert.Equal(Down(1440 - 43.5, 300), FloatingPlacement.Clamped(Down(2600, 300), Size, [Primary]));

    [Fact]
    public void TheScreenHoldingTheAnchorWinsOverANearerOne() =>
        // Anchor just inside the second screen, window straddling both.
        Assert.Equal(Down(1440 + 43.5, 300), FloatingPlacement.Clamped(Down(1450, 300), Size, [Primary, Second]));

    [Fact]
    public void NoScreensLeavesTheAnchorAlone() =>
        Assert.Equal(new Point(-500, 9000), FloatingPlacement.Clamped(new Point(-500, 9000), Size, []));

    [Fact]
    public void AWindowBiggerThanTheScreenIsCentered()
    {
        var tiny = new Rect(0, 0, 50, 50);
        Assert.Equal(25, FloatingPlacement.Clamped(new Point(300, 300), Size, [tiny]).X, 9);
    }

    [Fact]
    public void TheSideFacesTheMiddleOfTheScreen()
    {
        var nearRight = FloatingPlacement.Frame(Down(1300, 100), Size);
        var nearLeft = FloatingPlacement.Frame(Down(100, 100), Size);
        Assert.Equal(FloatingPlacement.Side.Left, FloatingPlacement.SideBeside(nearRight, Primary));
        Assert.Equal(FloatingPlacement.Side.Right, FloatingPlacement.SideBeside(nearLeft, Primary));
    }

    [Fact]
    public void ABesideFrameSitsNextToTheParentAndOnScreen()
    {
        var parent = FloatingPlacement.Frame(Down(1300, 100), Size);
        var bubble = new Size(200, 60);
        var left = FloatingPlacement.FrameBeside(bubble, parent, FloatingPlacement.Side.Left, PrimaryHeight - 160, -4, Primary);
        Assert.Equal(Down(parent.Origin.X + 4 - 200, 130, 200, 60), left);
        // Too close to the top: pushed down to stay under the top band.
        var high = FloatingPlacement.FrameBeside(bubble, parent, FloatingPlacement.Side.Right, PrimaryHeight - 870, 0, Primary);
        Assert.Equal(Primary.Origin.Y, high.Origin.Y);
        // No room on the right: kept on screen.
        Assert.True(high.Origin.X + high.Size.Width <= Primary.Origin.X + Primary.Size.Width);
    }
}
