using System.Drawing;
using System.Drawing.Imaging;
using Laching.Windows.Companion.Views;

namespace Laching.Windows.Companion.Tests.Views;

public sealed class CharacterImagesTests
{
    [Theory]
    [InlineData(1.0, 200)]
    [InlineData(2.0, 200)]
    [InlineData(2.5, 300)]
    [InlineData(6.0, 300)]
    public void ItPicksTheSmallestScaleThatIsSharpEnoughElseTheSharpest(double pixelsPerUnit, int expected) =>
        Assert.Equal(expected, CharacterImages.Pick([300, 200], pixelsPerUnit));

    [Fact]
    public void WithNoScalesThereIsNothingToPick() => Assert.Null(CharacterImages.Pick([], 2));

    [Fact]
    public void ItFindsAnAssetsScaledFilesByWindowsNames()
    {
        using var folder = new SoundFolder();
        foreach (var name in new[] { "skin.head.png", "skin.head.scale-200.png", "skin.head.scale-400.png", "skin.headband.scale-100.png", "skin.head.scale-200.jpg" })
            File.WriteAllBytes(Path.Combine(folder.Path, name), []);

        Assert.EndsWith("skin.head.png", CharacterImages.File(folder.Path, "skin.head", 1));
        Assert.EndsWith("skin.head.scale-200.png", CharacterImages.File(folder.Path, "skin.head", 1.5));
        Assert.EndsWith("skin.head.scale-400.png", CharacterImages.File(folder.Path, "skin.head", 3));
        Assert.Null(CharacterImages.File(folder.Path, "skin.tail", 2));
        Assert.Null(CharacterImages.File(Path.Combine(folder.Path, "missing"), "skin.head", 2));
    }

    [Fact]
    public void TheMaskReadsAPartsAlpha()
    {
        // A disc in a transparent square: the corners are empty, the middle is drawn.
        using var folder = new SoundFolder();
        var file = Path.Combine(folder.Path, "disc.png");
        using (var bitmap = new Bitmap(40, 20))
        {
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.FillEllipse(Brushes.Black, 0, 0, 40, 20);
            }
            bitmap.Save(file, ImageFormat.Png);
        }

        var mask = AlphaMask.Read(file);

        Assert.NotNull(mask);
        Assert.Equal((40, 20), (mask.Width, mask.Height));
        Assert.False(mask.IsDrawn(0.01, 0.01));
        Assert.True(mask.IsDrawn(0.5, 0.5));
        Assert.True(mask.IsDrawn(0.05, 0.5));
        Assert.False(mask.IsDrawn(1, 0.5));
        Assert.False(mask.IsDrawn(-0.1, 0.5));
    }

    [Fact]
    public void AFileThatIsNoImageHasNoMask()
    {
        using var folder = new SoundFolder();

        Assert.Null(AlphaMask.Read(folder.Garbage("broken.png")));
        Assert.Null(AlphaMask.Read(Path.Combine(folder.Path, "missing.png")));
    }
}
