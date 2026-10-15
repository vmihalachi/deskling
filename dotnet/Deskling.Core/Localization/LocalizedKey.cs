using System.Text.Json.Serialization;

namespace Deskling.Core.Localization;

/// <summary>
/// A localized string as a catalog key plus its arguments. Pure code returns these and the UI layer localizes
/// them, so conformance vectors never contain display text. Two keys are equal when their keys and arguments are.
/// </summary>
public sealed record LocalizedKey
{
    [JsonConstructor]
    public LocalizedKey(string key, IReadOnlyList<Argument> args)
    {
        Key = key;
        Args = args;
    }

    public LocalizedKey(string key)
        : this(key, Array.Empty<Argument>())
    {
    }

    public string Key { get; }

    public IReadOnlyList<Argument> Args { get; }

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
    /// What an argument is, and so how the UI layer formats it. Serialized by name ("int", "durationMinutes"), the
    /// same strings the Swift side writes.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ArgumentKind>))]
    public enum ArgumentKind
    {
        /// <summary>A number, also the plural count.</summary>
        [JsonStringEnumMemberName("int")]
        Int,

        /// <summary>
        /// Minutes, formatted with the platform's abbreviated hours/minutes duration style, e.g. "34 min",
        /// "1 hr, 5 min".
        /// </summary>
        [JsonStringEnumMemberName("durationMinutes")]
        DurationMinutes,
    }

    /// <summary>One argument: its <see cref="ArgumentKind"/> and the number it carries.</summary>
    public sealed record Argument(ArgumentKind Type, int Value)
    {
        public static Argument Int(int value) => new(ArgumentKind.Int, value);

        public static Argument DurationMinutes(int value) => new(ArgumentKind.DurationMinutes, value);
    }
}

/// <summary>
/// Reads and writes <see cref="LocalizedKey"/> as JSON with the same shape as the Swift side (camel-case names,
/// kinds by name). Source-generated, so apps can compile it with Native AOT.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(LocalizedKey))]
public sealed partial class LocalizedKeyJson : JsonSerializerContext;
