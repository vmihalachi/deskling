using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace Laching.Windows.Companion.Shell;

/// <summary>Win32 calls the Windows App SDK doesn't wrap. Internal: the services are the API.</summary>
internal static partial class NativeMethods
{
    /// <summary>The DPI a window is drawn at when its scale is 100 %.</summary>
    public const double DefaultDpi = 96;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(nint hwnd);

    [LibraryImport("user32.dll")]
    public static partial uint GetDpiForWindow(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out POINT point);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    /// <summary>MONITOR_DPI_TYPE::MDT_EFFECTIVE_DPI: the scale the user picked for the display.</summary>
    private const int MDT_EFFECTIVE_DPI = 0;

    [LibraryImport("user32.dll")]
    private static partial nint MonitorFromPoint(POINT point, uint flags);

    [LibraryImport("shcore.dll")]
    private static partial int GetDpiForMonitor(nint monitor, int dpiType, out uint dpiX, out uint dpiY);

    /// <summary>A window's scale (1 at 96 DPI, 2 at 192), or 1 when it has none yet.</summary>
    public static double Scale(nint hwnd)
    {
        var dpi = hwnd == 0 ? 0 : GetDpiForWindow(hwnd);
        return dpi == 0 ? 1 : dpi / DefaultDpi;
    }

    /// <summary>The scale of the display at a screen point (the nearest one), for sizing a window before it is there.</summary>
    public static double Scale(int x, int y)
    {
        var monitor = MonitorFromPoint(new POINT { X = x, Y = y }, MONITOR_DEFAULTTONEAREST);
        return monitor != 0 && GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out var dpi, out _) == 0 && dpi > 0
            ? dpi / DefaultDpi
            : 1;
    }

    /// <summary>
    /// Gives the window a client area of <paramref name="client"/> pixels. Not <c>AppWindow.ResizeClient</c>: for a
    /// window without a title bar it still adds a caption's height, so the frame is measured from the window instead.
    /// </summary>
    public static void SetClientSize(AppWindow window, SizeInt32 client)
    {
        var frameWidth = window.Size.Width - window.ClientSize.Width;
        var frameHeight = window.Size.Height - window.ClientSize.Height;
        window.Resize(new SizeInt32(client.Width + frameWidth, client.Height + frameHeight));
    }

    /// <summary>The pointer's position in screen pixels, or the origin when it can't be read.</summary>
    public static (int X, int Y) Cursor() => GetCursorPos(out var point) ? (point.X, point.Y) : (0, 0);

    private const uint DWMWA_CLOAK = 13;
    private static readonly nint HWND_BOTTOM = 1;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(nint hwnd, uint attribute, in int value, uint size);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);

    /// <summary>
    /// Shows the window where nobody sees it: cloaked (the compositor still draws it, so <c>PrintWindow</c> captures it,
    /// but never puts it on screen), out of the taskbar and Alt+Tab, behind every other window and without taking the focus.
    /// </summary>
    public static void ShowHidden(AppWindow window)
    {
        var hwnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(window.Id);
        DwmSetWindowAttribute(hwnd, DWMWA_CLOAK, 1, sizeof(int));
        window.IsShownInSwitchers = false;
        window.Show(activateWindow: false);
        SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
    }

    /// <summary>MUI_LANGUAGE_NAME: languages given by name ("de-DE"), not by number.</summary>
    private const uint MUI_LANGUAGE_NAME = 0x8;

    /// <summary>Takes a double-null-terminated list; skips the names it doesn't know, and fails only when it knows none.</summary>
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetProcessPreferredUILanguages(uint flags, string languages, out uint count);

    /// <summary>
    /// Makes <paramref name="languages"/> (BCP-47 tags, in order) this process's preferred UI languages, ahead of
    /// Windows' own: the language WinUI's own words come in when it starts. False when Windows took none of them.
    /// </summary>
    public static bool SetProcessLanguages(IEnumerable<string> languages) =>
        SetProcessPreferredUILanguages(MUI_LANGUAGE_NAME, string.Join('\0', languages) + "\0\0", out _);
}
