using Deskling.Companion.Rules;
using Deskling.Companion.Settings;
using Microsoft.UI.Xaml.Controls;

namespace FixtureApp.Views;

/// <summary>Settings: the channels (turning on the notification one asks Windows), the appearance and the shortcut.</summary>
public sealed partial class SettingsView : UserControl
{
    public SettingsView(FixtureModel model)
    {
        Model = model;
        InitializeComponent();
        Bind(VoiceSwitch, Channels.Voice);
        Bind(ChimeSwitch, Channels.Chime);
        Bind(NotificationSwitch, Channels.Notification);
        AppearanceButtons.SelectedIndex = (int)model.Settings.Appearance;
        AppearanceButtons.SelectionChanged += (_, _) => Model.Settings.Appearance = (AppearanceSetting)AppearanceButtons.SelectedIndex;
    }

    public FixtureModel Model { get; }

    private void Bind(ToggleSwitch toggle, Channels channel)
    {
        toggle.IsOn = Model.Settings.Channels.Contains(channel);
        toggle.Toggled += (_, _) => Model.SetChannel(channel, toggle.IsOn);
    }
}
