using Laching.Companion.Settings;
using Laching.Companion.Shell.App;

namespace Laching.Testing.Fixtures;

/// <summary>The fixture app's windows, which the shell's tests and the fixture app both run a model over.</summary>
public enum FixtureWindow
{
    Home,
    Onboarding,
    Settings,
    Support,
}

/// <summary>
/// The fixture model's placeholder words, like the fixture bank's: English, named for no app, for the shell's tests and
/// the fixture app. An app's model passes its own strings.
/// </summary>
public static class FixtureCopy
{
    /// <summary>The <see cref="FixtureWindow"/> each role opens.</summary>
    public static WindowRoles<FixtureWindow> Roles { get; } =
        new(FixtureWindow.Home, FixtureWindow.Onboarding, FixtureWindow.Settings, FixtureWindow.Support);

    /// <summary>The status pill's words, times written with <paramref name="timeFormat"/> ("HH:mm" in tests, "t" in the app).</summary>
    public static StatusCopy Status(string timeFormat) =>
        new(
            ShhUntilYouSay: () => "Shh until you say",
            ShhRestOfToday: () => "Shh for the rest of today",
            ShhUntil: time => $"Shh until {time.ToString(timeFormat)}",
            ShhUntilDay: day => $"Shh until {day:ddd} {day.ToString(timeFormat)}",
            QuietCall: () => "Quiet: you're on a call",
            QuietFullScreen: () => "Quiet: full screen",
            OffHours: () => "Off hours",
            Asleep: () => "Asleep",
            NextInMinutes: minutes => $"Next in about {minutes} min"
        );

    /// <summary>The desktop character's words, times written with <paramref name="timeFormat"/>.</summary>
    public static DesktopCopy Desktop(string timeFormat) =>
        new(SayActionName: () => "Say something", BackTomorrow: () => "Back tomorrow", BackAt: time => $"Back at {time.ToString(timeFormat)}");

    /// <summary>
    /// The menus' words: with their access keys marked ("&amp;Say something", for a host whose <c>MenuAccessKeys</c> is
    /// on) when <paramref name="accessKeys"/>, else plain.
    /// </summary>
    public static MenuCopy Menu(bool accessKeys)
    {
        string M(string text) => accessKeys ? text : text.Replace("&", "");
        return new(
            Say: M("&Say something"),
            WakeUp: M("&Wake up"),
            Shh: M("S&hh"),
            ForAnHour: M("For an &hour"),
            ForTheRestOfToday: M("For the &rest of today"),
            UntilISay: M("&Until I say"),
            DesktopSubmenu: M("&On the desktop"),
            Show: M("&Show"),
            ShowNow: M("&Show now"),
            HideForAnHour: M("&Hide for an hour"),
            HideForTheRestOfToday: M("Hide for the &rest of today"),
            Size: M("Si&ze"),
            Hide: M("H&ide"),
            UntilIShowItAgain: M("&Until I show it again"),
            Settings: M("S&ettings…"),
            Support: M("S&upport…"),
            ThanksForSupporting: M("&Thanks for supporting ♥…"),
            Quit: M("&Quit"),
            SizeLabel: size => size.RawValue()
        );
    }
}
