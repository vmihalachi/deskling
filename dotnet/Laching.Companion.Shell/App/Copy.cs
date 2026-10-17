using Laching.Companion.Settings;

namespace Laching.Companion.Shell.App;

/// <summary>Which of the app's windows plays which part. The model shows onboarding on the first run, Home otherwise.</summary>
public sealed record WindowRoles<TWindow>(TWindow Home, TWindow Onboarding, TWindow Settings, TWindow Support)
    where TWindow : notnull;

/// <summary>
/// The status pill's words, from the app's strings. Times come as moments in the model's time zone, so the app formats
/// them its own way (a short time, a weekday with a time) and the span until the next line as whole minutes.
/// </summary>
public sealed record StatusCopy(
    Func<string> ShhUntilYouSay,
    Func<string> ShhRestOfToday,
    Func<DateTimeOffset, string> ShhUntil,
    Func<DateTimeOffset, string> ShhUntilDay,
    Func<string> QuietCall,
    Func<string> QuietFullScreen,
    Func<string> OffHours,
    Func<string> Asleep,
    Func<int, string> NextInMinutes);

/// <summary>The desktop character's words: its accessibility action and the panel row while it's away for a while.</summary>
public sealed record DesktopCopy(Func<string> SayActionName, Func<string> BackTomorrow, Func<DateTimeOffset, string> BackAt);

/// <summary>
/// The menus' words (Swift's <c>MenuCopy</c>). On Windows an "&amp;" in them marks an access key only when the host is
/// told so (<c>WindowsCompanionHost.MenuAccessKeys</c>); otherwise it shows as written.
/// </summary>
public sealed record MenuCopy(
    string Say,
    string WakeUp,
    string Shh,
    string ForAnHour,
    string ForTheRestOfToday,
    string UntilISay,
    string DesktopSubmenu,
    string Show,
    string ShowNow,
    string HideForAnHour,
    string HideForTheRestOfToday,
    string Size,
    string Hide,
    string UntilIShowItAgain,
    string Settings,
    string Support,
    string ThanksForSupporting,
    string Quit,
    Func<DesktopSize, string> SizeLabel);

/// <summary>The app's short sounds by name (<c>&lt;name&gt;.wav</c>); null plays none.</summary>
public sealed record CompanionSounds(string? Blip, string? Shh, string? Hello);

/// <summary>
/// What the model needs from the app besides its views: the status item's tooltip, its sounds, its words, its phrase
/// lookup and its supporter product. The look (paper, palette, display font) is the platform side's.
/// </summary>
public sealed record CompanionAppConfiguration(
    string StatusItemTooltip,
    string AccessibilityLabel,
    CompanionSounds Sounds,
    StatusCopy Status,
    DesktopCopy Desktop,
    Func<string> TuneBubbleText,
    Func<string, string, string> Localize,
    string? SupporterProduct = null);

/// <summary>Whether the app may show notifications.</summary>
public enum NotificationPermission
{
    NotDetermined,
    Authorized,
    Denied,
}
