using System.Drawing;
using System.Drawing.Drawing2D;
using Laching.Companion.Figures;
using Laching.Companion.Rules;
using Laching.Windows.Shell;

namespace Laching.Windows.Companion.Shell;

/// <summary>
/// Draws a character's tray glyph (Swift's <c>GlyphRenderer</c>): its glyph frame for a mood (two while talking), the
/// line drawing in a <see cref="RiggedCharacter.GlyphSize"/> square scaled to the tray's small-icon size, in black ink
/// on a light taskbar and white on a dark one (the Mac's template image, which macOS tints). Through GDI+, as the
/// notification area takes an <c>HICON</c>. Bitmaps are cached per skin, mood, frame, size and ink (a handful).
/// UI thread only.
/// </summary>
public static class GlyphRenderer
{
    private static readonly Dictionary<(string Skin, Mood Mood, int Frame, int Pixels, bool Light), Bitmap> Cache = [];

    /// <summary>
    /// An <c>HICON</c> for <see cref="TrayIconHost.SetGlyph"/>, which frees it. <paramref name="frame"/> wraps around the
    /// mood's frames; <paramref name="pixels"/> is <see cref="TrayIconHost.SmallIconPixels"/> by default.
    /// </summary>
    public static nint Icon(RiggedCharacter character, Mood mood, int frame, bool lightTaskbar, int? pixels = null) =>
        Image(character, mood, frame, lightTaskbar, pixels ?? TrayIconHost.SmallIconPixels).GetHicon();

    /// <summary>The glyph as a square bitmap with a transparent background; the cache owns it.</summary>
    internal static Bitmap Image(RiggedCharacter character, Mood mood, int frame, bool lightTaskbar, int pixels)
    {
        var count = Math.Max(character.GlyphFrameCount(mood), 1);
        frame = ((frame % count) + count) % count;
        var key = (character.Skin.RawValue, mood, frame, pixels, lightTaskbar);
        if (Cache.TryGetValue(key, out var cached))
            return cached;
        var bitmap = Draw(character.Glyph(mood, frame), character.GlyphSize, pixels, lightTaskbar ? Color.Black : Color.White);
        Cache[key] = bitmap;
        return bitmap;
    }

    private static Bitmap Draw(IReadOnlyList<PathShape> shapes, double glyphSize, int pixels, Color ink)
    {
        var bitmap = new Bitmap(pixels, pixels);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);
        var scale = (float)(pixels / glyphSize);
        graphics.ScaleTransform(scale, scale);
        using var fill = new SolidBrush(ink);
        foreach (var shape in shapes)
        {
            using var path = Path(shape);
            if (shape.Fill is not null)
                graphics.FillPath(fill, path);
            if (shape.Stroke is not null && shape.StrokeWidth > 0)
            {
                // Round caps and joins keep the tiny strokes soft, as on the Mac. The width is in glyph units, so the
                // transform scales it with the rest.
                using var pen = new Pen(ink, (float)shape.StrokeWidth)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round,
                };
                graphics.DrawPath(pen, path);
            }
        }
        return bitmap;
    }

    private static GraphicsPath Path(PathShape shape)
    {
        var path = new GraphicsPath(shape.EvenOdd ? FillMode.Alternate : FillMode.Winding);
        var current = new PointF();
        var start = current;
        foreach (var command in shape.Path)
        {
            switch (command)
            {
                case PathCommand.Move move:
                    path.StartFigure();
                    current = start = P(move.To);
                    break;
                case PathCommand.Line line:
                    var to = P(line.To);
                    path.AddLine(current, to);
                    current = to;
                    break;
                case PathCommand.Cubic cubic:
                    var end = P(cubic.To);
                    path.AddBezier(current, P(cubic.Control1), P(cubic.Control2), end);
                    current = end;
                    break;
                case PathCommand.Close:
                    path.CloseFigure();
                    current = start;
                    break;
            }
        }
        return path;
    }

    private static PointF P(Laching.Companion.Figures.Point point) => new((float)point.X, (float)point.Y);
}
