using Deskling.Windows.Companion.Screenshots;

namespace Deskling.Windows.Companion.Tests.Screenshots;

public sealed class ScreenshotCaptureTests
{
    [Theory]
    [InlineData("home", "all", true)]
    [InlineData("home", "", true)]
    [InlineData("support-thanks", "support", true)]
    [InlineData("support-thanks", "home, support-thanks", true)]
    [InlineData("settings", "home,support", false)]
    [InlineData("panel", "Panel", false)]
    public void AShotIsWantedByAPrefixOfItsName(string shot, string mode, bool wanted) =>
        Assert.Equal(wanted, ScreenshotCapture.Wants(shot, mode));
}
