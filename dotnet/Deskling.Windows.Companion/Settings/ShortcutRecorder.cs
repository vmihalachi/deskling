using Deskling.Companion.Settings;
using Deskling.Core.Input;
using Deskling.Windows.Shell;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using VirtualKey = Windows.System.VirtualKey;
using VirtualKeyStates = Windows.UI.Core.CoreVirtualKeyStates;

namespace Deskling.Windows.Companion.Settings;

/// <summary>
/// The say-something shortcut (Swift's <c>ShortcutRecorder</c>): click, then press a key combination with Ctrl, Alt or
/// Windows. Esc cancels, Backspace or Delete clears, and the cross clears too. Every word is the app's
/// (<see cref="Copy"/>); the buttons take its <see cref="ButtonStyle"/>. <see cref="OnCaptureStarted"/> runs before
/// recording (the model's <c>SuspendHotKey</c>, so pressing the current one records it), <see cref="OnCaptureEnded"/>
/// whenever recording ends, however it ends (<c>RegisterHotKey</c>).
/// </summary>
public sealed partial class ShortcutRecorder : UserControl
{
    private readonly Button record = new();
    private readonly Button clear = new() { Content = "✕", Padding = new Thickness(6, 2, 6, 2), VerticalAlignment = VerticalAlignment.Center };
    private readonly SettingsWatch watch;
    private CompanionSettings? settings;
    private bool recording;

    public ShortcutRecorder()
    {
        IsTabStop = false;
        record.Click += (_, _) =>
        {
            if (recording)
                Stop();
            else
                Start();
        };
        record.PreviewKeyDown += OnKeyDown;
        record.LostFocus += (_, _) =>
        {
            if (recording)
                Stop();
        };
        clear.Click += (_, _) =>
        {
            if (settings is not null)
                settings.SayShortcut = null;
        };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        row.Children.Add(record);
        row.Children.Add(clear);
        Content = row;
        Unloaded += (_, _) =>
        {
            if (recording)
                Stop();
        };
        watch = new SettingsWatch(this, () => settings, Update, nameof(CompanionSettings.SayShortcut));
    }

    public CompanionSettings? Settings
    {
        get => settings;
        set
        {
            settings = value;
            watch.Reset();
        }
    }

    public ShortcutCopy? Copy
    {
        get;
        set
        {
            field = value;
            AutomationProperties.SetName(clear, value?.Clear ?? "");
            AutomationProperties.SetHelpText(record, value?.Hint ?? "");
            Update();
        }
    }

    /// <summary>The record button's style; WinUI's button when null. The cross stays plain.</summary>
    public Style? ButtonStyle
    {
        get => record.Style;
        set => record.Style = value;
    }

    public Action? OnCaptureStarted { get; set; }

    public Action? OnCaptureEnded { get; set; }

    /// <summary>
    /// "Ctrl+Alt+Shift+F", with the keyboard layout's names for the modifiers and the key's name as recorded. The
    /// shortcut keeps the Mac's masks: command is Windows, option is Alt.
    /// </summary>
    public static string Describe(KeyShortcut shortcut)
    {
        var parts = new List<string>();
        if ((shortcut.Modifiers & KeyShortcut.Command) != 0)
            parts.Add("Win");
        if ((shortcut.Modifiers & KeyShortcut.Control) != 0)
            parts.Add(GlobalHotKey.KeyName((int)VirtualKey.Control) ?? "Ctrl");
        if ((shortcut.Modifiers & KeyShortcut.Option) != 0)
            parts.Add(GlobalHotKey.KeyName((int)VirtualKey.Menu) ?? "Alt");
        if ((shortcut.Modifiers & KeyShortcut.Shift) != 0)
            parts.Add(GlobalHotKey.KeyName((int)VirtualKey.Shift) ?? "Shift");
        parts.Add(shortcut.Key);
        return string.Join("+", parts);
    }

    private void Start()
    {
        recording = true;
        OnCaptureStarted?.Invoke();
        Update();
    }

    private void Stop()
    {
        recording = false;
        Update();
        OnCaptureEnded?.Invoke();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!recording || settings is null)
            return;
        e.Handled = true;
        var key = e.OriginalKey;
        if (
            key
            is VirtualKey.Control
                or VirtualKey.LeftControl
                or VirtualKey.RightControl
                or VirtualKey.Shift
                or VirtualKey.LeftShift
                or VirtualKey.RightShift
                or VirtualKey.Menu
                or VirtualKey.LeftMenu
                or VirtualKey.RightMenu
                or VirtualKey.LeftWindows
                or VirtualKey.RightWindows
        )
            return;
        uint modifiers = 0;
        if (IsDown(VirtualKey.Control))
            modifiers |= KeyShortcut.Control;
        if (IsDown(VirtualKey.Menu))
            modifiers |= KeyShortcut.Option;
        if (IsDown(VirtualKey.Shift))
            modifiers |= KeyShortcut.Shift;
        if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows))
            modifiers |= KeyShortcut.Command;
        if (modifiers == 0 && key == VirtualKey.Escape)
        {
            Stop();
            return;
        }
        if (modifiers == 0 && key is VirtualKey.Back or VirtualKey.Delete)
        {
            settings.SayShortcut = null;
            Stop();
            return;
        }
        if ((int)key is <= 0 or >= 0xFF)
            return;
        var shortcut = new KeyShortcut((uint)key, modifiers, GlobalHotKey.KeyName((int)key) ?? $"#{(int)key}");
        if (!shortcut.IsValid)
            return;
        settings.SayShortcut = shortcut;
        Stop();
    }

    private static bool IsDown(VirtualKey key) => InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(VirtualKeyStates.Down);

    private void Update()
    {
        var shortcut = settings?.SayShortcut;
        record.Content =
            recording ? Copy?.PressKeys
            : shortcut is not null ? Describe(shortcut)
            : Copy?.Record;
        clear.Visibility = shortcut is not null && !recording ? Visibility.Visible : Visibility.Collapsed;
    }
}
