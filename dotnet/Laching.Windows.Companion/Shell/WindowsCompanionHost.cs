using Laching.Companion.Desktop;
using Laching.Companion.Settings;
using Laching.Companion.Shell.App;
using Laching.Companion.Shell.Desktop;
using Laching.Companion.Shell.Speech;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Launcher = Windows.System.Launcher;

namespace Laching.Windows.Companion.Shell;

/// <summary>
/// The app's views, which the host shows where the model asks: each window's spec and content, the tray panel, and the
/// character on the desktop.
/// </summary>
/// <param name="WindowSpec">Each window's title, size and traits.</param>
/// <param name="WindowContent">Each window's view, made when it opens.</param>
/// <param name="Panel">The tray icon's panel, made the first time it opens.</param>
public sealed record CompanionViews<TWindow>(
    Func<TWindow, WindowSpec> WindowSpec,
    Func<TWindow, UIElement> WindowContent,
    Func<UIElement> Panel
)
    where TWindow : notnull
{
    /// <summary>
    /// The character on the desktop (a <see cref="Views.CharacterCanvas"/> bound to the model), which the host stands in a
    /// <see cref="Desktop.DesktopCompanion"/> the first time the model shows it; none shows without it.
    /// </summary>
    public Func<UIElement>? DesktopCharacter { get; init; }
}

/// <summary>
/// Everything native the model drives on Windows (<see cref="ICompanionHost{TWindow}"/>): the windows
/// (<see cref="WindowManager{TWindow}"/>), timers on the UI thread, the tray icon
/// (<see cref="TrayStatusItem"/>), the desktop character, the appearance, the notification settings and quitting. The
/// app makes one on the UI thread, before the model, and hands both its views. UI thread only.
/// </summary>
public sealed class WindowsCompanionHost<TWindow> : ICompanionHost<TWindow>, IDisposable
    where TWindow : notnull
{
    private readonly CompanionViews<TWindow> views;
    private readonly Guid trayIconId;
    private readonly string trayToolTip;
    private readonly DispatcherQueue queue;
    private ElementTheme theme = ElementTheme.Default;

    /// <param name="trayIconId">The tray icon's identity (see <see cref="TrayStatusItem"/>).</param>
    /// <param name="trayToolTip">
    /// The tray icon's tooltip and accessible name (the configuration's <c>StatusItemTooltip</c>), and the title Narrator
    /// reads for the panel's window and the desktop character's.
    /// </param>
    /// <param name="windowIconPath">The windows' <c>.ico</c>; the executable's icon by default.</param>
    /// <param name="queue">The UI thread's queue; the current thread's by default.</param>
    public WindowsCompanionHost(
        CompanionViews<TWindow> views,
        Guid trayIconId,
        string trayToolTip,
        string? windowIconPath = null,
        DispatcherQueue? queue = null
    )
    {
        this.views = views;
        this.trayIconId = trayIconId;
        this.trayToolTip = trayToolTip;
        this.queue = UiThread.Resolve(queue, "host");
        Windows = new WindowManager<TWindow>(views.WindowSpec, views.WindowContent, windowIconPath);
        Timers = new DispatcherTimers(this.queue);
    }

    public WindowManager<TWindow> Windows { get; }

    public DispatcherTimers Timers { get; }

    /// <summary>The tray icon, once the model made it.</summary>
    public TrayStatusItem? StatusItem { get; private set; }

    /// <summary>The desktop character, once the model made it.</summary>
    public IDesktopCompanion? Desktop { get; private set; }

    public bool ReduceMotion => Shell.ReduceMotion.IsOn;

    /// <summary>
    /// Runs first on <see cref="Quit"/>: the app lets go of what else it holds (its <see cref="CompanionNotifier"/>, so a
    /// click on a notification left behind starts the app again).
    /// </summary>
    public Action? OnQuit { get; set; }

    IWindowHost<TWindow> ICompanionHost<TWindow>.Windows => Windows;

    ITimers ICompanionHost<TWindow>.Timers => Timers;

    /// <summary>
    /// Whether the menus' words (the app's <c>MenuCopy</c>) carry their own access keys, written the Win32 way:
    /// "&amp;Say something" makes S the key and "&amp;&amp;" shows one "&amp;". Off by default, so every "&amp;" shows as
    /// written and Windows takes each item's first letter. Applies to the tray icon's menu and the desktop character's
    /// (<see cref="Laching.Windows.Shell.TrayIconHost.AccessKeys"/>).
    /// </summary>
    public bool MenuAccessKeys { get; init; }

    public IStatusItem MakeStatusItem()
    {
        StatusItem?.Dispose();
        StatusItem = new TrayStatusItem(trayIconId, trayToolTip, views.Panel, Timers, () => ReduceMotion, queue)
        {
            Theme = theme,
            MenuAccessKeys = MenuAccessKeys,
        };
        return StatusItem;
    }

    public IDesktopCompanion MakeDesktop(DesktopLayout layout, DesktopSize size, DesktopPosition? position, string sayActionName)
    {
        Desktop = views.DesktopCharacter is { } character
            ? new Desktop.DesktopCompanion(character(), layout, size, position, sayActionName, trayToolTip) { MenuAccessKeys = MenuAccessKeys, Theme = theme }
            : new AbsentDesktop();
        return Desktop;
    }

    public void ApplyAppearance(AppearanceSetting appearance)
    {
        theme = Theme(appearance);
        Windows.Theme = theme;
        if (StatusItem is not null)
            StatusItem.Theme = theme;
        if (Desktop is Desktop.DesktopCompanion desktop)
            desktop.Theme = theme;
    }

    /// <summary>Settings › System › Notifications, where the app's switch is.</summary>
    public void OpenNotificationSettings() => _ = Launcher.LaunchUriAsync(new Uri("ms-settings:notifications"));

    /// <summary>The app's <see cref="OnQuit"/>, then the tray icon (which would linger until hovered), and the windows go, and the app exits.</summary>
    public void Quit()
    {
        OnQuit?.Invoke();
        Dispose();
        Application.Current.Exit();
    }

    /// <summary>Closes the windows and takes the tray icon away; once, at exit.</summary>
    public void Dispose()
    {
        Windows.CloseAll();
        StatusItem?.Dispose();
        StatusItem = null;
        (Desktop as IDisposable)?.Dispose();
    }

    /// <summary>The appearance setting as a XAML theme (<see cref="ElementTheme.Default"/> follows Windows' app mode).</summary>
    public static ElementTheme Theme(AppearanceSetting appearance) =>
        appearance switch
        {
            AppearanceSetting.Light => ElementTheme.Light,
            AppearanceSetting.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

    /// <summary>No desktop character: the app gave no view for one.</summary>
    private sealed class AbsentDesktop : IDesktopCompanion
    {
        public Action? OnSay { get; set; }

        public Action<DesktopPosition>? OnMoved { get; set; }

        public Action<int>? OnPinch { get; set; }

        public Action? OnBubbleDismissed { get; set; }

        public Func<IReadOnlyList<MenuItem>>? Menu { get; set; }

        public bool IsShown => false;

        public void Show(bool animated) { }

        public void Hide(bool animated) { }

        public void SetSize(DesktopSize size) { }

        public void SetPosition(DesktopPosition? position) { }

        public void ShowBubble(string text, BubbleCountdown? countdown, bool animated) { }

        public void SetCountdown(BubbleCountdown? countdown) { }

        public void HideBubble(bool animated) { }
    }
}
