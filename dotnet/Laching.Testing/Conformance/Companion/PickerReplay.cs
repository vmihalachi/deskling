using System.Globalization;
using System.Text.Json;
using Laching.Companion.Phrases;

namespace Laching.Testing.Conformance;

/// <summary>
/// Replays a <c>picker/*.json</c> (Swift's <c>PickerVector.verify</c>): the <c>context</c> (an absent
/// <c>language</c> is "en") must yield the committed <c>candidates</c> in bank order, and each draw's <c>unit</c> the
/// committed <c>id</c> (absent when nothing qualifies).
/// </summary>
public static class PickerReplay
{
    /// <summary>The problems found, empty when the picker agrees with the file.</summary>
    public static IReadOnlyList<string> Verify(Stream json, string file, PhraseBank bank)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var context = ReadContext(root.GetProperty("context"));
        var problems = new List<string>();
        var candidates = PhrasePicker.Candidates(bank, context).Select(p => p.Id).ToArray();
        var expected = VectorSupport.Strings(root.GetProperty("candidates"));
        if (!candidates.SequenceEqual(expected))
            problems.Add($"{file}: candidates [{string.Join(", ", candidates)}] != [{string.Join(", ", expected)}]");
        foreach (var draw in root.GetProperty("draws").EnumerateArray())
        {
            var unit = draw.GetProperty("unit").GetDouble();
            var id = PhrasePicker.Pick(bank, context, unit)?.Id;
            var expectedID = VectorSupport.OptionalString(draw, "id");
            if (id != expectedID)
                problems.Add($"{file}: unit {unit.ToString(CultureInfo.InvariantCulture)} picked {id ?? "nil"}, expected {expectedID ?? "nil"}");
        }
        return problems;
    }

    /// <summary>A vector's <c>context</c> object.</summary>
    public static PhraseContext ReadContext(JsonElement c) =>
        new(
            c.GetProperty("hour").GetInt32(),
            c.GetProperty("weekday").GetInt32(),
            c.GetProperty("chimeEnabled").GetBoolean(),
            new SkinId(c.GetProperty("skin").GetString()!),
            VectorSupport.OptionalString(c, "language") ?? "en"
        )
        {
            Event = VectorSupport.OptionalString(c, "event") is { } name
                ? Tags.Parse(name) ?? throw new InvalidDataException($"unknown tag {name}")
                : null,
            RecentIDs = VectorSupport.Strings(c.GetProperty("recentIDs")),
            Flavors = VectorSupport.Strings(c.GetProperty("flavors")).Select(f => new FlavorId(f)).ToHashSet(),
            ExcludedIDs = VectorSupport.Strings(c.GetProperty("excludedIDs")).ToHashSet(StringComparer.Ordinal),
        };
}
