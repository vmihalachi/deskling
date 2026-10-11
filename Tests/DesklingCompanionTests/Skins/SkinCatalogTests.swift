import DesklingCompanion
import DesklingTesting
import XCTest

final class SkinCatalogTests: XCTestCase {
    let catalog = SkinCatalog.fixture
    let vector = FixtureIDs.vectorSkin
    let raster = FixtureIDs.rasterSkin

    func testTheDefaultIsTheFirstSkin() {
        XCTAssertEqual(catalog.defaultSkin, vector)
        XCTAssertEqual(catalog.skins.map(\.id), FixtureIDs.skins)
        XCTAssertEqual(catalog[raster]?.unlockedBy, FixtureArt.rasterProduct)
        XCTAssertNil(catalog["fixture-gone"])
    }

    func testLockedSkinFallsBackToTheDefault() {
        XCTAssertFalse(catalog.isUnlocked(raster, owned: []))
        XCTAssertEqual(catalog.effectiveSkin(selected: raster, owned: []), vector)
        XCTAssertEqual(catalog.effectiveSkin(selected: raster, owned: ["some.other.product"]), vector)
    }

    func testOwnedSkinIsDrawn() {
        XCTAssertTrue(catalog.isUnlocked(raster, owned: [FixtureArt.rasterProduct]))
        XCTAssertEqual(catalog.effectiveSkin(selected: raster, owned: [FixtureArt.rasterProduct]), raster)
        XCTAssertEqual(catalog.character(for: raster).skin, raster)
    }

    func testRevokedSkinDropsBackToTheDefault() {
        var owned: Set<String> = [FixtureArt.rasterProduct]
        XCTAssertEqual(catalog.effectiveSkin(selected: raster, owned: owned), raster)
        owned.remove(FixtureArt.rasterProduct)  // a refund
        XCTAssertEqual(catalog.effectiveSkin(selected: raster, owned: owned), vector)
    }

    func testUnknownSkinIsTheDefault() {
        XCTAssertFalse(catalog.isUnlocked("fixture-gone", owned: [FixtureArt.rasterProduct]))
        XCTAssertEqual(catalog.effectiveSkin(selected: "fixture-gone", owned: []), vector)
        XCTAssertEqual(catalog.character(for: "fixture-gone").skin, vector)
    }

    func testFreeSkinsAreAlwaysUnlocked() {
        XCTAssertTrue(catalog.isUnlocked(vector, owned: []))
        XCTAssertEqual(catalog.effectiveSkin(selected: vector, owned: []), vector)
    }
}
