using Deskling.Companion.Shell.App;
using Deskling.Windows.Companion.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace Deskling.Windows.Companion.Shell;

/// <summary>
/// The app's windows by id (<see cref="IWindowHost{TWindow}"/>, Swift's <c>WindowManager</c>), so they can be opened
/// from anywhere (the tray, a notification, the hotkey). The app supplies each window's <see cref="WindowSpec"/> and
/// content. A window's content reaches under its title bar (the Mac's transparent title bar), so the content leaves
/// room for the caption buttons; it is sized in DIPs on the display it opens on. The app has no taskbar button while
/// idle; each open window has its own. UI thread only.
/// </summary>
public sealed class WindowManager<TWindow> : IWindowHost<TWindow>
    where TWindow : notnull
{
    private readonly Dictionary<TWindow, Entry> windows = [];
    private readonly Func<TWindow, WindowSpec> spec;
    private readonly Func<TWindow, UIElement> content;
    private readonly string? iconPath;
    private ElementTheme theme = ElementTheme.Default;

    /// <param name="spec">Each window's title, size and traits.</param>
    /// <param name="content">Each window's view, made when it opens; disposed when it closes if it is <see cref="IDisposable"/>.</param>
    /// <param name="iconPath">The <c>.ico</c> for the title bar and taskbar button; the executable's icon by default.</param>
    public WindowManager(Func<TWindow, WindowSpec> spec, Func<TWindow, UIElement> content, string? iconPath = null)
    {
        this.spec = spec;
        this.content = content;
        this.iconPath = iconPath;
    }

    public Action<TWindow>? OnClose { get; set; }

    /// <summary>Light, dark or Windows' app mode (<see cref="ElementTheme.Default"/>), for every open window and new ones.</summary>
    public ElementTheme Theme
    {
        get => theme;
        set
        {
            theme = value;
            foreach (var entry in windows.Values)
                ApplyTheme(entry);
        }
    }

    public bool IsOpen(TWindow id) => windows.ContainsKey(id);

    /// <summary>The open window, for the odd call this class doesn't cover.</summary>
    public Window? Find(TWindow id) => windows.TryGetValue(id, out var entry) ? entry.Window : null;

    public bool ShowOpenWindows()
    {
        if (windows.Count == 0)
            return false;
        foreach (var entry in windows.Values.ToArray())
            BringToFront(entry);
        return true;
    }

    public void Show(TWindow id)
    {
        if (windows.TryGetValue(id, out var existing))
        {
            CloseOtherExclusive(id);
            BringToFront(existing);
            return;
        }
        var entry = Make(id);
        windows[id] = entry;
        CloseOtherExclusive(id);
        BringToFront(entry);
    }

    public void Close(TWindow id)
    {
        if (windows.TryGetValue(id, out var entry))
            entry.Window.Close();
    }

    public void CloseAll()
    {
        foreach (var entry in windows.Values.ToArray())
            entry.Window.Close();
    }

    private Entry Make(TWindow id)
    {
        var spec = this.spec(id);
        var view = content(id);
        var root = new Grid();
        // The look's paper behind the content and under the title bar, as the Mac's window background.
        LookBinding.Keep(root, () => root.Background = LookBinding.Brush(root, p => p.Paper));
        root.Children.Add(view);
        var window = new Window
        {
            Title = spec.Title,
            Content = root,
            ExtendsContentIntoTitleBar = true,
        };
        var presenter = OverlappedPresenter.Create();
        presenter.IsMaximizable = false;
        presenter.IsAlwaysOnTop = spec.IsAlwaysOnTop;
        window.AppWindow.SetPresenter(presenter);
        if (iconPath is not null)
            window.AppWindow.SetIcon(iconPath);
        var entry = new Entry(window, presenter, spec, root);
        ApplyTheme(entry);
        Place(entry);
        // Moving to a display with another scale keeps the content's size in DIPs; the minimum follows it.
        void Loaded(object sender, RoutedEventArgs args)
        {
            root.Loaded -= Loaded;
            root.XamlRoot.Changed += (_, _) => ApplyMinSize(entry);
        }
        root.Loaded += Loaded;
        window.Closed += (_, _) =>
        {
            windows.Remove(id);
            // The content is the manager's to end: a view's Unloaded isn't reliably raised when its window closes.
            try
            {
                (view as IDisposable)?.Dispose();
            }
            finally
            {
                OnClose?.Invoke(id);
            }
        };
        return entry;
    }

    /// <summary>
    /// Sizes the content at <see cref="WindowSpec.Size"/> on the display it opens on (never past the work area, where it
    /// scrolls instead) and puts it in the middle or by the tray.
    /// </summary>
    private void Place(Entry entry)
    {
        var (cursorX, cursorY) = NativeMethods.Cursor();
        var display =
            entry.Spec.Placement == WindowPlacement.NearTray
                ? DisplayArea.Primary
                : DisplayArea.GetFromPoint(new PointInt32(cursorX, cursorY), DisplayAreaFallback.Primary);
        var area = display.WorkArea;
        var scale = NativeMethods.Scale(area.X + area.Width / 2, area.Y + area.Height / 2);
        var appWindow = entry.Window.AppWindow;
        // On the display first, so the client size below is measured with its frame.
        appWindow.Move(new PointInt32(area.X, area.Y));
        NativeMethods.SetClientSize(
            appWindow,
            new SizeInt32(
                Math.Min((int)Math.Round(entry.Spec.Size.Width * scale), area.Width),
                Math.Min((int)Math.Round(entry.Spec.Size.Height * scale), area.Height)
            )
        );
        var size = appWindow.Size;
        var origin =
            entry.Spec.Placement == WindowPlacement.NearTray
                ? TrayPlacement.Corner(area, display.OuterBounds, size, (int)Math.Round(12 * scale))
                : new PointInt32(area.X + (area.Width - size.Width) / 2, area.Y + (area.Height - size.Height) / 2);
        appWindow.Move(origin);
        ApplyMinSize(entry, scale);
    }

    private static void ApplyMinSize(Entry entry, double? scale = null)
    {
        if (entry.Spec.MinSize is not { } min)
            return;
        var factor = scale ?? entry.Root.XamlRoot?.RasterizationScale ?? 1;
        entry.Presenter.PreferredMinimumWidth = (int)Math.Round(min.Width * factor);
        entry.Presenter.PreferredMinimumHeight = (int)Math.Round(min.Height * factor);
    }

    private void ApplyTheme(Entry entry)
    {
        entry.Root.RequestedTheme = theme;
        entry.Window.AppWindow.TitleBar.PreferredTheme = theme switch
        {
            ElementTheme.Light => TitleBarTheme.Light,
            ElementTheme.Dark => TitleBarTheme.Dark,
            _ => TitleBarTheme.UseDefaultAppMode,
        };
    }

    private void CloseOtherExclusive(TWindow id)
    {
        if (!windows[id].Spec.IsExclusive)
            return;
        foreach (var (other, entry) in windows.ToArray())
            if (!EqualityComparer<TWindow>.Default.Equals(other, id) && entry.Spec.IsExclusive)
                entry.Window.Close();
    }

    private static void BringToFront(Entry entry)
    {
        if (entry.Presenter.State == OverlappedPresenterState.Minimized)
            entry.Presenter.Restore();
        entry.Window.Activate();
        // A tray app isn't the foreground app, so Windows would only flash the taskbar button.
        NativeMethods.SetForegroundWindow(Win32Interop.GetWindowFromWindowId(entry.Window.AppWindow.Id));
    }

    private sealed record Entry(Window Window, OverlappedPresenter Presenter, WindowSpec Spec, Grid Root);
}
