using Laching.Windows.Companion.Screenshots;

namespace Laching.Windows.Companion.Tests.Screenshots;

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

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ARunIsHiddenOnlyWhenTheScriptsSayOne(string? value, bool hidden)
    {
        var before = Environment.GetEnvironmentVariable(ScreenshotCapture.HiddenVariable);
        try
        {
            Environment.SetEnvironmentVariable(ScreenshotCapture.HiddenVariable, value);
            Assert.Equal(hidden, ScreenshotCapture.IsHidden);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ScreenshotCapture.HiddenVariable, before);
        }
    }

    [Fact]
    public void EachProcessShootsIntoAFolderOfItsOwnUnlessTold()
    {
        var before = Environment.GetEnvironmentVariable(ScreenshotCapture.DirectoryVariable);
        try
        {
            Environment.SetEnvironmentVariable(ScreenshotCapture.DirectoryVariable, null);
            Assert.Equal(Path.Combine(Path.GetTempPath(), $"screenshots-{Environment.ProcessId}"), ScreenshotCapture.OutputDirectory);
            Environment.SetEnvironmentVariable(ScreenshotCapture.DirectoryVariable, @"C:\shots");
            Assert.Equal(@"C:\shots", ScreenshotCapture.OutputDirectory);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ScreenshotCapture.DirectoryVariable, before);
        }
    }
}
