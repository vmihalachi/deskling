using System.Text.Json.Nodes;
using Deskling.Companion.Phrases;

namespace Deskling.Testing.Conformance;

/// <summary>
/// Replays a phrases.json (Swift's <c>PhrasesVector.verify</c>): every entry must match the bank's phrase at its
/// position (id, kind and its fields, sorted tags, weight, flavor, sorted skins, languages), nothing may be missing or
/// extra, and the tag tables, the app's <c>flavors</c> and <c>recentWindow</c> must agree with the code.
/// </summary>
public static class PhrasesReplay
{
    /// <summary>The problems found, empty when the file agrees with <paramref name="bank"/>.</summary>
    /// <param name="flavors">The app's flavors in its display order, as the Swift side wrote them.</param>
    public static IReadOnlyList<string> Verify(Stream json, string file, PhraseBank bank, IReadOnlyList<FlavorId> flavors)
    {
        var committed = JsonNode.Parse(json)!.AsObject();
        var current = Vector(bank, flavors);
        var problems = new List<string>();
        var entries = committed["phrases"]!.AsArray();
        var expected = current["phrases"]!.AsArray();
        if (entries.Count != expected.Count)
            problems.Add($"{file}: {entries.Count} phrases, the bank has {expected.Count}");
        for (var i = 0; i < Math.Min(entries.Count, expected.Count); i++)
        {
            if (!VectorSupport.Same(entries[i], expected[i]))
                problems.Add($"{file}: {entries[i]?["id"]} differs from the bank");
        }
        foreach (var key in new[] { "tags", "flavors", "eventTags", "dayparts", "weekdays", "recentWindow" })
        {
            if (!VectorSupport.Same(committed[key], current[key]))
                problems.Add($"{file}: {key} differs");
        }
        if (committed.Count != current.Count)
            problems.Add($"{file}: {committed.Count} top-level fields, expected {current.Count}");
        return problems;
    }

    /// <summary>The file Swift's <c>PhrasesVector.vector</c> writes for <paramref name="bank"/>, as a tree.</summary>
    public static JsonObject Vector(PhraseBank bank, IReadOnlyList<FlavorId> flavors)
    {
        var dayparts = new JsonObject();
        foreach (var tag in Tags.Dayparts)
        {
            var hours = Enumerable.Range(0, 24).Where(hour => Tags.Daypart(hour) == tag).ToArray();
            dayparts[tag.RawValue()] = VectorSupport.Array([hours.Min(), hours.Max()]);
        }
        var weekdays = new JsonObject();
        foreach (var tag in Tags.Weekdays)
            weekdays[tag.RawValue()] = VectorSupport.Array(Enumerable.Range(1, 7).Where(day => Tags.WeekdayTag(day) == tag));
        return new JsonObject
        {
            ["phrases"] = new JsonArray(bank.Phrases.Select(p => (JsonNode?)Entry(p)).ToArray()),
            ["tags"] = VectorSupport.Array(Tags.All.Select(t => t.RawValue())),
            ["flavors"] = VectorSupport.Array(flavors.Select(f => f.RawValue)),
            ["eventTags"] = VectorSupport.Array(Tags.Events.Select(t => t.RawValue()).Order(StringComparer.Ordinal)),
            ["dayparts"] = dayparts,
            ["weekdays"] = weekdays,
            ["recentWindow"] = PhrasePicker.RecentWindow,
        };
    }

    /// <summary>One phrase as Swift's <c>PhrasesVector.Entry</c> writes it: absent fields left out.</summary>
    public static JsonObject Entry(Phrase phrase)
    {
        var entry = new JsonObject
        {
            ["id"] = phrase.Id,
            ["tags"] = VectorSupport.Array(phrase.Tags.Select(t => t.RawValue()).Order(StringComparer.Ordinal)),
            ["weight"] = phrase.Weight,
            ["skins"] = VectorSupport.Array(phrase.Skins.Select(s => s.RawValue).Order(StringComparer.Ordinal)),
        };
        if (phrase.Flavor is { } flavor)
            entry["flavor"] = flavor.RawValue;
        if (phrase.Languages.Count > 0)
            entry["languages"] = VectorSupport.Array(phrase.Languages.Order(StringComparer.Ordinal));
        switch (phrase.Kind)
        {
            case PhraseKind.Line line:
                entry["kind"] = "line";
                entry["key"] = phrase.CatalogKey;
                entry["english"] = line.Source;
                break;
            case PhraseKind.Tune tune:
                entry["kind"] = "tune";
                entry["sound"] = tune.Sound;
                break;
            case PhraseKind.Sequence sequence:
                entry["kind"] = "sequence";
                entry["keys"] = VectorSupport.Array(phrase.CatalogKeys);
                entry["parts"] = VectorSupport.Array(sequence.Source);
                break;
        }
        return entry;
    }
}
