import DesklingCompanion

extension CompanionDefaults {
    /// The package's defaults with the fixture's product choices: the vector skin and every fixture flavor on.
    /// Mechanism tests and `conformance/companion/` run over it.
    public static let fixture = CompanionDefaults(skin: FixtureIDs.vectorSkin, phraseFlavors: FixtureIDs.flavors)
}
