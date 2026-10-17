using Laching.Companion.Figures;
using Laching.Companion.Rules;
using Laching.Companion.Shell.App;
using Laching.Companion.Shell.Speech;
using Laching.Windows.Shell;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Laching.Windows.Companion.Shell;

/// <summary>
/// The notification-area icon (<see cref="IStatusItem"/>, Swift's <c>StatusItemController</c>) over
/// <see cref="TrayIconHost"/>: the character's glyph (<see cref="GlyphRenderer"/>, stepped at 8 fps while talking or
/// waving, one frame when motion is reduced), the app's panel next to the icon on a left click, and the model's menu,
/// built fresh, on a right click. The tooltip is the icon's accessible name too, and the panel's window title. Create it
/// on the UI thread.
/// </summary>
public sealed class TrayStatusItem : IStatusItem, IDisposable
{
    private readonly TrayIconHost tray;
    private readonly TrayPanel panel;
    private readonly GlyphAnimation animation;
    private readonly string toolTip;
    private RiggedCharacter? character;

    /// <param name="iconId">The icon's identity to Windows (see <see cref="TrayIconHost"/>): one per app, another for a Dev build, never changed.</param>
    /// <param name="toolTip">The app's name or the character's, already localized.</param>
    /// <param name="panel">The panel's view, made the first time it opens; it keeps the size it asks for.</param>
    /// <param name="timers">For the glyph's frames.</param>
    /// <param name="reduceMotion">Read when the frames start; <see cref="Shell.ReduceMotion.IsOn"/> by default.</param>
    /// <param name="queue">The UI thread's queue; the current thread's by default.</param>
    /// <param name="windowClassName">The icon's hidden window's class name, unique within the process.</param>
    public TrayStatusItem(
        Guid iconId,
        string toolTip,
        Func<UIElement> panel,
        ITimers timers,
        Func<bool>? reduceMotion = null,
        DispatcherQueue? queue = null,
        string windowClassName = "Laching.Companion.Tray"
    )
    {
        this.toolTip = toolTip;
        tray = new TrayIconHost(queue ?? DispatcherQueue.GetForCurrentThread(), iconId, windowClassName);
        this.panel = new TrayPanel(panel, () => tray.TryGetIconRect(out var rect) ? rect : null, toolTip);
        animation = new GlyphAnimation(timers, reduceMotion ?? (() => Shell.ReduceMotion.IsOn), Draw);
        tray.Selected += anchor => this.panel.Toggle(anchor, tray.ByKeyboard);
        tray.MenuOpening += () =>
        {
            this.panel.Close();
            tray.SetMenu(Menu is { } menu ? MenuConversion.ToTray(menu()) : []);
        };
    }

    public Func<IReadOnlyList<MenuItem>>? Menu { get; set; }

    /// <summary>
    /// Whether the menu's words carry their own access keys ("&amp;Say something"); off by default, so every "&amp;"
    /// shows as written. See <see cref="TrayIconHost.AccessKeys"/>.
    /// </summary>
    public bool MenuAccessKeys
    {
        get => tray.AccessKeys;
        set => tray.AccessKeys = value;
    }

    /// <summary>Light, dark or Windows' app mode, for the panel.</summary>
    public ElementTheme Theme
    {
        get => panel.Theme;
        set => panel.Theme = value;
    }

    public bool IsPanelOpen => panel.IsOpen;

    public void Show(RiggedCharacter character, Mood mood, bool talking)
    {
        this.character = character;
        animation.Show(mood, talking);
    }

    public void Wave() => animation.Wave();

    public void OpenPanel() => panel.Open();

    public void ClosePanel() => panel.Close();

    public void Dispose()
    {
        animation.Stop();
        panel.Dispose();
        tray.Dispose();
    }

    private void Draw(Mood mood, int frame)
    {
        if (character is not { } shown)
            return;
        tray.SetGlyph(light => GlyphRenderer.Icon(shown, mood, frame, light), toolTip);
    }
}
