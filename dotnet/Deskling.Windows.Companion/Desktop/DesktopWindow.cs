using Deskling.Companion.Shell.Desktop;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using FigurePoint = Deskling.Companion.Figures.Point;
using FigureRect = Deskling.Companion.Figures.Rect;
using FigureSize = Deskling.Companion.Figures.Size;
using ShellNative = Deskling.Windows.Companion.Shell.NativeMethods;
using XamlPoint = Windows.Foundation.Point;

namespace Deskling.Windows.Companion.Desktop;

/// <summary>
/// A small window floating above other apps' windows, for a desk character and its speech bubble (Swift's
/// <c>FloatingWindow</c>), as 3.1's spike found it: a WinUI window with a fully transparent backdrop, layered, topmost,
/// never activated, in neither the taskbar nor Alt+Tab, so the app in front keeps the keyboard. Clicks pass through
/// everywhere but where something is drawn (<see cref="HitTest"/>): a poll off the UI thread watches the pointer and
/// turns the window's click-through off only while the pointer is over a drawn pixel.
/// </summary>
/// <remarks>
/// It stands on its <see cref="Anchor"/>, the middle of its bottom edge in screen pixels (y down), so
/// <see cref="SetSize"/> grows it upward in place; its size is in DIPs, at the scale of the display it stands on. It stays
/// wholly on a display's work area (<see cref="FloatingPlacement.Clamped"/>): a drag ends inside one, and when displays
/// change it moves onto one, returning to the anchor once that fits again. A click (under 3 DIPs of movement) calls
/// <see cref="OnClick"/>; a drag moves it and reports the new anchor; a right click calls <see cref="OnMenu"/>;
/// Ctrl+wheel (which a precision touchpad's pinch sends) and a touch pinch report one step per gesture. A second window
/// can ride along (<see cref="AttachTo"/>, for the bubble): Windows keeps it above its owner, and the app places it with
/// <see cref="SetFrame"/>. UI thread only.
/// </remarks>
public sealed class DesktopWindow : IDisposable
{
    public const double FadeInSeconds = 0.4;
    public const double FadeOutSeconds = 0.2;

    /// <summary>How far the pointer moves, in DIPs, before a press is a drag rather than a click.</summary>
    public const double DragThreshold = 3;

    /// <summary>The wheel turns one notch (120) per step; a touch pinch must scale this much.</summary>
    private const int WheelStep = 120;
    private const double PinchStep = 0.15;

    /// <summary>
    /// How often the pointer is looked at for click-through, off the UI thread: often while it is near the window, so
    /// the window takes a click the moment the pointer reaches the character, and less often while it is far.
    /// </summary>
    private const int PollNearMilliseconds = 33;
    private const int PollFarMilliseconds = 100;

    /// <summary>How far from the window, in pixels, the pointer counts as near.</summary>
    private const int NearPixels = 160;

    private readonly Window window;
    private readonly Grid root = new() { Background = new SolidColorBrush(Colors.Transparent) };
    private readonly DispatcherQueue queue;
    private readonly Timer poll;
    private readonly DisplayAreaWatcher displays;
    private DesktopWindow? parent;
    private PointInt32 anchor;
    private FigureSize size;
    private double placedScale;
    private int generation;
    private bool displaysEnumerated;

    // Set on the UI thread, read by the poll.
    private volatile bool polling;
    private volatile bool pressed;
    private volatile bool clickThrough;
    private volatile bool hitPending;

    private DesktopNative.POINT pressStart;
    private RectInt32 frameAtPress;
    private bool dragging;
    private int wheel;
    private bool wheelStepped;
    private DispatcherQueueTimer? wheelEnd;
    private bool pinched;

