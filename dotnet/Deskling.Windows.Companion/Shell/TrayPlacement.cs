using Windows.Graphics;

namespace Deskling.Windows.Companion.Shell;

/// <summary>
/// Where a window goes near the notification area, in screen pixels: the taskbar's edge is the side of the display the
/// work area gives up to it (the bottom on Windows 11, or when the taskbar hides itself).
/// </summary>
internal static class TrayPlacement
{
    private enum Edge
    {
        Bottom,
        Top,
        Left,
        Right,
    }

    /// <summary>
    /// The work area's corner by the notification area (the taskbar's end), <paramref name="margin"/> in from both edges.
    /// </summary>
    public static PointInt32 Corner(RectInt32 workArea, RectInt32 bounds, SizeInt32 size, int margin)
    {
        var right = workArea.X + workArea.Width - size.Width - margin;
        var bottom = workArea.Y + workArea.Height - size.Height - margin;
        return TaskbarEdge(workArea, bounds) switch
        {
            Edge.Top => new(right, workArea.Y + margin),
            Edge.Left => new(workArea.X + margin, bottom),
            _ => new(right, bottom),
        };
    }

    /// <summary>
    /// Next to the icon on the work area's side of the taskbar, centered on the icon along it and kept
    /// <paramref name="margin"/> inside the work area (a flyout).
    /// </summary>
    public static PointInt32 NextTo(RectInt32 icon, RectInt32 workArea, RectInt32 bounds, SizeInt32 size, int margin)
    {
        var x = Clamp(icon.X + icon.Width / 2 - size.Width / 2, workArea.X + margin, workArea.X + workArea.Width - size.Width - margin);
        var y = Clamp(icon.Y + icon.Height / 2 - size.Height / 2, workArea.Y + margin, workArea.Y + workArea.Height - size.Height - margin);
        return TaskbarEdge(workArea, bounds) switch
        {
            Edge.Top => new(x, workArea.Y + margin),
            Edge.Left => new(workArea.X + margin, y),
            Edge.Right => new(workArea.X + workArea.Width - size.Width - margin, y),
            _ => new(x, workArea.Y + workArea.Height - size.Height - margin),
        };
    }

    private static Edge TaskbarEdge(RectInt32 workArea, RectInt32 bounds)
    {
        if (workArea.Y > bounds.Y)
            return Edge.Top;
        if (workArea.X > bounds.X)
            return Edge.Left;
        if (workArea.X + workArea.Width < bounds.X + bounds.Width)
            return Edge.Right;
        return Edge.Bottom;
    }

    /// <summary>Within [low, high], or low when the window is wider than the room.</summary>
    private static int Clamp(int value, int low, int high) => Math.Max(low, Math.Min(value, high));
}
