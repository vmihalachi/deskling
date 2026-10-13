using Deskling.Companion.Figures;
using Deskling.Companion.Rules;
using Deskling.Companion.Settings;
using Deskling.Companion.Shell.Desktop;
using Deskling.Companion.Shell.Speech;
using Deskling.Core.Input;

namespace Deskling.Companion.Shell.App;

/// <summary>
/// The tray or menu bar icon (Swift's <c>StatusItemController</c>): the character's glyph, its panel (left click) and
/// its menu (right click). The platform draws the glyph and owns the panel's content.
/// </summary>
public interface IStatusItem
{
    /// <summary>The menu, built fresh each time it opens.</summary>
    Func<IReadOnlyList<MenuItem>>? Menu { get; set; }

    /// <summary>Shows <paramref name="character"/>'s glyph for <paramref name="mood"/>, stepping the talking frames while <paramref name="talking"/>.</summary>
    void Show(RiggedCharacter character, Mood mood, bool talking);

    /// <summary>The glyph waves for a moment (Home appeared).</summary>
    void Wave();

    void OpenPanel();

    void ClosePanel();
}

/// <summary>The app's windows by id (Swift's <c>WindowManager</c>): one window per id, its content from the app.</summary>
public interface IWindowHost<TWindow>
    where TWindow : notnull
{
    /// <summary>A window closed (by the user or <see cref="Close"/>).</summary>
    Action<TWindow>? OnClose { get; set; }

    bool IsOpen(TWindow id);

    /// <summary>Shows the window, making it first if needed, and brings it to the front.</summary>
    void Show(TWindow id);

    void Close(TWindow id);

    void CloseAll();

    /// <summary>Brings the open windows to the front; false when none is open.</summary>
    bool ShowOpenWindows();
}

/// <summary>The character's window on the desktop and its speech bubble (Swift's <c>DesktopCompanion</c>).</summary>
public interface IDesktopCompanion
{
    /// <summary>A click on the character: say something.</summary>
    Action? OnSay { get; set; }

    /// <summary>The character was dragged to <c>position</c> (its anchor, y-down screen coordinates).</summary>
    Action<DesktopPosition>? OnMoved { get; set; }

    /// <summary>A pinch or Ctrl+wheel: one size up (+1) or down (−1).</summary>
    Action<int>? OnPinch { get; set; }

    /// <summary>The user dismissed the bubble.</summary>
    Action? OnBubbleDismissed { get; set; }

    /// <summary>The right-click menu.</summary>
    Func<IReadOnlyList<MenuItem>>? Menu { get; set; }

    bool IsShown { get; }

    void Show(bool animated);

    void Hide(bool animated);

    void SetSize(DesktopSize size);

    /// <summary>Null puts it back in the default corner.</summary>
    void SetPosition(DesktopPosition? position);

    void ShowBubble(string text, BubbleCountdown? countdown, bool animated);

    void SetCountdown(BubbleCountdown? countdown);

    void HideBubble(bool animated);
}

/// <summary>When the desktop bubble started running down and for how long.</summary>
public readonly record struct BubbleCountdown(DateTimeOffset Start, double Seconds);

/// <summary>The global "say something" shortcut (Swift's <c>GlobalHotKey</c>).</summary>
public interface IHotKey
{
    Action? OnPress { get; set; }

    /// <summary>Registers <paramref name="shortcut"/> in place of the last one; false when another app holds it.</summary>
    bool Register(KeyShortcut shortcut);

    void Unregister();
}

/// <summary>
/// What the model needs from the app's notifications (usually a wrapper over the platform's with the app's title and
/// buttons), so tests can count permission requests. Actions arrive as their raw ids.
/// </summary>
public interface ICompanionNotifying
{
    /// <summary>A button was pressed: its action id.</summary>
    Action<string>? OnAction { get; set; }

    /// <summary>Registers the buttons and takes the activations; once at launch.</summary>
    void Configure();

    /// <summary>Asks the user (where the platform asks). Only called when the notification channel is turned on.</summary>
    void RequestAuthorization(Action<bool> completion);

    void RefreshAuthorization(Action<NotificationPermission> completion);

    /// <summary>Shows the line (both parts of a joke on two lines), replacing the last banner.</summary>
    void Post(string line);
}

/// <summary>
/// Everything native the model drives, supplied by the platform (Deskling.Windows.Companion) or by tests. The model
/// creates nothing native itself: the status item and the desktop window are made through here when first needed.
/// </summary>
public interface ICompanionHost<TWindow>
    where TWindow : notnull
{
    IWindowHost<TWindow> Windows { get; }

    IHotKey HotKey { get; }

    ITimers Timers { get; }

    /// <summary>Animations are reduced (the system's setting).</summary>
    bool ReduceMotion { get; }

    /// <summary>The tray or menu bar icon, made once at launch.</summary>
    IStatusItem MakeStatusItem();

    /// <summary>The desktop character's window, made the first time it is shown.</summary>
    IDesktopCompanion MakeDesktop(DesktopLayout layout, DesktopSize size, DesktopPosition? position, string sayActionName);

    /// <summary>Light, dark or the system's, for the app's windows.</summary>
    void ApplyAppearance(AppearanceSetting appearance);

    /// <summary>The system's notification settings for the app, for when permission was denied.</summary>
    void OpenNotificationSettings();

    void Quit();
}
