using Laching.Companion.Figures;
using Laching.Companion.Rules;

namespace Laching.Companion.Tests;

/// <summary>The rig over the raster fixture (<c>Tests/Fixtures/characters/fixture-raster</c>): image parts are placed, never drawn.</summary>
public sealed class RasterCharacterTests
{
    private static readonly RiggedCharacter Character = new(Fixture.Raster);

    [Fact]
    public void EveryPartIsAnImageNamedForTheSkin()
    {
        foreach (var (name, content) in Fixture.Raster.Parts)
        {
            var image = Assert.IsType<PartContent.Image>(content);
            Assert.Equal($"fixture-raster.{name}", image.Asset);
            Assert.True(image.Frame.Size.Width > 0);
        }
        Assert.Equal(new PartContent.Image("fixture-raster.head", new Rect(24, 14, 52, 52)), Fixture.Raster.Parts["head"]);
    }

    [Fact]
    public void LayersPlaceTheImages()
    {
        var layers = Character.Layers(Character.StillPose(Mood.Asleep));
        Assert.Equal(["stem", "head", "cheeks", "eyes-closed", "mouth-talk-2", "zz"], layers.Select(l => l.Name));
        Assert.All(layers, l => Assert.Equal("image", l.Content.Kind));
        Assert.Equal(Transform.Identity, layers[^1].Transform);
    }

    [Fact]
    public void HeadTransformIsDroopThenSink()
    {
        var pose = Character.Pose(Mood.Shushed, 0);
        var head = Character.Layers(pose).First(l => l.Name == "eyes-open").Transform;
        Assert.Equal(Transform.Rotation(14, new Point(50, 62)).Then(Transform.Translation(0, 2)), head);
        // Rotation and translation only: the 2×2 part stays orthonormal, so an image never shears or stretches.
        var a = head.Affine;
        Assert.Equal(1, a.A * a.A + a.B * a.B, 12);
        Assert.Equal(0, a.A * a.C + a.B * a.D, 12);
    }

    [Fact]
    public void PulseScalesTheHeadUniformly()
    {
        var pose = Character.Pose(Mood.Talking, 1.0 / 12);
        Assert.True(pose.Pulse > 0);
        var head = Character.Layers(pose).First(l => l.Name == "head").Transform;
        Assert.Equal(head.A, head.D, 12);
        Assert.Equal(head.B, -head.C, 12);
        Assert.Equal(1 + pose.Pulse, Math.Sqrt(head.A * head.A + head.B * head.B), 12);
    }

    [Fact]
    public void ShapesSkipImageParts()
    {
        foreach (var mood in Moods.All)
            Assert.Empty(Character.Shapes(Character.StillPose(mood)));
    }

    [Fact]
    public void TheGlyphStaysVector()
    {
        Assert.NotEmpty(Character.Glyph(Mood.Awake, 0));
        Assert.Equal(new RiggedCharacter(Fixture.Vector).Glyph(Mood.Talking, 1), Character.Glyph(Mood.Talking, 1));
    }
}
