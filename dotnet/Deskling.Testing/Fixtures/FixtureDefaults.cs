using Deskling.Companion.Settings;

namespace Deskling.Testing.Fixtures;

/// <summary>Swift's <c>CompanionDefaults.fixture</c>.</summary>
public static class FixtureDefaults
{
    /// <summary>
    /// The package's defaults with the fixture's product choices: the vector skin and every fixture flavor on.
    /// Mechanism tests and <c>conformance/companion/</c> run over it.
    /// </summary>
    public static CompanionDefaults Value { get; } = new(FixtureIds.VectorSkin, FixtureIds.Flavors.ToHashSet());
}
