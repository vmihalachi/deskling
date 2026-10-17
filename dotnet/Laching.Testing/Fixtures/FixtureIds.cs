using Laching.Companion.Phrases;

namespace Laching.Testing.Fixtures;

/// <summary>
/// The ids the fixtures use (Swift's <c>FixtureIDs</c>): three flavors and two skins, named for no app. Mechanism tests
/// and the vectors in <c>conformance/companion/</c> run over these.
/// </summary>
public static class FixtureIds
{
    public static FlavorId FlavorA { get; } = new("a");

    public static FlavorId FlavorB { get; } = new("b");

    public static FlavorId FlavorC { get; } = new("c");

    /// <summary>Every flavor, in display order; the fixture defaults turn them all on.</summary>
    public static IReadOnlyList<FlavorId> Flavors { get; } = [FlavorA, FlavorB, FlavorC];

    /// <summary>The default skin, drawn with vector parts.</summary>
    public static SkinId VectorSkin { get; } = new("fixture-vector");

    /// <summary>The second skin, drawn with image parts.</summary>
    public static SkinId RasterSkin { get; } = new("fixture-raster");

    /// <summary>Every skin, the default first.</summary>
    public static IReadOnlyList<SkinId> Skins { get; } = [VectorSkin, RasterSkin];
}
