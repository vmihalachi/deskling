using Deskling.Companion.Rules;
using Deskling.Companion.Settings;
using Deskling.Windows.Companion.Settings;
using Microsoft.UI.Xaml.Controls;

namespace FixtureApp.Views;

/// <summary>
/// Settings: the channels (turning on the notification one asks Windows), the package's rows (the hours, the quiet
/// hours, the shortcut, startup, the language) and the appearance. The fixture's words are plain English; an app's come
/// from its strings.
/// </summary>
public sealed partial class SettingsView : UserControl
{
    public SettingsView(FixtureModel model)
    {
        Model = model;
        InitializeComponent();
        Bind(VoiceSwitch, Channels.Voice);
        Bind(ChimeSwitch, Channels.Chime);
        Bind(NotificationSwitch, Channels.Notification);
        NotificationNote.Copy = new NotificationNoteCopy("Notifications are off for the fixture in Windows' settings.", "Open settings");
        NotificationNote.OnOpen = Model.OpenNotificationSettings;
        Hours.Settings = model.Settings;
        Hours.Copy = new ActiveHoursCopy("Active hours", "From", "To", "Days");
        Quiet.Settings = model.Settings;
        Shortcut.Settings = model.Settings;
        Shortcut.Copy = new ShortcutCopy(
            "Press keys…",
            "Record shortcut",
            "Clear shortcut",
            "Press a key combination with Ctrl, Alt or Windows. Esc cancels."
        );
        Shortcut.OnCaptureStarted = Model.SuspendHotKey;
        Shortcut.OnCaptureEnded = Model.RegisterHotKey;
        // Unpackaged, the fixture has no startup task: the toggle shows off and can't be turned on.
        Startup.Copy = new LaunchAtStartupCopy("Open at sign-in", "Windows blocked this. Turn it on in Settings › Apps › Startup.");
        LanguageChoice.Copy = new LanguageCopy("Language", "Same as Windows", "Restart the fixture to use it.", "Restart");
        LanguageChoice.Languages = ["en", "de", "ro"];
        AppearanceButtons.SelectedIndex = (int)model.Settings.Appearance;
        AppearanceButtons.SelectionChanged += (_, _) => Model.Settings.Appearance = (AppearanceSetting)AppearanceButtons.SelectedIndex;
    }

    public FixtureModel Model { get; }

    private void Bind(ToggleSwitch toggle, Channels channel)
    {
        toggle.IsOn = Model.Settings.IsOn(channel);
        toggle.Toggled += (_, _) => Model.SetChannel(channel, toggle.IsOn);
    }
}
