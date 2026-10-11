using Deskling.Companion.Figures;
using Deskling.Companion.Phrases;
using Deskling.Companion.Settings;
using Deskling.Companion.Skins;

namespace Deskling.Companion.Tests;

/// <summary>The fixtures every test runs over (Swift's <c>PhraseBank.fixture</c>, <c>CompanionDefaults.fixture</c>, <c>FixtureArt</c>), loaded once.</summary>
internal static class Fixture
{
    public static readonly string Root = ConformanceRoot.Find();

    public static readonly PhraseBank Bank = FixtureBank.Load(Root);

    public static CompanionDefaults Defaults => FixtureDefaults.Value;

    public static readonly CharacterArt Vector = FixtureArt.Load(Root, "fixture-vector");

    public static readonly CharacterArt Raster = FixtureArt.Load(Root, "fixture-raster");

    public static readonly SkinCatalog Catalog = FixtureArt.Catalog(Root);
}
