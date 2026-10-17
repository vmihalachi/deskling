namespace Laching.Testing.Tests.Conformance;

/// <summary>The vectors folder is found from the test binary by its marker file.</summary>
public sealed class ConformanceRootTests
{
    [Fact]
    public void FindsTheVectorsFolderByItsMarker()
    {
        var root = ConformanceRoot.Find();
        Assert.Equal("conformance", Path.GetFileName(root));
        Assert.True(File.Exists(Path.Combine(root, "README.md")));
        Assert.True(Directory.Exists(Path.Combine(root, "companion")));
    }

    [Fact]
    public void AMissingMarkerIsAnError() =>
        Assert.Throws<DirectoryNotFoundException>(() => ConformanceRoot.Find($"no/such/marker-{Guid.NewGuid()}"));

    [Fact]
    public void FilesListsAFolderInOrdinalOrderOnePerRow()
    {
        var files = ConformanceRoot.Files("companion").Select(row => Assert.IsType<string>(Assert.Single(row))).ToList();
        Assert.NotEmpty(files);
        Assert.Equal(files.Order(StringComparer.Ordinal), files);
        Assert.All(files, f => Assert.EndsWith(".json", f));
        Assert.Contains(files, f => Path.GetFileName(f) == "phrases.json");
    }
}
