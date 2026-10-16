using System.Runtime.InteropServices;
using Deskling.Windows.Companion.Views;
using Deskling.Windows.Shell;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Deskling.Windows.Companion.Settings;

/// <summary>
/// "Open at sign-in" (Swift's <c>LaunchAtLoginToggle</c>), through the package's startup task (<see cref="StartupService"/>),
/// with a line under it when Windows refuses: the user or a policy blocked the task, and only they can turn it back on
/// (Settings › Apps › Startup). Without package identity (an unpackaged build) the toggle is off and can't be turned
/// on. Narrator hears the line when it appears, and again as the toggle's help text while it shows. The words are the
/// app's; the line is in the look's accent text color.
/// </summary>
public sealed partial class LaunchAtStartupToggle : UserControl
{
    private readonly TextBlock label = new() { VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
    private readonly ToggleSwitch toggle = new() { OnContent = "", OffContent = "", MinWidth = 0, HorizontalAlignment = HorizontalAlignment.Right };
    private readonly TextBlock blocked = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed };
    private StartupService? startup;
    private bool updating;

    public LaunchAtStartupToggle()
    {
        IsTabStop = false;
        var row = new Grid { ColumnSpacing = Space.Item };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(label);
        // The control carries the label for Narrator, which would otherwise read it twice.
        AutomationProperties.SetAccessibilityView(label, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
        Grid.SetColumn(toggle, 1);
        row.Children.Add(toggle);
        var stack = new StackPanel { Spacing = Space.Tight };
        stack.Children.Add(row);
        stack.Children.Add(blocked);
        Content = stack;
        toggle.Toggled += async (_, _) => await Set(toggle.IsOn);
        Loaded += async (_, _) => await Read();
        LookBinding.Keep(this, () => blocked.Foreground = LookBinding.Brush(this, p => p.AccentText));
    }

    /// <summary>The <c>TaskId</c> of the <c>uap5:StartupTask</c> in the app's <c>Package.appxmanifest</c>.</summary>
    public string? TaskId
    {
        get;
        set
        {
            field = value;
            startup = value is null ? null : new StartupService(value);
        }
    }

    public LaunchAtStartupCopy? Copy
    {
        get;
        set
        {
            field = value;
            label.Text = value?.Label ?? "";
            AutomationProperties.SetName(toggle, value?.Label ?? "");
            blocked.Text = value?.Blocked ?? "";
        }
    }

    private async Task Read()
    {
        if (startup is null)
        {
            Show(on: false, available: false);
            return;
        }
        try
        {
            Show(await startup.IsEnabledAsync(), available: true);
        }
        catch (Exception e) when (e is InvalidOperationException or COMException or UnauthorizedAccessException)
        {
            // No package identity, or no such task: nothing to turn on.
            Show(on: false, available: false);
        }
    }

    private async Task Set(bool on)
    {
        if (updating || startup is null)
            return;
        try
        {
            var enabled = await startup.SetEnabledAsync(on);
            Show(enabled, available: true);
            ShowBlocked(on && !enabled);
        }
        catch (Exception e) when (e is InvalidOperationException or COMException or UnauthorizedAccessException)
        {
            Show(on: false, available: false);
        }
    }

    /// <summary>The line Windows' refusal brings: announced as it appears, and the toggle's help text while it shows.</summary>
    private void ShowBlocked(bool show)
    {
        var before = blocked.Visibility;
        blocked.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetHelpText(toggle, show ? blocked.Text : "");
        if (Announcer.Appears(before, blocked.Visibility))
            Announcer.Say(blocked, blocked.Text, Announcer.Note);
    }

    private void Show(bool on, bool available)
    {
        updating = true;
        toggle.IsOn = on;
        toggle.IsEnabled = available;
        updating = false;
    }
}
