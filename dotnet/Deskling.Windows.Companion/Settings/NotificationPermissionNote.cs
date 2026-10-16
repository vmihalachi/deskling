using Deskling.Companion.Shell.App;
using Deskling.Windows.Companion.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Deskling.Windows.Companion.Settings;

/// <summary>
/// "Notifications are off for the app in Windows' settings" with a button to them (Swift's
/// <c>NotificationPermissionNote</c>), shown only while the permission is denied: Narrator hears the note when it
/// appears on screen, and again as the button's help text. In the look's accent text color; the
/// words are the app's and the button takes its <see cref="ButtonStyle"/>. <see cref="OnOpen"/> is the model's
/// <c>OpenNotificationSettings</c>.
/// </summary>
public sealed partial class NotificationPermissionNote : UserControl
{
    private readonly FontIcon bell = new() { Glyph = "", FontSize = 16, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock text = new() { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
    private readonly Button open = new() { VerticalAlignment = VerticalAlignment.Center };

    public NotificationPermissionNote()
    {
        IsTabStop = false;
        AutomationProperties.SetAccessibilityView(bell, AccessibilityView.Raw);
        var row = new Grid { ColumnSpacing = Space.Snug };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(bell);
        Grid.SetColumn(text, 1);
        row.Children.Add(text);
        Grid.SetColumn(open, 2);
        row.Children.Add(open);
        Content = row;
        open.Click += (_, _) => OnOpen?.Invoke();
        Visibility = Visibility.Collapsed;
        LookBinding.Keep(
            this,
            () =>
            {
                var brush = LookBinding.Brush(this, p => p.AccentText);
                bell.Foreground = brush;
                text.Foreground = brush;
            }
        );
    }

    /// <summary>The model's <c>NotificationPermission</c>; the note shows only for <see cref="NotificationPermission.Denied"/>.</summary>
    public NotificationPermission Permission
    {
        get;
        set
        {
            field = value;
            var before = Visibility;
            Visibility = value == NotificationPermission.Denied ? Visibility.Visible : Visibility.Collapsed;
            if (Announcer.Appears(before, Visibility))
                Announcer.Say(text, Copy?.Text, Announcer.Note);
        }
    }

    public NotificationNoteCopy? Copy
    {
        get;
        set
        {
            field = value;
            text.Text = value?.Text ?? "";
            open.Content = value?.Open;
            // The button says why it's there: the note beside it.
            AutomationProperties.SetHelpText(open, value?.Text ?? "");
        }
    }

    public Style? ButtonStyle
    {
        get => open.Style;
        set => open.Style = value;
    }

    public Action? OnOpen { get; set; }
}
