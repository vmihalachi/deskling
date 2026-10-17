using Laching.Companion.Figures;
using Laching.Companion.Skins;

namespace Laching.Testing.Fixtures;

/// <summary>
/// The fixture skins' art (Swift's <c>CharacterArt.fixtureVector</c> and <c>.fixtureRaster</c>), loaded from
/// <c>companion/characters/</c>: one vector skin (the default) and one raster skin.
/// </summary>
public static class FixtureArt
{
    /// <summary>The product id that unlocks the raster skin in <see cref="Catalog"/>.</summary>
    public const string RasterProduct = "fixture.skin.raster";

    /// <summary>
    /// The art of skin <paramref name="name"/> ("fixture-vector" or "fixture-raster") from <paramref name="root"/> (the
    /// vectors folder, <c>ConformanceRoot.Find()</c>).
    /// </summary>
    public static CharacterArt Load(string root, string name)
    {
        using var stream = File.OpenRead(Path.Combine(root, "companion", "characters", name + ".json"));
        return CharacterArt.Parse(stream);
    }

    /// <summary>Both fixture characters, the default first.</summary>
    public static IReadOnlyList<RiggedCharacter> Characters(string root) =>
        FixtureIds.Skins.Select(skin => new RiggedCharacter(Load(root, skin.RawValue))).ToArray();

    /// <summary>
    /// Swift's <c>SkinCatalog.fixture</c>: the vector skin (free, the default) and the raster skin (unlocked by
    /// <see cref="RasterProduct"/>).
    /// </summary>
    public static SkinCatalog Catalog(string root) =>
        new([new SkinEntry(Load(root, FixtureIds.VectorSkin.RawValue)), new SkinEntry(Load(root, FixtureIds.RasterSkin.RawValue), RasterProduct)]);
}
