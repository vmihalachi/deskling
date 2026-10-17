using Laching.Companion.Figures;
using Laching.Companion.Phrases;
using Laching.Companion.Settings;
using Laching.Companion.Skins;

namespace Laching.Companion.Tests;

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
