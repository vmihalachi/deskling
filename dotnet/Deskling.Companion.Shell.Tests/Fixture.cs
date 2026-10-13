using Deskling.Companion.Figures;
using Deskling.Companion.Phrases;
using Deskling.Companion.Settings;

namespace Deskling.Companion.Shell.Tests;

/// <summary>The fixtures the shell's tests run over (Swift's <c>PhraseBank.fixture</c>, <c>CompanionDefaults.fixture</c>), loaded once.</summary>
internal static class Fixture
{
    public static readonly string Root = ConformanceRoot.Find();

    public static readonly PhraseBank Bank = FixtureBank.Load(Root);

    public static CompanionDefaults Defaults => FixtureDefaults.Value;

    public static readonly CharacterArt Vector = FixtureArt.Load(Root, "fixture-vector");

    /// <summary>sunnysays' sunflower, shared with the Swift tests.</summary>
    public static CharacterArt Sunflower()
    {
        using var json = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Resources", "sunflower.json"));
        return CharacterArt.Parse(json);
    }
}
