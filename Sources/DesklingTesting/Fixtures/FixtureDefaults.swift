import DesklingCompanion
import DesklingCore

extension CompanionDefaults {
    /// The package's defaults with the fixture's product choices: the vector skin, every fixture flavor on and
    /// ⌃⌥⌘F for "Say something". Mechanism tests and `conformance/companion/` run over it.
    public static let fixture = CompanionDefaults(
        skin: FixtureIDs.vectorSkin, phraseFlavors: FixtureIDs.flavors,
        sayShortcut: KeyShortcut(keyCode: 3, modifiers: KeyShortcut.control | KeyShortcut.option | KeyShortcut.command, key: "F"))
}
