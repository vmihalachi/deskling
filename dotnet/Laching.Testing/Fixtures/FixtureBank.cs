using Laching.Companion.Phrases;

namespace Laching.Testing.Fixtures;

/// <summary>
/// Swift's <c>PhraseBank.fixture</c>, loaded from the <c>companion/phrases.json</c> it generates: a small bank with one
/// of everything the picker tells apart, and placeholder texts (three general lines of flavor a with weights 1, 3 and
/// 5, one each of b and c, a line per daypart, weekday and event tag, a tune, a line only the raster skin says, an
/// Italian-only line, a two-part joke for the button and an Italian-only joke).
/// </summary>
public static class FixtureBank
{
    /// <summary>The bank, from <paramref name="root"/> (the vectors folder, <c>ConformanceRoot.Find()</c>).</summary>
    public static PhraseBank Load(string root)
    {
        using var stream = File.OpenRead(Path.Combine(root, "companion", "phrases.json"));
        return PhraseBank.Parse(stream);
    }
}
