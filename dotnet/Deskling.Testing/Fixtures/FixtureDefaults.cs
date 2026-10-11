using Deskling.Companion.Settings;
using Deskling.Core.Input;

namespace Deskling.Testing.Fixtures;

/// <summary>Swift's <c>CompanionDefaults.fixture</c>.</summary>
public static class FixtureDefaults
{
    /// <summary>
    /// The package's defaults with the fixture's product choices: the vector skin, every fixture flavor on and ⌃⌥⌘F
    /// (Carbon key code 3) for "Say something". Mechanism tests and <c>conformance/companion/</c> run over it.
    /// </summary>
    public static CompanionDefaults Value { get; } =
        new(
            FixtureIds.VectorSkin,
            FixtureIds.Flavors.ToHashSet(),
            new KeyShortcut(3, KeyShortcut.Control | KeyShortcut.Option | KeyShortcut.Command, "F")
        );
}
