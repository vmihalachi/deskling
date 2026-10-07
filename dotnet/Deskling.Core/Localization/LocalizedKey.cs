namespace Deskling.Core.Localization;

/// <summary>
/// A localized string as a catalog key plus its arguments. Pure code returns these and the UI layer localizes
/// them, so conformance vectors never contain display text. Two keys are equal when their keys and arguments are.
/// </summary>
public sealed record LocalizedKey(string Key, IReadOnlyList<LocalizedKey.Argument> Args)
{
    public LocalizedKey(string key)
        : this(key, Array.Empty<Argument>())
    {
    }

    public bool Equals(LocalizedKey? other) =>
        other is not null && Key == other.Key && Args.SequenceEqual(other.Args);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Key);
        foreach (var arg in Args)
            hash.Add(arg);
        return hash.ToHashCode();
    }

    /// <summary>
    /// One argument: <see cref="Type"/> is "int" (a number, also the plural count) or "durationMinutes" (format
    /// with the platform's abbreviated hours/minutes duration style, e.g. "34 min", "1 hr, 5 min").
    /// </summary>
    public sealed record Argument(string Type, int Value)
    {
        public static Argument Int(int value) => new("int", value);

        public static Argument DurationMinutes(int value) => new("durationMinutes", value);
    }
}
