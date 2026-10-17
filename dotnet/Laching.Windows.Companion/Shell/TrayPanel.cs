using Laching.Windows.Companion.Views;
using Laching.Windows.Shell;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;
using Windows.System;
using Win32 = Laching.Windows.Shell.NativeMethods;
using XamlSize = Windows.Foundation.Size;

namespace Laching.Windows.Companion.Shell;

/// <summary>
/// The tray icon's panel (the Mac's transient popover): a borderless, always-on-top window without a taskbar button,
/// next to the icon and sized to its content, which closes when it loses focus or on Esc. Made the first time it
/// opens and hidden, not closed, after that, so the content (and its bindings) lives as long as the app. Every open puts
/// the focus on the content's first focusable element (its first row), so a quick Enter never runs the row used last
/// (Quit, say), and the arrow keys move between the rows, as in Windows' own flyouts. Its window takes
/// <paramref name="title"/> (the tray icon's tooltip), which Narrator says when it opens. UI thread only.
/// </summary>
internal sealed class TrayPanel(Func<UIElement> makeContent, Func<RectInt32?> iconRect, string title) : IDisposable
{
    /// <summary>
    /// A click on the icon while the panel is open first takes the focus from it (closing it), then arrives as a click
    /// that would open it again: within this long of closing that way, the click only closes it, as Windows' own do.
    /// </summary>
    private const long ReopenGuardMilliseconds = 500;

    private Window? window;
    private Canvas? root;
    private FrameworkElement? content;
    private ElementTheme theme = ElementTheme.Default;
    private long deactivatedAt = long.MinValue / 2;
    private PointInt32? lastAnchor;
    private FocusState? focusOnOpen;
    private bool disposed;

    public bool IsOpen => window?.AppWindow.IsVisible == true;

    public ElementTheme Theme
    {
        get => theme;
        set
        {
            theme = value;
            if (root is not null)
                root.RequestedTheme = value;
        }
    }

    /// <summary>
    /// A click on the icon (or Enter on it, <paramref name="byKeyboard"/>): opens the panel next to
    /// <paramref name="anchor"/>'s icon, or closes it.
    /// </summary>
    public void Toggle(PointInt32 anchor, bool byKeyboard = false)
    {
        if (IsOpen)
            Close();
        else if (Environment.TickCount64 - deactivatedAt > ReopenGuardMilliseconds)
            Open(anchor, byKeyboard);
    }

    /// <summary>
    /// Opens the panel next to the icon with its first row focused: showing the keyboard's focus visual when
    /// <paramref name="byKeyboard"/>, not for a click on the icon, and as WinUI decides when the app opens it.
    /// </summary>
    public void Open(PointInt32? anchor = null, bool byKeyboard = false)
    {
        if (disposed || IsOpen)
            return;
        var made = Make();
        Place(made, anchor);
        focusOnOpen =
            byKeyboard ? FocusState.Keyboard
            : anchor is null ? FocusState.Programmatic
            : FocusState.Pointer;
        made.Activate();
        // The click on the icon lets this process take the foreground.
        Win32.SetForegroundWindow(Win32Interop.GetWindowFromWindowId(made.AppWindow.Id));
    }

    public void Close() => window?.AppWindow.Hide();

    public void Dispose()
    {
        disposed = true;
        window?.Close();
        window = null;
    }

    private Window Make()
    {
        if (window is not null)
            return window;
        content = makeContent() as FrameworkElement ?? throw new InvalidOperationException("The panel must be a FrameworkElement.");
        // A canvas measures its child without a limit, so the child keeps the size it asks for and the window follows it.
        var canvas = new Canvas { RequestedTheme = theme };
        LookBinding.Keep(canvas, () => canvas.Background = LookBinding.Brush(canvas, p => p.Paper));
        root = canvas;
        root.Children.Add(content);
        // Up and Down move between the rows, as Tab does.
        root.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
        // Esc is a convenience, not something to show as a tooltip floating above the panel.
        root.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;
        var accelerator = new KeyboardAccelerator { Key = VirtualKey.Escape };
        accelerator.Invoked += (_, args) =>
        {
            args.Handled = true;
            Close();
        };
        root.KeyboardAccelerators.Add(accelerator);
        content.SizeChanged += (_, _) =>
        {
            if (IsOpen)
                Resize();
        };
        // The first open's content loads after the window activates.
        content.Loaded += (_, _) => FocusFirst();

        var made = new Window { Content = root, Title = title };
        var presenter = OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);
        presenter.IsResizable = presenter.IsMaximizable = presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        made.AppWindow.SetPresenter(presenter);
        made.AppWindow.IsShownInSwitchers = false;
        made.Activated += (_, args) =>
        {
            if (args.WindowActivationState != WindowActivationState.Deactivated)
            {
                FocusFirst();
                return;
            }
            if (!IsOpen)
                return;
            deactivatedAt = Environment.TickCount64;
            Close();
        };
        made.Closed += (_, args) =>
        {
            // Alt+F4 hides it like any other way of closing; only Dispose ends it.
            if (disposed)
                return;
            args.Handled = true;
            Close();
        };
        window = made;
        return made;
    }

    /// <summary>
    /// The open's focus on the content's first focusable element, once the content is loaded and the window active.
    /// Queued, so it comes after WinUI puts back the element that had the focus when the panel last closed.
    /// </summary>
    private void FocusFirst()
    {
        if (focusOnOpen is null || content is not { IsLoaded: true } loaded)
            return;
        loaded.DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            () =>
            {
                if (focusOnOpen is not { } state || !IsOpen)
                    return;
                if (XamlCast.As<UIElement>(FocusManager.FindFirstFocusableElement(loaded)) is { } first && first.Focus(state))
                    focusOnOpen = null;
            }
        );
    }

    /// <summary>Next to the icon (or the point clicked, when the icon can't be found) on the icon's display, at the content's size.</summary>
    private void Place(Window made, PointInt32? anchor)
    {
        lastAnchor = anchor;
        var icon = iconRect() ?? (anchor is { } point ? new RectInt32(point.X, point.Y, 0, 0) : (RectInt32?)null);
        var (cursorX, cursorY) = NativeMethods.Cursor();
        var center = icon is { } rect
            ? new PointInt32(rect.X + rect.Width / 2, rect.Y + rect.Height / 2)
            : new PointInt32(cursorX, cursorY);
        var display = DisplayArea.GetFromPoint(center, DisplayAreaFallback.Primary);
        var area = display.WorkArea;
        var scale = NativeMethods.Scale(center.X, center.Y);
        made.AppWindow.Move(new PointInt32(area.X, area.Y));
        NativeMethods.SetClientSize(made.AppWindow, ClientSize(scale));
        var origin = TrayPlacement.NextTo(
            icon ?? new RectInt32(center.X, center.Y, 0, 0),
            area,
            display.OuterBounds,
            made.AppWindow.Size,
            (int)Math.Round(12 * scale)
        );
        made.AppWindow.Move(origin);
    }

    /// <summary>The content grew or shrank while open: the window follows, still next to the icon.</summary>
    private void Resize()
    {
        if (window is not null)
            Place(window, lastAnchor);
    }

    private SizeInt32 ClientSize(double scale)
    {
        content!.Measure(new XamlSize(double.PositiveInfinity, double.PositiveInfinity));
        var desired = content.DesiredSize;
        return new SizeInt32(
            Math.Max(1, (int)Math.Ceiling(desired.Width * scale)),
            Math.Max(1, (int)Math.Ceiling(desired.Height * scale))
        );
    }
}
