namespace Deskling.Companion.Cadence;

/// <summary>How often the character pipes up when left alone. The raw values are stored in settings; never rename one.</summary>
public enum Frequency
{
    /// <summary>About once an hour.</summary>
    Rarely,

    /// <summary>About twice an hour: the default.</summary>
    Sometimes,

    /// <summary>About four times an hour.</summary>
    Chatty,
}

/// <summary>The preset table and raw values (Swift's <c>Frequency.interval</c>, <c>jitter</c>, <c>rawValue</c>).</summary>
public static class Frequencies
{
    /// <summary>Every preset, in declaration order.</summary>
    public static IReadOnlyList<Frequency> All { get; } = Enum.GetValues<Frequency>();

    /// <summary>The base interval between lines, in seconds.</summary>
    public static double IntervalSeconds(this Frequency frequency) =>
        frequency switch
        {
            Frequency.Rarely => 60 * 60,
            Frequency.Sometimes => 30 * 60,
            Frequency.Chatty => 15 * 60,
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "unknown frequency"),
        };

    /// <summary>Each interval is offset by a random amount within ±jitter seconds, so the character never feels like a clock.</summary>
    public static double JitterSeconds(this Frequency frequency) =>
        frequency switch
        {
            Frequency.Rarely => 15 * 60,
            Frequency.Sometimes => 10 * 60,
            Frequency.Chatty => 5 * 60,
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "unknown frequency"),
        };

    /// <summary>The Swift raw value: the stored name, also in settings.json and the vectors.</summary>
    public static string RawValue(this Frequency frequency) =>
        frequency switch
        {
            Frequency.Rarely => "rarely",
            Frequency.Sometimes => "sometimes",
            Frequency.Chatty => "chatty",
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "unknown frequency"),
        };

    /// <summary>The inverse of <see cref="RawValue"/>: null for an unknown name.</summary>
    public static Frequency? Parse(string? rawValue) =>
        rawValue switch
        {
            "rarely" => Frequency.Rarely,
            "sometimes" => Frequency.Sometimes,
            "chatty" => Frequency.Chatty,
            _ => null,
        };
}
