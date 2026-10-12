using Deskling.Companion.Figures;

namespace Deskling.Companion.Tests;

/// <summary>
/// Replays every vector in conformance/companion through the <c>Deskling.Testing</c> bodies (the same ones apps run over
/// their own vectors): each must come back without problems.
/// </summary>
public sealed class CompanionVectorsTests
{
    private static readonly IReadOnlyList<RiggedCharacter> Characters = FixtureArt.Characters(Fixture.Root);

    public static IEnumerable<object[]> Pickers => ConformanceRoot.Files("companion/picker");

    public static IEnumerable<object[]> Cadences => ConformanceRoot.Files("companion/cadence");

    public static IEnumerable<object[]> Timelines => ConformanceRoot.Files("companion/quiet");

    public static IEnumerable<object[]> CharacterFiles => ConformanceRoot.Files("companion/characters");

    private static string Name(string path) => Path.GetRelativePath(Fixture.Root, path).Replace('\\', '/');

    private static IReadOnlyList<string> Verify(string relativeOrFull, Func<Stream, string, IReadOnlyList<string>> verify)
    {
        var path = Path.IsPathRooted(relativeOrFull) ? relativeOrFull : Path.Combine(Fixture.Root, relativeOrFull);
        using var stream = File.OpenRead(path);
        return verify(stream, Name(path));
    }

    [Fact]
    public void Phrases() =>
        Assert.Empty(Verify("companion/phrases.json", (s, f) => PhrasesReplay.Verify(s, f, Fixture.Bank, FixtureIds.Flavors)));

    [Fact]
    public void Settings() => Assert.Empty(Verify("companion/settings.json", (s, f) => SettingsReplay.Verify(s, f, Fixture.Defaults)));

    [Fact]
    public void Desktop() => Assert.Empty(Verify("companion/desktop.json", DesktopReplay.Verify));

    [Theory]
    [MemberData(nameof(Pickers))]
    public void Picker(string path) => Assert.Empty(Verify(path, (s, f) => PickerReplay.Verify(s, f, Fixture.Bank)));

    [Theory]
    [MemberData(nameof(Cadences))]
    public void Cadence(string path) => Assert.Empty(Verify(path, CadenceReplay.Verify));

    [Theory]
    [MemberData(nameof(Timelines))]
    public void Quiet(string path) => Assert.Empty(Verify(path, (s, f) => QuietReplay.Verify(s, f, Fixture.Bank, Fixture.Defaults)));

    [Theory]
    [MemberData(nameof(CharacterFiles))]
    public void Character(string path) => Assert.Empty(Verify(path, (s, f) => CharacterReplay.Verify(s, f, Characters)));

    [Fact]
    public void EveryFolderHasVectors()
    {
        Assert.Equal(24, Pickers.Count());
        Assert.Equal(6, Cadences.Count());
        Assert.Equal(19, Timelines.Count());
        Assert.Equal(4, CharacterFiles.Count());
    }
}
