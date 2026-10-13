using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Deskling.Windows.Companion.Views;
using Deskling.Windows.Shell;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using XamlSize = Windows.Foundation.Size;

namespace Deskling.Windows.Companion.Screenshots;

/// <summary>
/// Renders an app's windows and views to PNGs in its own process (Swift's <c>ScreenshotCapture</c>), for documentation
/// and store screenshots: the generic half of a screenshot mode. The app's run script sets <see cref="ModeVariable"/> (a
/// comma-separated list of shot name prefixes, or <c>all</c>) and optionally <see cref="DirectoryVariable"/> (else a
/// folder in the temporary directory); <see cref="Finish"/> prints the folder last. The app builds its model over a
/// <c>SampleEnvironment</c> (in-memory settings, a fixed clock, silent channels, fixed prices), so the user's settings,
/// speakers and the store are never touched, then shoots what it wants (<see cref="ScreenshotScript{TWindow, TSettings}"/>).
/// While a run goes, the character holds its still pose and nothing animates.
/// </summary>
/// <remarks>
/// A window is captured as the screen shows it (<c>PrintWindow</c> with full content, cropped to its visible frame),
/// caption buttons included, as the Mac's shots show the title bar. Not <c>RenderTargetBitmap</c>: it draws XAML only,
/// so it would miss the composition visuals the character's parts move on and the bubble's shadow and dial. Every shot is
/// also checked: a window that didn't open, a PNG that wasn't written, or text that doesn't fit
/// (<see cref="LayoutCheck.TextProblems"/>) is a problem, written to <see cref="ProblemsFile"/> next to the shots.
/// </remarks>
public static partial class ScreenshotCapture
{
    public const string ModeVariable = "DESKLING_SCREENSHOT_MODE";
    public const string DirectoryVariable = "DESKLING_SCREENSHOT_DIR";

    /// <summary>The file <see cref="Finish"/> writes the run's problems to, next to the shots; absent when there were none.</summary>
    public const string ProblemsFile = "problems.txt";

    /// <summary>How long a window gets to lay out and draw before it is captured.</summary>
    public static readonly TimeSpan Settle = TimeSpan.FromSeconds(1.2);

    private static readonly List<string> Found = [];

    /// <summary>The language a run shoots in, an app's tag ("it"); unset runs in the language the app would choose.</summary>
    public const string LanguageVariable = "DESKLING_SCREENSHOT_LANGUAGE";

    public static bool IsEnabled => Environment.GetEnvironmentVariable(ModeVariable) is not null;

    /// <summary>
    /// The language asked for (<see cref="LanguageVariable"/>), else null. An app passes it to <c>Loc.UseLanguage</c>
    /// instead of <c>Loc.Choose</c>'s pick, so one machine shoots every language without changing its own.
    /// </summary>
    public static string? Language =>
        IsEnabled && Environment.GetEnvironmentVariable(LanguageVariable) is { Length: > 0 } language ? language : null;

    /// <summary>The shot name prefixes asked for; empty means all.</summary>
    public static IReadOnlyList<string> Wanted => WantedIn(Environment.GetEnvironmentVariable(ModeVariable));

    /// <summary>Whether <paramref name="shot"/> was asked for (by a prefix of its name).</summary>
    public static bool Wants(string shot) => Wants(shot, Environment.GetEnvironmentVariable(ModeVariable));

    /// <summary>Whether <paramref name="mode"/> (the variable's value: prefixes, or <c>all</c>) asks for <paramref name="shot"/>.</summary>
    public static bool Wants(string shot, string? mode) =>
        WantedIn(mode) is var wanted && (wanted.Count == 0 || wanted.Any(p => shot.StartsWith(p, StringComparison.Ordinal)));

