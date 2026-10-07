namespace Deskling.Core.Tests;

/// <summary>The repo's conformance/ folder, found by walking up from the test binary.</summary>
public static class ConformanceRoot
{
    public static string Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            // The marker is conformance/README.md, not the folder: on Windows (case-insensitive) the test
            // project's own Conformance/ folder would otherwise match.
            var candidate = Path.Combine(dir.FullName, "conformance");
            if (File.Exists(Path.Combine(candidate, "README.md")))
                return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("conformance");
    }

    public static IEnumerable<object[]> Files(string folder, string pattern = "*.json") =>
        Directory
            .GetFiles(Path.Combine(Find(), folder), pattern)
            .Order(StringComparer.Ordinal)
            .Select(f => new object[] { f });
}
