using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using IOPath = System.IO.Path;

namespace Deskling.Windows.Companion.Views;

/// <summary>
/// A raster part's PNGs, found by asset name (<c>&lt;skin&gt;.&lt;part&gt;</c>) in a folder as Windows names scaled
/// images: <c>&lt;asset&gt;.scale-200.png</c> has 2 pixels per view-box unit, <c>&lt;asset&gt;.png</c> one. The canvas
/// draws the smallest that is at least as sharp as the screen needs, else the sharpest; the hit test reads its alpha.
/// Cached per folder and asset (the files ship with the app and don't change while it runs).
/// </summary>
public static partial class CharacterImages
{
    private static readonly Dictionary<(string Folder, string Asset), IReadOnlyList<(int Scale, string Path)>> Variants = [];
    private static readonly Dictionary<string, AlphaMask?> Masks = [];
    private static readonly Lock Gate = new();

    /// <summary>The PNG to draw <paramref name="asset"/> from at <paramref name="pixelsPerUnit"/>; null when there is none.</summary>
    public static string? File(string folder, string asset, double pixelsPerUnit)
    {
        var variants = VariantsOf(folder, asset);
        var scale = Pick(variants.Select(v => v.Scale), pixelsPerUnit);
        return scale is { } s ? variants.First(v => v.Scale == s).Path : null;
    }

    /// <summary>The scale (percent) to use: the smallest of <paramref name="available"/> at or above <paramref name="pixelsPerUnit"/> × 100, else the largest.</summary>
    public static int? Pick(IEnumerable<int> available, double pixelsPerUnit)
    {
        var scales = available.Order().ToArray();
        if (scales.Length == 0)
            return null;
        var needed = pixelsPerUnit * 100 - 0.5;
        foreach (var scale in scales)
            if (scale >= needed)
                return scale;
        return scales[^1];
    }

    /// <summary>The alpha of the PNG at <paramref name="path"/>, read once; null when it can't be read.</summary>
    public static AlphaMask? Mask(string path)
    {
        lock (Gate)
        {
            if (Masks.TryGetValue(path, out var cached))
                return cached;
        }
        var mask = AlphaMask.Read(path);
        lock (Gate)
            Masks[path] = mask;
        return mask;
    }

    private static IReadOnlyList<(int Scale, string Path)> VariantsOf(string folder, string asset)
    {
        lock (Gate)
        {
            if (Variants.TryGetValue((folder, asset), out var cached))
                return cached;
        }
        var found = new List<(int, string)>();
        if (Directory.Exists(folder))
        {
            foreach (var path in Directory.EnumerateFiles(folder, asset + ".*png"))
            {
                var name = IOPath.GetFileName(path);
                if (name.Equals(asset + ".png", StringComparison.OrdinalIgnoreCase))
                    found.Add((100, path));
                else if (ScaleName().Match(name) is { Success: true } m && name.StartsWith(asset + ".scale-", StringComparison.OrdinalIgnoreCase))
                    found.Add((int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), path));
            }
        }
        lock (Gate)
            Variants[(folder, asset)] = found;
        return found;
    }

    [GeneratedRegex(@"\.scale-(\d+)\.png$", RegexOptions.IgnoreCase)]
    private static partial Regex ScaleName();
}

/// <summary>An image's alpha, one byte per pixel, row by row: what the hit test reads under a point.</summary>
public sealed class AlphaMask(int width, int height, byte[] alpha)
{
    /// <summary>Below this alpha a pixel counts as empty: a soft edge's faint fringe lets clicks through.</summary>
    public const byte Threshold = 0x20;

    public int Width { get; } = width;

    public int Height { get; } = height;

    /// <summary>Whether the pixel at <paramref name="u"/>, <paramref name="v"/> (0 to 1 across and down) is drawn.</summary>
    public bool IsDrawn(double u, double v)
    {
        if (!(u >= 0 && u < 1 && v >= 0 && v < 1))
            return false;
        var x = Math.Min(Width - 1, (int)(u * Width));
        var y = Math.Min(Height - 1, (int)(v * Height));
        return alpha[y * Width + x] >= Threshold;
    }

    /// <summary>Through GDI+, as the tray glyph is drawn; null when the file isn't an image.</summary>
    public static AlphaMask? Read(string path)
    {
        try
        {
            using var bitmap = new Bitmap(path);
            var (width, height) = (bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var row = new byte[width * 4];
                var alpha = new byte[width * height];
                for (var y = 0; y < height; y++)
                {
                    Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, row.Length);
                    for (var x = 0; x < width; x++)
                        alpha[y * width + x] = row[x * 4 + 3]; // BGRA
                }
                return new AlphaMask(width, height, alpha);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
        catch (Exception e) when (e is ArgumentException or IOException or ExternalException)
        {
            return null;
        }
    }
}
