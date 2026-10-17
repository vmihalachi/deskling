using Windows.Graphics;
using FigureRect = Laching.Companion.Figures.Rect;

namespace Laching.Windows.Companion.Desktop;

/// <summary>
/// Where a finger reaches a desktop window while the mouse pointer is off it, in the window's pixels: each drawn box
/// (content DIPs, one per part, by index) grown by <see cref="Margin"/> DIPs and held for <see cref="HoldSeconds"/>, so a
/// pose that sways or talks stays inside between updates. Boxes are cut to the window. UI thread only.
/// </summary>
internal sealed class TouchArea
{
    /// <summary>How far past the drawing a finger still lands, in DIPs.</summary>
    public const double Margin = 8;

    /// <summary>How long a box the drawing has filled stays in.</summary>
    public const double HoldSeconds = 3;

    private readonly List<Queue<(double At, FigureRect Box)>> held = [];

    /// <summary>Forgets the held boxes: the drawing changed size.</summary>
    public void Clear() => held.Clear();

    /// <summary>
    /// The region's boxes (pixels, from the window's top left) at <paramref name="now"/> (seconds) with
    /// <paramref name="drawn"/> the boxes drawn now, at <paramref name="scale"/> pixels per DIP in a window
    /// <paramref name="width"/> × <paramref name="height"/> pixels. A different number of boxes starts over.
    /// </summary>
    public IReadOnlyList<RectInt32> Update(double now, IReadOnlyList<FigureRect> drawn, double scale, int width, int height)
    {
        if (drawn.Count != held.Count)
        {
            held.Clear();
            for (var i = 0; i < drawn.Count; i++)
                held.Add(new Queue<(double, FigureRect)>());
        }
        var boxes = new List<RectInt32>(drawn.Count);
        for (var i = 0; i < drawn.Count; i++)
        {
            var times = held[i];
            times.Enqueue((now, drawn[i]));
            while (times.Peek().At < now - HoldSeconds)
                times.Dequeue();
            var (left, top, right, bottom) = (double.MaxValue, double.MaxValue, double.MinValue, double.MinValue);
            foreach (var (_, box) in times)
            {
                left = Math.Min(left, box.Origin.X);
                top = Math.Min(top, box.Origin.Y);
                right = Math.Max(right, box.Origin.X + box.Size.Width);
                bottom = Math.Max(bottom, box.Origin.Y + box.Size.Height);
            }
            var x0 = Math.Max(0, (int)Math.Floor((left - Margin) * scale));
            var y0 = Math.Max(0, (int)Math.Floor((top - Margin) * scale));
            var x1 = Math.Min(width, (int)Math.Ceiling((right + Margin) * scale));
            var y1 = Math.Min(height, (int)Math.Ceiling((bottom + Margin) * scale));
            if (x1 > x0 && y1 > y0)
                boxes.Add(new RectInt32(x0, y0, x1 - x0, y1 - y0));
        }
        return boxes;
    }
}
