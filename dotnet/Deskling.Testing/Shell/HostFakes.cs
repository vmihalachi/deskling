using Deskling.Companion.Figures;
using Deskling.Companion.Rules;
using Deskling.Companion.Settings;
using Deskling.Companion.Shell.App;
using Deskling.Companion.Shell.Desktop;
using Deskling.Companion.Shell.Speech;

namespace Deskling.Testing;

/// <summary>Counts permission requests and posted lines; grants by default.</summary>
public sealed class FakeNotifier : ICompanionNotifying
{
    private readonly List<string> posted = [];

    public Action<string>? OnAction { get; set; }

    public int Requests { get; private set; }

    public IReadOnlyList<string> Posted => posted;

    public bool Granted { get; set; } = true;

    public void Configure() { }

    public void RequestAuthorization(Action<bool> completion)
    {
        Requests++;
        completion(Granted);
    }

    public void RefreshAuthorization(Action<NotificationPermission> completion) =>
        completion(Granted ? NotificationPermission.Authorized : NotificationPermission.NotDetermined);

    public void Post(string line) => posted.Add(line);
}

/// <summary>A tray icon that remembers what it was told to show.</summary>
public sealed class FakeStatusItem : IStatusItem
{
    public Func<IReadOnlyList<MenuItem>>? Menu { get; set; }

    public (RiggedCharacter Character, Mood Mood, bool Talking)? Shown { get; private set; }

    public int Waves { get; private set; }

    public bool IsPanelOpen { get; private set; }

    public void Show(RiggedCharacter character, Mood mood, bool talking) => Shown = (character, mood, talking);

    public void Wave() => Waves++;

    public void OpenPanel() => IsPanelOpen = true;

    public void ClosePanel() => IsPanelOpen = false;
}

/// <summary>Windows as a set of open ids.</summary>
public sealed class FakeWindowHost<TWindow> : IWindowHost<TWindow>
    where TWindow : notnull
{
    private readonly List<TWindow> open = [];

    public Action<TWindow>? OnClose { get; set; }

    /// <summary>The open windows, in the order they were shown.</summary>
    public IReadOnlyList<TWindow> Open => open;

    public bool IsOpen(TWindow id) => open.Contains(id);

    public void Show(TWindow id)
    {
        if (!open.Contains(id))
            open.Add(id);
    }

    public void Close(TWindow id)
    {
        if (open.Remove(id))
            OnClose?.Invoke(id);
    }

    public void CloseAll()
    {
        foreach (var id in open.ToArray())
            Close(id);
    }

    public bool ShowOpenWindows() => open.Count > 0;
}

/// <summary>The desktop character's window as state: shown, size, position, bubble.</summary>
public sealed class FakeDesktopCompanion : IDesktopCompanion
{
    public Action? OnSay { get; set; }

    public Action<DesktopPosition>? OnMoved { get; set; }

    public Action<int>? OnPinch { get; set; }

    public Action? OnBubbleDismissed { get; set; }

    public Func<IReadOnlyList<MenuItem>>? Menu { get; set; }

    public bool IsShown { get; private set; }

    public DesktopSize Size { get; private set; }

    public DesktopPosition? Position { get; private set; }

    public string? Bubble { get; private set; }

    public BubbleCountdown? Countdown { get; private set; }

    public void Show(bool animated) => IsShown = true;

    public void Hide(bool animated) => IsShown = false;

    public void SetSize(DesktopSize size) => Size = size;

    public void SetPosition(DesktopPosition? position) => Position = position;

    public void ShowBubble(string text, BubbleCountdown? countdown, bool animated)
    {
        Bubble = text;
        Countdown = countdown;
    }

    public void SetCountdown(BubbleCountdown? countdown) => Countdown = countdown;

    public void HideBubble(bool animated)
    {
        Bubble = null;
        Countdown = null;
    }
}

/// <summary>Everything native, faked: the companion model's host in tests.</summary>
public sealed class FakeHost<TWindow>(ManualTimers timers) : ICompanionHost<TWindow>
    where TWindow : notnull
{
    public FakeWindowHost<TWindow> WindowHost { get; } = new();

    public ManualTimers ManualTimers { get; } = timers;

    /// <summary>The status item, once the model launched.</summary>
    public FakeStatusItem? StatusItem { get; private set; }

    /// <summary>The desktop window, once the model first showed it.</summary>
    public FakeDesktopCompanion? Desktop { get; private set; }

    public AppearanceSetting? Appearance { get; private set; }

    public int Quits { get; private set; }

    public IWindowHost<TWindow> Windows => WindowHost;

    public ITimers Timers => ManualTimers;

    public bool ReduceMotion { get; set; }

    public IStatusItem MakeStatusItem() => StatusItem = new FakeStatusItem();

    public IDesktopCompanion MakeDesktop(DesktopLayout layout, DesktopSize size, DesktopPosition? position, string sayActionName) =>
        Desktop = new FakeDesktopCompanion();

    public void ApplyAppearance(AppearanceSetting appearance) => Appearance = appearance;

    public void OpenNotificationSettings() { }

    public void Quit() => Quits++;
}
