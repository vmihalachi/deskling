using System.Drawing;
using Deskling.Companion.Figures;
using Deskling.Companion.Rules;
using Deskling.Testing.Conformance;
using Deskling.Testing.Fixtures;
using Deskling.Windows.Companion.Shell;

namespace Deskling.Windows.Companion.Tests.Shell;

public sealed class GlyphRendererTests
{
    private static readonly RiggedCharacter Character = FixtureArt.Characters(ConformanceRoot.Find())[0];

    [Theory]
    [InlineData(16)]
    [InlineData(20)]
    [InlineData(24)]
    [InlineData(32)]
    public void ItFillsTheSmallIconSizeWithTransparentCorners(int pixels)
    {
        var image = GlyphRenderer.Image(Character, Mood.Awake, 0, lightTaskbar: true, pixels);

        Assert.Equal((pixels, pixels), (image.Width, image.Height));
        Assert.Equal(0, image.GetPixel(0, 0).A);
        Assert.True(Pixels(image).Count(color => color.A > 0) > pixels, "a glyph draws something");
    }

    [Fact]
    public void TheInkIsBlackOnALightTaskbarAndWhiteOnADarkOne()
    {
        var light = Ink(GlyphRenderer.Image(Character, Mood.Awake, 0, lightTaskbar: true, 32));
        var dark = Ink(GlyphRenderer.Image(Character, Mood.Awake, 0, lightTaskbar: false, 32));

        Assert.All(light, color => Assert.Equal((0, 0, 0), (color.R, color.G, color.B)));
        Assert.All(dark, color => Assert.Equal((255, 255, 255), (color.R, color.G, color.B)));
    }

    [Fact]
    public void FramesWrapAroundTheMoodsFrames()
    {
        var count = Character.GlyphFrameCount(Mood.Talking);

        Assert.Same(
            GlyphRenderer.Image(Character, Mood.Talking, 1, true, 24),
            GlyphRenderer.Image(Character, Mood.Talking, 1 + count, true, 24)
        );
        Assert.NotEqual(
            Pixels(GlyphRenderer.Image(Character, Mood.Talking, 0, true, 24)),
            Pixels(GlyphRenderer.Image(Character, Mood.Talking, 1, true, 24))
        );
    }

    [Fact]
    public void EachMoodDrawsItsOwnFrame()
    {
        Assert.NotEqual(
            Pixels(GlyphRenderer.Image(Character, Mood.Awake, 0, true, 24)),
            Pixels(GlyphRenderer.Image(Character, Mood.Asleep, 0, true, 24))
        );
    }

    [Fact]
    public void TheIconIsAnHicon()
    {
        var icon = GlyphRenderer.Icon(Character, Mood.Awake, 0, lightTaskbar: true, pixels: 16);
        try
        {
            Assert.NotEqual(0, icon);
        }
        finally
        {
            DestroyIcon(icon);
        }
    }

    /// <summary>The fully opaque pixels: the strokes' and fills' cores, away from their soft edges.</summary>
    private static List<Color> Ink(Bitmap image) => [.. Pixels(image).Where(color => color.A == 255)];

    private static List<Color> Pixels(Bitmap image) =>
        [.. Enumerable.Range(0, image.Height).SelectMany(y => Enumerable.Range(0, image.Width).Select(x => image.GetPixel(x, y)))];

    [global::System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint icon);
}
