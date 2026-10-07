namespace Deskling.Core.Tests;

/// <summary>The repo's conformance/ folder, found by walking up from the test binary.</summary>
public static class ConformanceRoot
{
    public static string Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "conformance");
            if (Directory.Exists(candidate))
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
