namespace Deskling.Companion.Cadence;

/// <summary>
/// How often the character pipes up when left alone, from every 15 minutes to every 3 hours. Declared from the most
/// frequent to the least, the order a picker lists them in. The raw values are stored in settings; never rename one
/// (the first three presets keep their original names).
/// </summary>
public enum Frequency
{
    /// <summary>Every 15 minutes or so.</summary>
    Chatty,

    /// <summary>Every 30 minutes or so.</summary>
    Sometimes,

    /// <summary>Every 45 minutes or so.</summary>
    Every45Minutes,

    /// <summary>Every hour or so: the default.</summary>
    Rarely,

    /// <summary>Every 90 minutes or so.</summary>
    Every90Minutes,

    /// <summary>Every 2 hours or so.</summary>
    Every2Hours,

    /// <summary>Every 3 hours or so. Being away restarts the interval, so on most days this means once or twice.</summary>
    Every3Hours,
}

/// <summary>The preset table and raw values (Swift's <c>Frequency.interval</c>, <c>jitter</c>, <c>rawValue</c>).</summary>
public static class Frequencies
{
    /// <summary>Every preset, in declaration order: the most frequent first.</summary>
    public static IReadOnlyList<Frequency> All { get; } = Enum.GetValues<Frequency>();

    /// <summary>The base interval between lines, in seconds.</summary>
    public static double IntervalSeconds(this Frequency frequency) =>
        frequency switch
        {
            Frequency.Chatty => 15 * 60,
            Frequency.Sometimes => 30 * 60,
            Frequency.Every45Minutes => 45 * 60,
            Frequency.Rarely => 60 * 60,
            Frequency.Every90Minutes => 90 * 60,
            Frequency.Every2Hours => 120 * 60,
            Frequency.Every3Hours => 180 * 60,
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "unknown frequency"),
        };

    /// <summary>Each interval is offset by a random amount within ±jitter seconds, so the character never feels like a clock.</summary>
    public static double JitterSeconds(this Frequency frequency) =>
        frequency switch
        {
            Frequency.Chatty => 5 * 60,
            Frequency.Sometimes => 10 * 60,
            Frequency.Every45Minutes => 10 * 60,
            Frequency.Rarely => 15 * 60,
            Frequency.Every90Minutes => 20 * 60,
            Frequency.Every2Hours => 30 * 60,
            Frequency.Every3Hours => 45 * 60,
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "unknown frequency"),
        };

    /// <summary>The Swift raw value: the stored name, also in settings.json and the vectors.</summary>
    public static string RawValue(this Frequency frequency) =>
        frequency switch
        {
            Frequency.Chatty => "chatty",
            Frequency.Sometimes => "sometimes",
            Frequency.Every45Minutes => "every45Minutes",
            Frequency.Rarely => "rarely",
            Frequency.Every90Minutes => "every90Minutes",
            Frequency.Every2Hours => "every2Hours",
            Frequency.Every3Hours => "every3Hours",
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "unknown frequency"),
        };

    /// <summary>The inverse of <see cref="RawValue"/>: null for an unknown name.</summary>
    public static Frequency? Parse(string? rawValue) =>
        rawValue switch
        {
            "chatty" => Frequency.Chatty,
            "sometimes" => Frequency.Sometimes,
            "every45Minutes" => Frequency.Every45Minutes,
            "rarely" => Frequency.Rarely,
            "every90Minutes" => Frequency.Every90Minutes,
            "every2Hours" => Frequency.Every2Hours,
            "every3Hours" => Frequency.Every3Hours,
            _ => null,
        };
}
