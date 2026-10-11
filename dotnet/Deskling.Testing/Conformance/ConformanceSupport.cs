using System.Globalization;

namespace Deskling.Testing.Conformance;

/// <summary>The date and time zone conventions of conformance/README.md, shared by every vector replay.</summary>
public static class ConformanceSupport
{
    /// <summary>Parses ISO 8601 with an offset, with or without fractional seconds.</summary>
    public static DateTimeOffset Date(string s) => DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    /// <summary>An IANA time zone such as "UTC" or "Europe/Bucharest".</summary>
    public static TimeZoneInfo TimeZone(string id) => TimeZoneInfo.FindSystemTimeZoneById(id);

    /// <summary>
    /// ISO 8601 in <paramref name="tz"/> ("2026-09-28T10:00:00+03:00", "Z" for a zero offset), with milliseconds only
    /// when the date has a fraction of a second: the format the Swift generators write.
    /// </summary>
    public static string Iso(DateTimeOffset date, TimeZoneInfo tz)
    {
        var local = TimeZoneInfo.ConvertTime(date, tz);
        var hasFraction = local.Ticks % TimeSpan.TicksPerSecond != 0;
        var format = hasFraction ? "yyyy-MM-dd'T'HH:mm:ss.fff" : "yyyy-MM-dd'T'HH:mm:ss";
        var offset = local.Offset == TimeSpan.Zero ? "Z" : local.ToString("zzz", CultureInfo.InvariantCulture);
        return local.ToString(format, CultureInfo.InvariantCulture) + offset;
    }
}
