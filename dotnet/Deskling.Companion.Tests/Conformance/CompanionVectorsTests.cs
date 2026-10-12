using Deskling.Companion.Figures;

namespace Deskling.Companion.Tests;

/// <summary>
/// Replays every vector in conformance/companion through the <c>Deskling.Testing</c> bodies (the same ones apps run over
/// their own vectors): each must come back without problems.
/// </summary>
public sealed class CompanionVectorsTests
{
    private static readonly IReadOnlyList<RiggedCharacter> Characters = FixtureArt.Characters(Fixture.Root);

    private static readonly string Root = Path.Combine(Fixture.Root, "companion");

    public static IEnumerable<object[]> Pickers => CompanionReplay.Files("companion/picker");

    public static IEnumerable<object[]> Cadences => CompanionReplay.Files("companion/cadence");

    public static IEnumerable<object[]> Timelines => CompanionReplay.Files("companion/quiet");

    public static IEnumerable<object[]> CharacterFiles => CompanionReplay.Files("companion/characters");

    [Fact]
    public void Phrases() => Assert.Empty(CompanionReplay.Phrases(Root, Fixture.Bank, FixtureIds.Flavors));

    [Fact]
    public void Settings() => Assert.Empty(CompanionReplay.Settings(Root, Fixture.Defaults));

    [Fact]
    public void Desktop() => Assert.Empty(CompanionReplay.Desktop(Root));

    [Theory]
    [MemberData(nameof(Pickers))]
    public void Picker(string path) => Assert.Empty(CompanionReplay.Picker(path, Fixture.Bank));

    [Theory]
    [MemberData(nameof(Cadences))]
    public void Cadence(string path) => Assert.Empty(CompanionReplay.Cadence(path));

    [Theory]
    [MemberData(nameof(Timelines))]
    public void Quiet(string path) => Assert.Empty(CompanionReplay.Quiet(path, Fixture.Bank, Fixture.Defaults));

    [Theory]
    [MemberData(nameof(CharacterFiles))]
    public void Character(string path) => Assert.Empty(CompanionReplay.Character(path, Characters));

    [Fact]
    public void EveryFolderHasVectors()
    {
        Assert.Equal(24, Pickers.Count());
        Assert.Equal(6, Cadences.Count());
        Assert.Equal(19, Timelines.Count());
        Assert.Equal(4, CharacterFiles.Count());
    }
}