    private static IReadOnlyList<string> WantedIn(string? mode) =>
        [.. (mode ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Where(p => p != "all")];

    public static string OutputDirectory =>
        Environment.GetEnvironmentVariable(DirectoryVariable) is { Length: > 0 } dir ? dir : Path.Combine(Path.GetTempPath(), "screenshots");

    /// <summary>What the run found wrong so far, one line each, starting with the shot's file name.</summary>
    public static IReadOnlyList<string> Problems => Found;

    /// <summary>
    /// The appearances a run shoots: light and dark, or the contrast theme alone while Windows is in one (an app can't
    /// turn a contrast theme on or off for itself, so run it again with one on for those shots).
    /// </summary>
    public static IReadOnlyList<(string Name, ElementTheme Theme)> Appearances =>
        CompanionPalette.ContrastThemeIsOn ? [("contrast", ElementTheme.Default)] : [("light", ElementTheme.Light), ("dark", ElementTheme.Dark)];

    /// <summary>Records a problem (once), for example a window the app expected to open and didn't, and prints it to standard error.</summary>
    public static void Report(string problem)
    {
        if (Found.Contains(problem))
            return;
        Found.Add(problem);
        Console.Error.WriteLine(problem);
    }

    /// <summary>Empties and makes <see cref="OutputDirectory"/>.</summary>
    public static void PrepareOutputDirectory()
    {
        try
        {
            if (Directory.Exists(OutputDirectory))
                Directory.Delete(OutputDirectory, recursive: true);
        }
        catch (IOException) { }
        Directory.CreateDirectory(OutputDirectory);
    }

    /// <summary>Writes the problems found (<see cref="ProblemsFile"/>), prints the output folder (the run script reads the last line) and quits.</summary>
    public static void Finish()
    {
        if (Found.Count > 0)
            File.WriteAllLines(Path.Combine(OutputDirectory, ProblemsFile), Found);
        Console.WriteLine(OutputDirectory);
        Application.Current.Exit();
    }

    /// <summary>Captures <paramref name="window"/> as the screen shows it, then checks that its text fits.</summary>
    public static void Capture(Window window, string path)
    {
        var shot = Path.GetFileName(path);
        var hwnd = Win32Interop.GetWindowFromWindowId(window.AppWindow.Id);
        if (!Write(hwnd, path))
            Report($"{shot}: the window couldn't be drawn");
        if (window.Content is UIElement root)
            foreach (var problem in LayoutCheck.TextProblems(root))
                Report($"{shot}: {problem}");
    }

    /// <summary>
    /// <paramref name="view"/> in a window built the way the app's are (content under a title bar on the look's paper),
    /// <paramref name="size"/> DIPs of content, in <paramref name="theme"/>: for states a real window can't be told to open in.
    /// </summary>
    public static async Task CaptureWindow(UIElement view, XamlSize size, ElementTheme theme, string path)
    {
        var root = new Grid { RequestedTheme = theme };
        LookBinding.Keep(root, () => root.Background = LookBinding.Brush(root, p => p.Paper));
        root.Children.Add(view);
        var window = new Window { Content = root, ExtendsContentIntoTitleBar = true };
        window.AppWindow.TitleBar.PreferredTheme = theme == ElementTheme.Dark ? TitleBarTheme.Dark : TitleBarTheme.Light;
        var scale = Scale(window);
        // Not ResizeClient, which adds a caption's height (3.3's outcome).
        Shell.NativeMethods.SetClientSize(window.AppWindow, new SizeInt32((int)Math.Round(size.Width * scale), (int)Math.Round(size.Height * scale)));
        window.Activate();
        await Task.Delay(Settle);
        Capture(window, path);
        window.Close();
    }

    /// <summary><paramref name="view"/> on its own at its fitting size (the panel, the desktop character), on the look's paper, in <paramref name="theme"/>.</summary>
    public static async Task CaptureFitting(FrameworkElement view, ElementTheme theme, string path)
    {
        var root = new Grid { RequestedTheme = theme, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        LookBinding.Keep(root, () => root.Background = LookBinding.Brush(root, p => p.Paper));
        root.Children.Add(view);
        var window = new Window { Content = root };
        // The context-menu presenter, as the desktop window's: a popup with no frame, so no border edges the shot.
        var presenter = OverlappedPresenter.CreateForContextMenu();
        presenter.SetBorderAndTitleBar(false, false);
        window.AppWindow.SetPresenter(presenter);
        var hwnd = Win32Interop.GetWindowFromWindowId(window.AppWindow.Id);
        Desktop.DesktopNative.DwmSetWindowAttribute(hwnd, Desktop.DesktopNative.DWMWA_WINDOW_CORNER_PREFERENCE, Desktop.DesktopNative.DWMWCP_DONOTROUND, sizeof(uint));
        Desktop.DesktopNative.DwmSetWindowAttribute(hwnd, Desktop.DesktopNative.DWMWA_BORDER_COLOR, Desktop.DesktopNative.DWMWA_COLOR_NONE, sizeof(uint));
        var scale = Scale(window);
        void Fit()
        {
            root.Measure(new XamlSize(double.PositiveInfinity, double.PositiveInfinity));
            var fit = root.DesiredSize;
            Shell.NativeMethods.SetClientSize(window.AppWindow, new SizeInt32((int)Math.Ceiling(fit.Width * scale), (int)Math.Ceiling(fit.Height * scale)));
        }
        Fit();
        window.Activate();
        await Task.Delay(Settle);
        // What x:Bind sets arrives once the view has loaded, so it can be taller now than when it was first measured.
        Fit();
        await Task.Delay(TimeSpan.FromSeconds(0.3));
        Capture(window, path);
        window.Close();
    }

    private static double Scale(Window window)
    {
        var dpi = GetDpiForWindow(Win32Interop.GetWindowFromWindowId(window.AppWindow.Id));
        return dpi == 0 ? 1 : dpi / 96.0;
    }

    /// <summary>The window's pixels as the screen composes them, cropped to its visible frame (no resize border), as a PNG.</summary>
    private static bool Write(nint hwnd, string path)
    {
        if (hwnd == 0 || !GetWindowRect(hwnd, out var outer))
            return false;
        if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out var visible, Marshal.SizeOf<RECT>()) != 0)
            visible = outer;
        var (width, height) = (outer.Right - outer.Left, outer.Bottom - outer.Top);
        if (width <= 0 || height <= 0)
            return false;
        try
        {
            using var full = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(full))
            {
                var hdc = graphics.GetHdc();
                try
                {
                    if (!PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT))
                        return false;
                }
                finally
                {
                    graphics.ReleaseHdc(hdc);
                }
            }
            var crop = new Rectangle(visible.Left - outer.Left, visible.Top - outer.Top, visible.Right - visible.Left, visible.Bottom - visible.Top);
            crop.Intersect(new Rectangle(0, 0, width, height));
            using var image = full.Clone(crop, PixelFormat.Format32bppArgb);
            image.Save(path, ImageFormat.Png);
            return true;
        }
        catch (Exception e) when (e is ExternalException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            Report($"{Path.GetFileName(path)}: not written ({e.Message})");
            return true;
        }
    }

    private const uint PW_RENDERFULLCONTENT = 2;
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PrintWindow(nint hwnd, nint hdc, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint hwnd, out RECT rect);

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint hwnd);

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmGetWindowAttribute(nint hwnd, int attribute, out RECT value, int size);
}
