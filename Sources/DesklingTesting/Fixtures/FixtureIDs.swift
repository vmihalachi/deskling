import DesklingCompanion

/// The ids the fixtures use: three flavors and two skins, named for no app. Mechanism tests and the vectors in
/// `conformance/companion/` run over these.
public enum FixtureIDs {
    public static let flavorA: FlavorID = "a"
    public static let flavorB: FlavorID = "b"
    public static let flavorC: FlavorID = "c"
    /// Every flavor, the fixture defaults' choice.
    public static let flavors: Set<FlavorID> = [flavorA, flavorB, flavorC]

    /// The default skin, drawn with vector parts.
    public static let vectorSkin: SkinID = "fixture-vector"
    /// The second skin, drawn with image parts.
    public static let rasterSkin: SkinID = "fixture-raster"
    /// Every skin, the default first.
    public static let skins: [SkinID] = [vectorSkin, rasterSkin]
}
