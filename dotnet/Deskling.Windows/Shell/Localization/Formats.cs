using Windows.Globalization.DateTimeFormatting;

namespace Deskling.Windows.Shell.Localization;

/// <summary>
/// Locale-aware formatting for times, days, durations, lists and numbers in <see cref="Loc.Language"/>. Dates and
/// numbers come from the OS; durations and lists need the app's catalog keys, set once with <see cref="Configure"/>.
/// </summary>
public static class Formats
{
    private static FormatKeys? keys;
    private static DateTimeFormatter? weekdayMonthDay;
    private static DateTimeFormatter? hourOnly;

    /// <summary>The keys <see cref="DurationMinutes"/> and <see cref="List"/> format with. Call once at launch.</summary>
    public static void Configure(FormatKeys formatKeys)
    {
        keys = formatKeys;
    }

    /// <summary>"14:30" or "2:30 PM".</summary>
    public static string ShortTime(DateTimeOffset date) => date.ToLocalTime().ToString("t", Loc.Culture);

    /// <summary>"Mon, Oct 5", ordered for the language.</summary>
    public static string WeekdayMonthDay(DateTimeOffset date)
    {
        weekdayMonthDay ??= new DateTimeFormatter("dayofweek.abbreviated month.abbreviated day", [Loc.Language]);
        return weekdayMonthDay.Format(date.ToLocalTime());
    }

    /// <summary>"3 PM" or "15".</summary>
    public static string Hour(int hour)
    {
        hourOnly ??= new DateTimeFormatter("hour", [Loc.Language]);
        return hourOnly.Format(new DateTimeOffset(2026, 1, 5, hour % 24, 0, 0, TimeSpan.Zero), "UTC");
    }

    /// <summary>"3 PM – 4 PM": the hour a quiet hour or a suggestion covers.</summary>
    public static string HourRange(int hour) => $"{Hour(hour)} – {Hour(hour + 1)}";

    /// <summary>"34 min" or "1 hr, 5 min".</summary>
    public static string DurationMinutes(int minutes)
    {
        var k = Keys;
        var hours = minutes / 60;
        var rest = minutes % 60;
        return hours == 0 ? Loc.Plural(k.Minutes, minutes, minutes)
            : rest == 0 ? Loc.Plural(k.Hours, hours, hours)
            : Loc.Plural(k.HoursAndMinutes, hours, hours, rest);
    }

    /// <summary>"A", "A and B", "A, B, and C", with the language's joiners.</summary>
    public static string List(IReadOnlyList<string> items)
    {
        if (items.Count <= 1)
            return items.Count == 0 ? "" : items[0];
        var k = Keys;
        if (items.Count == 2)
            return Loc.Format(k.Pair, items[0], items[1]);
        var head = items.Take(items.Count - 1).Aggregate((list, item) => Loc.Format(k.ListMiddle, list, item));
        return Loc.Format(k.ListEnd, head, items[^1]);
    }

    /// <summary>A whole number with the language's grouping, e.g. "1,250".</summary>
    public static string Number(int value) => value.ToString("N0", Loc.Culture);

    private static FormatKeys Keys =>
        keys ?? throw new InvalidOperationException("Call Formats.Configure with the app's catalog keys first.");
}