    /// <param name="content">What the window shows; it fills the window.</param>
    /// <param name="size">The window's size in DIPs.</param>
    /// <param name="anchor">The middle of its bottom edge, in screen pixels.</param>
    /// <param name="isMovable">Whether a drag moves it; off for a window riding along beside another.</param>
    public DesktopWindow(UIElement content, FigureSize size, PointInt32 anchor, bool isMovable = true)
    {
        queue = DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("Make a desktop window on the UI thread.");
        this.size = size;
        this.anchor = anchor;
        IsMovable = isMovable;
        root.Children.Add(content);
        window = new Window { Content = root, SystemBackdrop = new TransparentBackdrop() };
        // The context-menu presenter is the one whose popup style sticks: WS_POPUP and nothing else, so no frame.
        var presenter = OverlappedPresenter.CreateForContextMenu();
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = presenter.IsMaximizable = presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        window.AppWindow.SetPresenter(presenter);
        window.AppWindow.IsShownInSwitchers = false;
        Hwnd = Win32Interop.GetWindowFromWindowId(window.AppWindow.Id);
        var ex = DesktopNative.GetWindowLongPtr(Hwnd, DesktopNative.GWL_EXSTYLE);
        ex |= (nint)(DesktopNative.WS_EX_LAYERED | DesktopNative.WS_EX_TOOLWINDOW | DesktopNative.WS_EX_NOACTIVATE);
        ex &= ~(nint)DesktopNative.WS_EX_APPWINDOW;
        DesktopNative.SetWindowLongPtr(Hwnd, DesktopNative.GWL_EXSTYLE, ex);
        // No Windows 11 rounded corners or accent border around a character.
        DesktopNative.DwmSetWindowAttribute(Hwnd, DesktopNative.DWMWA_WINDOW_CORNER_PREFERENCE, DesktopNative.DWMWCP_DONOTROUND, sizeof(uint));
        DesktopNative.DwmSetWindowAttribute(Hwnd, DesktopNative.DWMWA_BORDER_COLOR, DesktopNative.DWMWA_COLOR_NONE, sizeof(uint));
        SetClickThrough(true);

        root.PointerPressed += OnPressed;
        root.PointerMoved += OnMoved;
        root.PointerReleased += OnReleased;
        root.PointerCaptureLost += (_, _) => EndPress();
        root.RightTapped += (_, e) =>
        {
            e.Handled = true;
            OnMenu?.Invoke();
        };
        root.PointerWheelChanged += OnWheel;
        root.ManipulationMode = ManipulationModes.Scale;
        root.ManipulationStarted += (_, _) => pinched = false;
        root.ManipulationDelta += OnPinchDelta;
        root.Loaded += (_, _) => root.XamlRoot.Changed += (_, _) => ScaleMaybeChanged();

        poll = new Timer(_ => Poll(), null, Timeout.Infinite, Timeout.Infinite);
        displays = DisplayArea.CreateWatcher();
        displays.EnumerationCompleted += (_, _) => queue.TryEnqueue(() => displaysEnumerated = true);
        displays.Added += (_, _) => queue.TryEnqueue(ScreensChanged);
        displays.Removed += (_, _) => queue.TryEnqueue(ScreensChanged);
        displays.Updated += (_, _) => queue.TryEnqueue(ScreensChanged);
        displays.Start();
        Place();
    }

    /// <summary>A click without a drag.</summary>
    public Action? OnClick { get; set; }

    /// <summary>The window moves under a drag (each step), for a window riding along to follow.</summary>
    public Action? OnDragging { get; set; }

    /// <summary>A drag ended; the new anchor, already on a display.</summary>
    public Action<PointInt32>? OnDragEnded { get; set; }

    /// <summary>A pinch or Ctrl+wheel: +1 bigger, -1 smaller, once per gesture.</summary>
    public Action<int>? OnPinch { get; set; }

    /// <summary>A right click: show the menu.</summary>
    public Action? OnMenu { get; set; }

    /// <summary>The displays changed (one was added, removed or rearranged), after the window moved onto one.</summary>
    public Action? OnScreensChanged { get; set; }

    /// <summary>
    /// Whether something is drawn at a point in the content (DIPs): only there do clicks land on the window. Null takes
    /// any element XAML finds there.
    /// </summary>
    public Func<XamlPoint, bool>? HitTest { get; set; }

    public bool IsMovable { get; set; }

    public bool IsShown { get; private set; }

    public nint Hwnd { get; }

    /// <summary>The window's title, which Narrator reads for it; WinUI's until set. Never shown: the window has no title bar.</summary>
    public string Title
    {
        get => window.Title;
        set => window.Title = value;
    }

    /// <summary>The content's root: the window's background, for the app's theme.</summary>
    public FrameworkElement Root => root;

