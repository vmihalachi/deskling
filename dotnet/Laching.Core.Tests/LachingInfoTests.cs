namespace Laching.Core.Tests;

public class LachingInfoTests
{
    [Fact]
    public void VersionLooksLikeSemVer()
    {
        var parts = LachingInfo.Version.Split('.');
        Assert.Equal(3, parts.Length);
        Assert.All(parts, p => Assert.True(int.TryParse(p, out _), LachingInfo.Version));
    }

    [Fact]
    public void ConformanceFolderIsFound()
    {
        Assert.True(File.Exists(Path.Combine(ConformanceRoot.Find(), "README.md")));
    }
}
