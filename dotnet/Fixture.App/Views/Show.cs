using Deskling.Companion.Shell.App;
using Microsoft.UI.Xaml;

namespace FixtureApp.Views;

/// <summary>The views' <c>x:Bind</c> functions.</summary>
public static class Show
{
    public static Visibility When(bool on) => on ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility Unless(bool on) => on ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility WhenSet(string? text) => When(text is not null);

    public static bool Has(string? text) => text is not null;

    public static string Bubble(string? text) => text ?? "Nothing said yet.";

    public static string Permission(NotificationPermission permission) =>
        permission switch
        {
            NotificationPermission.Authorized => "Notifications are on.",
            NotificationPermission.Denied => "Notifications are off in Windows' settings.",
            _ => "Notifications: not asked yet.",
        };
}