    /// <summary>Where the window stands (screen pixels). Setting it moves the window (onto a display).</summary>
    public PointInt32 Anchor
    {
        get => anchor;
        set
        {
            anchor = value;
            Place();
        }
    }

    /// <summary>The window's size in DIPs.</summary>
    public FigureSize Size => size;

    /// <summary>The window's frame on screen right now, in pixels.</summary>
    public RectInt32 Frame
    {
        get
        {
            DesktopNative.GetWindowRect(Hwnd, out var r);
            return new RectInt32(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        }
    }

    /// <summary>The scale of the display the window is on (1 at 100 %).</summary>
    public double Scale => ShellNative.Scale(Hwnd);

    /// <summary>The work area (without the taskbar) of the display the window is on, in pixels.</summary>
    public RectInt32 WorkArea => DisplayArea.GetFromWindowId(window.AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;

    /// <summary>Every display's work area, in pixels.</summary>
    public static IReadOnlyList<FigureRect> WorkAreas()
    {
        // By index: enumerating the projected list throws InvalidCastException (a known Windows App SDK projection bug).
        var displays = DisplayArea.FindAll();
        var areas = new List<FigureRect>(displays.Count);
        for (var i = 0; i < displays.Count; i++)
            areas.Add(Rect(displays[i].WorkArea));
        return areas;
    }

    public void SetContent(UIElement content)
    {
        root.Children.Clear();
        root.Children.Add(content);
    }

    /// <summary>Resizes around the anchor: the bottom middle stays put.</summary>
    public void SetSize(FigureSize size)
    {
        this.size = size;
        Place();
    }

    /// <summary>Puts a riding-along window exactly at <paramref name="frame"/> (pixels), without clamping.</summary>
    public void SetFrame(RectInt32 frame)
    {
        var scale = ShellNative.Scale(frame.X + frame.Width / 2, frame.Y + frame.Height / 2);
        size = new FigureSize(frame.Width / scale, frame.Height / scale);
        window.AppWindow.MoveAndResize(frame);
    }

    /// <summary>Rides along with <paramref name="owner"/> from now on: Windows keeps it above, and the app places it.</summary>
    public void AttachTo(DesktopWindow owner)
    {
        parent = owner;
        DesktopNative.SetWindowLongPtr(Hwnd, DesktopNative.GWLP_HWNDPARENT, owner.Hwnd);
    }

    /// <summary>Shows the window without activating it, fading in unless <paramref name="animated"/> is false.</summary>
    public void Show(bool animated)
    {
        generation++;
        var wasVisible = window.AppWindow.IsVisible;
        IsShown = true;
        if (parent is null)
            Place();
        var visual = ElementCompositionPreview.GetElementVisual(root);
        visual.StopAnimation(nameof(Visual.Opacity));
        if (animated && !wasVisible)
        {
            visual.Opacity = 0;
            window.AppWindow.Show(activateWindow: false);
            Fade(visual, 1, FadeInSeconds, null);
        }
        else
        {
            visual.Opacity = 1;
            window.AppWindow.Show(activateWindow: false);
        }
        polling = true;
        poll.Change(0, Timeout.Infinite);
    }

    /// <summary>Fades the window out (unless <paramref name="animated"/> is false), then hides it.</summary>
    public void Hide(bool animated)
    {
        if (!IsShown)
            return;
        IsShown = false;
        generation++;
        var current = generation;
        polling = false;
        poll.Change(Timeout.Infinite, Timeout.Infinite);
        SetClickThrough(true);
        var visual = ElementCompositionPreview.GetElementVisual(root);
        if (!animated || !window.AppWindow.IsVisible)
        {
            window.AppWindow.Hide();
            return;
        }
        Fade(
            visual,
            0,
            FadeOutSeconds,
            () =>
            {
                if (generation != current)
                    return;
                window.AppWindow.Hide();
                visual.Opacity = 1;
            }
        );
    }

    public void Dispose()
    {
        poll.Dispose();
        displays.Stop();
        wheelEnd?.Stop();
        window.Close();
    }

    // Placement

    private void Place()
    {
        if (parent is not null)
            return;
        var scale = ShellNative.Scale(anchor.X, anchor.Y);
        placedScale = scale;
        var pixels = new FigureSize(Math.Round(size.Width * scale), Math.Round(size.Height * scale));
        var target = FloatingPlacement.Clamped(new FigurePoint(anchor.X, anchor.Y), pixels, WorkAreas());
        var frame = FloatingPlacement.Frame(target, pixels);
        window.AppWindow.MoveAndResize(Int(frame));
    }

    private void ScaleMaybeChanged()
    {
        if (parent is null && Math.Abs(ShellNative.Scale(anchor.X, anchor.Y) - placedScale) > 0.001)
            Place();
    }

    private void ScreensChanged()
    {
        if (!displaysEnumerated)
            return;
        Place();
        OnScreensChanged?.Invoke();
    }

    // Click-through: the pointer is read off the UI thread; the UI thread wakes only while it is over the window.

    private void Poll()
    {
        DesktopNative.GetCursorPos(out var point);
        DesktopNative.GetWindowRect(Hwnd, out var rect);
        var inside = point.X >= rect.Left && point.X < rect.Right && point.Y >= rect.Top && point.Y < rect.Bottom;
        var near =
            point.X >= rect.Left - NearPixels && point.X < rect.Right + NearPixels && point.Y >= rect.Top - NearPixels && point.Y < rect.Bottom + NearPixels;
        // One-shot, re-armed each time: never two polls at once, and none after a hide.
        if (polling)
            poll.Change(near || pressed ? PollNearMilliseconds : PollFarMilliseconds, Timeout.Infinite);
        if (!inside && !pressed)
        {
            if (!clickThrough)
                queue.TryEnqueue(() => SetClickThrough(true));
            return;
        }
        if (hitPending)
            return;
        hitPending = true;
        queue.TryEnqueue(() =>
        {
            hitPending = false;
            Hit();
        });
    }

    private void Hit()
    {
        if (!IsShown)
            return;
        DesktopNative.GetCursorPos(out var point);
        DesktopNative.GetWindowRect(Hwnd, out var rect);
        var inside = point.X >= rect.Left && point.X < rect.Right && point.Y >= rect.Top && point.Y < rect.Bottom;
        var scale = Scale;
        var local = new XamlPoint((point.X - rect.Left) / scale, (point.Y - rect.Top) / scale);
        SetClickThrough(!(pressed || (inside && IsDrawn(local))));
    }

    private bool IsDrawn(XamlPoint point)
    {
        if (HitTest is { } test)
            return test(point);
        if (root.XamlRoot is null)
            return false;
        return VisualTreeHelper.FindElementsInHostCoordinates(point, root).Any(e => !ReferenceEquals(e, root));
    }

    private void SetClickThrough(bool on)
    {
        if (on == clickThrough && on == HasTransparentStyle())
            return;
        clickThrough = on;
        var ex = DesktopNative.GetWindowLongPtr(Hwnd, DesktopNative.GWL_EXSTYLE);
        ex = on ? ex | (nint)DesktopNative.WS_EX_TRANSPARENT : ex & ~(nint)DesktopNative.WS_EX_TRANSPARENT;
        DesktopNative.SetWindowLongPtr(Hwnd, DesktopNative.GWL_EXSTYLE, ex);
    }

    private bool HasTransparentStyle() => (DesktopNative.GetWindowLongPtr(Hwnd, DesktopNative.GWL_EXSTYLE) & (nint)DesktopNative.WS_EX_TRANSPARENT) != 0;

    // Pointer: clicks, drags, the menu, pinches

    private void OnPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(root);
        if (point.Properties.IsRightButtonPressed || (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse && !point.Properties.IsLeftButtonPressed))
            return;
        DesktopNative.GetCursorPos(out pressStart);
        frameAtPress = Frame;
        pressed = true;
        dragging = false;
        root.CapturePointer(e.Pointer);
    }

    private void OnMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!pressed || !IsMovable)
            return;
        DesktopNative.GetCursorPos(out var now);
        var (dx, dy) = (now.X - pressStart.X, now.Y - pressStart.Y);
        if (!dragging && Math.Sqrt(dx * dx + dy * dy) < DragThreshold * Scale)
            return;
        dragging = true;
        window.AppWindow.Move(new PointInt32(frameAtPress.X + dx, frameAtPress.Y + dy));
        OnDragging?.Invoke();
    }

    private void OnReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!pressed)
            return;
        // Read first: releasing the capture raises PointerCaptureLost at once, which ends the press.
        var wasDragging = dragging;
        pressed = dragging = false;
        root.ReleasePointerCapture(e.Pointer);
        if (wasDragging)
            DragEnded();
        else
            OnClick?.Invoke();
    }

    /// <summary>The capture went elsewhere mid-press: a drag ends where it is, a click doesn't happen.</summary>
    private void EndPress()
    {
        var wasDragging = dragging;
        pressed = dragging = false;
        if (wasDragging)
            DragEnded();
    }

    private void DragEnded()
    {
        var dropped = FloatingPlacement.Anchor(Rect(Frame));
        var scale = ShellNative.Scale((int)dropped.X, (int)dropped.Y);
        var pixels = new FigureSize(Math.Round(size.Width * scale), Math.Round(size.Height * scale));
        var clamped = FloatingPlacement.Clamped(dropped, pixels, WorkAreas());
        anchor = new PointInt32((int)Math.Round(clamped.X), (int)Math.Round(clamped.Y));
        Place();
        OnDragEnded?.Invoke(anchor);
    }

    private void OnWheel(object sender, PointerRoutedEventArgs e)
    {
        if ((e.KeyModifiers & global::Windows.System.VirtualKeyModifiers.Control) == 0)
            return;
        e.Handled = true;
        wheel += e.GetCurrentPoint(root).Properties.MouseWheelDelta;
        if (!wheelStepped && Math.Abs(wheel) >= WheelStep)
        {
            wheelStepped = true;
            OnPinch?.Invoke(wheel > 0 ? 1 : -1);
        }
        // A gesture ends when the wheel rests for a moment.
        wheelEnd ??= MakeWheelEnd();
        wheelEnd.Stop();
        wheelEnd.Start();
    }

    private DispatcherQueueTimer MakeWheelEnd()
    {
        var timer = queue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(400);
        timer.IsRepeating = false;
        timer.Tick += (_, _) => (wheel, wheelStepped) = (0, false);
        return timer;
    }

    private void OnPinchDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        var scale = e.Cumulative.Scale;
        if (pinched || Math.Abs(scale - 1) < PinchStep)
            return;
        pinched = true;
        OnPinch?.Invoke(scale > 1 ? 1 : -1);
    }

    private static void Fade(Visual visual, float to, double seconds, Action? completed)
    {
        var compositor = visual.Compositor;
        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        var fade = compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(1, to);
        fade.Duration = TimeSpan.FromSeconds(seconds);
        visual.StartAnimation(nameof(Visual.Opacity), fade);
        batch.End();
        if (completed is not null)
            batch.Completed += (_, _) => completed();
    }

    private static FigureRect Rect(RectInt32 r) => new(r.X, r.Y, r.Width, r.Height);

    private static RectInt32 Int(FigureRect r) =>
        new((int)Math.Round(r.Origin.X), (int)Math.Round(r.Origin.Y), (int)Math.Round(r.Size.Width), (int)Math.Round(r.Size.Height));
}

/// <summary>
/// A system backdrop that is a fully transparent brush, so only the content draws (with the window's
/// <c>WS_EX_LAYERED</c>, its per-pixel alpha reaches the desktop). The system compositor needs a
/// <c>Windows.System.DispatcherQueue</c>, which WinUI's UI thread lacks: one is made the first time.
/// </summary>
internal sealed partial class TransparentBackdrop : SystemBackdrop
{
    private static global::Windows.UI.Composition.Compositor? compositor;

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        if (compositor is null)
        {
            if (global::Windows.System.DispatcherQueue.GetForCurrentThread() is null)
            {
                // The current thread (DQTYPE_THREAD_CURRENT), single-threaded apartment (DQTAT_COM_STA).
                var options = new DesktopNative.DispatcherQueueOptions { dwSize = 12, threadType = 2, apartmentType = 2 };
                DesktopNative.CreateDispatcherQueueController(options, out _);
            }
            compositor = new global::Windows.UI.Composition.Compositor();
        }
        connectedTarget.SystemBackdrop = compositor.CreateColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0));
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        disconnectedTarget.SystemBackdrop = null;
        base.OnTargetDisconnected(disconnectedTarget);
    }
}
