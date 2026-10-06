namespace Deskling.Core.Tests;

public class DesklingInfoTests
{
    [Fact]
    public void VersionLooksLikeSemVer()
    {
        var parts = DesklingInfo.Version.Split('.');
        Assert.Equal(3, parts.Length);
        Assert.All(parts, p => Assert.True(int.TryParse(p, out _), DesklingInfo.Version));
    }

    [Fact]
    public void ConformanceFolderIsFound()
    {
        Assert.True(File.Exists(Path.Combine(ConformanceRoot.Find(), "README.md")));
    }
}
