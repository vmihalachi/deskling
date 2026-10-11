using Deskling.Companion.Skins;

namespace Deskling.Companion.Tests;

public sealed class SkinCatalogTests
{
    private static readonly SkinCatalog Catalog = Fixture.Catalog;
    private static readonly HashSet<string> None = [];
    private static readonly HashSet<string> Raster = [FixtureArt.RasterProduct];

    [Fact]
    public void TheDefaultIsTheFirstSkin()
    {
        Assert.Equal(FixtureIds.VectorSkin, Catalog.DefaultSkin);
        Assert.Equal(FixtureIds.Skins, Catalog.Skins.Select(s => s.Id));
        Assert.Equal(FixtureArt.RasterProduct, Catalog[FixtureIds.RasterSkin]?.UnlockedBy);
        Assert.Null(Catalog["fixture-gone"]);
    }

    [Fact]
    public void LockedSkinFallsBackToTheDefault()
    {
        Assert.False(Catalog.IsUnlocked(FixtureIds.RasterSkin, None));
        Assert.Equal(FixtureIds.VectorSkin, Catalog.EffectiveSkin(FixtureIds.RasterSkin, None));
        Assert.Equal(FixtureIds.VectorSkin, Catalog.EffectiveSkin(FixtureIds.RasterSkin, new HashSet<string> { "some.other.product" }));
    }

    [Fact]
    public void OwnedSkinIsDrawn()
    {
        Assert.True(Catalog.IsUnlocked(FixtureIds.RasterSkin, Raster));
        Assert.Equal(FixtureIds.RasterSkin, Catalog.EffectiveSkin(FixtureIds.RasterSkin, Raster));
        Assert.Equal(FixtureIds.RasterSkin, Catalog.Character(FixtureIds.RasterSkin).Skin);
    }

    [Fact]
    public void RevokedSkinDropsBackToTheDefault()
    {
        var owned = new HashSet<string>(Raster);
        Assert.Equal(FixtureIds.RasterSkin, Catalog.EffectiveSkin(FixtureIds.RasterSkin, owned));
        owned.Remove(FixtureArt.RasterProduct); // a refund
        Assert.Equal(FixtureIds.VectorSkin, Catalog.EffectiveSkin(FixtureIds.RasterSkin, owned));
    }

    [Fact]
    public void UnknownSkinIsTheDefault()
    {
        Assert.False(Catalog.IsUnlocked("fixture-gone", Raster));
        Assert.Equal(FixtureIds.VectorSkin, Catalog.EffectiveSkin("fixture-gone", None));
        Assert.Equal(FixtureIds.VectorSkin, Catalog.Character("fixture-gone").Skin);
    }

    [Fact]
    public void FreeSkinsAreAlwaysUnlocked()
    {
        Assert.True(Catalog.IsUnlocked(FixtureIds.VectorSkin, None));
        Assert.Equal(FixtureIds.VectorSkin, Catalog.EffectiveSkin(FixtureIds.VectorSkin, None));
    }

    [Fact]
    public void TheCatalogRefusesABadList()
    {
        var vector = new SkinEntry(Fixture.Vector);
        var raster = new SkinEntry(Fixture.Raster, FixtureArt.RasterProduct);
        Assert.Throws<ArgumentException>(() => new SkinCatalog([]));
        Assert.Throws<ArgumentException>(() => new SkinCatalog([raster, vector]));
        Assert.Throws<ArgumentException>(() => new SkinCatalog([vector, vector]));
    }
}
