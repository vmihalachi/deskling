using Windows.Graphics;
using FigureRect = Laching.Companion.Figures.Rect;

namespace Laching.Windows.Companion.Desktop;

/// <summary>Screen rectangles between WinUI's whole pixels and the figures' placement math, for the desktop windows.</summary>
internal static class ScreenRects
{
    public static FigureRect Rect(RectInt32 r) => new(r.X, r.Y, r.Width, r.Height);

    /// <summary>To whole pixels, each edge rounded.</summary>
    public static RectInt32 Int(FigureRect r) =>
        new((int)Math.Round(r.Origin.X), (int)Math.Round(r.Origin.Y), (int)Math.Round(r.Size.Width), (int)Math.Round(r.Size.Height));
}
