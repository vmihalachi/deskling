import LachingCompanion

/// The fixture skins' art, compiled by `scripts/fixture-art.sh` from `Tests/Fixtures/characters/` into
/// `FixtureVectorArt.swift` and `FixtureRasterArt.swift`: one vector skin (the default) and one raster skin.
public enum FixtureArt {
    /// `fixture-vector`: every part is shapes.
    public static let vector = CharacterArt.fixtureVector
    /// `fixture-raster`: every part is an image (`fixture-raster.<part>`).
    public static let raster = CharacterArt.fixtureRaster
    /// The product id that unlocks the raster skin in `SkinCatalog.fixture`.
    public static let rasterProduct = "fixture.skin.raster"
}

extension SkinCatalog {
    /// The vector skin (free, the default) and the raster skin (unlocked by `FixtureArt.rasterProduct`).
    public static let fixture = SkinCatalog([
        SkinEntry(art: FixtureArt.vector),
        SkinEntry(art: FixtureArt.raster, unlockedBy: FixtureArt.rasterProduct),
    ])
}
