using Deskling.Windows.Companion.Shell;
using Windows.Graphics;

namespace Deskling.Windows.Companion.Tests.Shell;

public sealed class TrayPlacementTests
{
    /// <summary>A 1920×1080 display.</summary>
    private static readonly RectInt32 Bounds = new(0, 0, 1920, 1080);

    private static readonly SizeInt32 Size = new(400, 300);

    [Fact]
    public void ByABottomTaskbarTheCornerIsBottomRight()
    {
        var area = new RectInt32(0, 0, 1920, 1032);

        Assert.Equal(new PointInt32(1920 - 400 - 12, 1032 - 300 - 12), TrayPlacement.Corner(area, Bounds, Size, 12));
    }

    [Fact]
    public void ByATopOrLeftTaskbarTheCornerMovesThere()
    {
        Assert.Equal(new PointInt32(1508, 60), TrayPlacement.Corner(new RectInt32(0, 48, 1920, 1032), Bounds, Size, 12));
        Assert.Equal(new PointInt32(60, 768), TrayPlacement.Corner(new RectInt32(48, 0, 1872, 1080), Bounds, Size, 12));
    }

    [Fact]
    public void AnAutoHidingTaskbarCountsAsTheBottom()
    {
        Assert.Equal(new PointInt32(1508, 768), TrayPlacement.Corner(Bounds, Bounds, Size, 12));
    }

    [Fact]
    public void AFlyoutSitsAboveABottomTaskbarCenteredOnTheIcon()
    {
        var area = new RectInt32(0, 0, 1920, 1032);
        var icon = new RectInt32(1200, 1040, 32, 32);

        Assert.Equal(new PointInt32(1216 - 200, 1032 - 300 - 12), TrayPlacement.NextTo(icon, area, Bounds, Size, 12));
    }

    [Fact]
    public void AFlyoutStaysInsideTheWorkArea()
    {
        var area = new RectInt32(0, 0, 1920, 1032);
        var icon = new RectInt32(1880, 1040, 32, 32);

        Assert.Equal(new PointInt32(1920 - 400 - 12, 720), TrayPlacement.NextTo(icon, area, Bounds, Size, 12));
    }

    [Fact]
    public void ByASideTaskbarAFlyoutSitsBesideIt()
    {
        var icon = new RectInt32(8, 900, 32, 32);

        Assert.Equal(new PointInt32(60, 916 - 150), TrayPlacement.NextTo(icon, new RectInt32(48, 0, 1872, 1080), Bounds, Size, 12));
        Assert.Equal(
            new PointInt32(1872 - 400 - 12, 766),
            TrayPlacement.NextTo(new RectInt32(1880, 900, 32, 32), new RectInt32(0, 0, 1872, 1080), Bounds, Size, 12)
        );
    }

    [Fact]
    public void ByATopTaskbarAFlyoutHangsBelowIt()
    {
        var icon = new RectInt32(1200, 8, 32, 32);

        Assert.Equal(new PointInt32(1016, 60), TrayPlacement.NextTo(icon, new RectInt32(0, 48, 1920, 1032), Bounds, Size, 12));
    }
}
