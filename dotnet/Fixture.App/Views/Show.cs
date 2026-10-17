using Laching.Companion.Shell.App;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FixtureApp.Views;

/// <summary>The views' <c>x:Bind</c> functions.</summary>
public static class Show
{
    public static Visibility When(bool on) => on ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility Unless(bool on) => on ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Visible while <paramref name="text"/> has something in it: <c>x:Bind</c> hands a null string over as empty.</summary>
    public static Visibility WhenSet(string? text) => When(!string.IsNullOrEmpty(text));

    /// <summary>True while <paramref name="text"/> has something in it: <c>x:Bind</c> hands a null string over as empty.</summary>
    public static bool Has(string? text) => !string.IsNullOrEmpty(text);

    public static string Bubble(string? text) => text ?? "Nothing said yet.";

    /// <summary>A joke's setup, small, for a bubble's header over the punchline; none while there's no setup.</summary>
    public static UIElement? Setup(string? text) =>
        string.IsNullOrEmpty(text) ? null : new TextBlock { Text = text, FontSize = 12, Opacity = 0.7, TextWrapping = TextWrapping.WrapWholeWords };

    public static string Permission(NotificationPermission permission) =>
        permission switch
        {
            NotificationPermission.Authorized => "Notifications are on.",
            NotificationPermission.Denied => "Notifications are off in Windows' settings.",
            _ => "Notifications: not asked yet.",
        };
}
