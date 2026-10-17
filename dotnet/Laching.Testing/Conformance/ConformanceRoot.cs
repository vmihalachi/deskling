namespace Laching.Testing.Conformance;

/// <summary>A repo's vectors folder, found by walking up from the test binary.</summary>
public static class ConformanceRoot
{
    /// <summary>
    /// The folder holding <paramref name="markerFile"/> (a path such as "conformance/README.md"), searched for from the
    /// test binary's folder up to the root. A marker file rather than a folder name, because on a case-insensitive file
    /// system a test project's own <c>Conformance/</c> folder would match.
    /// </summary>
    public static string Find(string markerFile = "conformance/README.md")
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var marker = Path.Combine(dir.FullName, markerFile);
            if (File.Exists(marker))
                return Path.GetDirectoryName(marker)!;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException($"no ancestor of {AppContext.BaseDirectory} has {markerFile}");
    }

    /// <summary>
    /// The files matching <paramref name="pattern"/> in <paramref name="folder"/> (relative to the vectors folder), in
    /// ordinal order, one per row: the shape xUnit's <c>MemberData</c> takes.
    /// </summary>
    public static IEnumerable<object[]> Files(string folder, string pattern = "*.json", string markerFile = "conformance/README.md") =>
        Directory
            .GetFiles(Path.Combine(Find(markerFile), folder), pattern)
            .Order(StringComparer.Ordinal)
            .Select(f => new object[] { f });
}
