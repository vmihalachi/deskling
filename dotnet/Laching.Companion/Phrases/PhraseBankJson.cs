using System.Text.Json;
using System.Text.Json.Serialization;

namespace Laching.Companion.Phrases;

public sealed partial class PhraseBank
{
    /// <summary>
    /// Loads a phrases.json (the format of <c>PhrasesVector</c> in the Swift <c>LachingTesting</c>, which writes it from
    /// the app's Swift bank), so a new line reaches Windows without a port. Strict: an unknown tag, kind or field, a
    /// flavor the file doesn't list, a line or part key that isn't the catalog key, or a sequence of fewer than two
    /// parts fails, so a stale port can't silently fall back. An absent <c>languages</c> means every language.
    /// </summary>
    public static PhraseBank Parse(Stream json)
    {
        PhrasesFile file;
        try
        {
            file =
                JsonSerializer.Deserialize(json, PhrasesJson.Default.PhrasesFile)
                ?? throw new InvalidDataException("phrases.json is empty");
        }
        catch (JsonException error)
        {
            throw new InvalidDataException($"not a phrases.json: {error.Message}", error);
        }
        var flavors = new HashSet<string>(file.Flavors, StringComparer.Ordinal);
        return new PhraseBank(file.Phrases.Select(entry => ToPhrase(entry, flavors)));
    }

    private static Phrase ToPhrase(PhraseEntry entry, HashSet<string> flavors)
    {
        var id = entry.Id;
        PhraseKind kind = entry.Kind switch
        {
            "line" => new PhraseKind.Line(entry.English ?? throw Invalid(id, "a line needs english")),
            "tune" => new PhraseKind.Tune(entry.Sound ?? throw Invalid(id, "a tune needs sound")),
            "sequence" => new PhraseKind.Sequence(entry.Parts ?? throw Invalid(id, "a sequence needs parts")),
            _ => throw Invalid(id, $"unknown kind {entry.Kind}"),
        };
        if (kind is PhraseKind.Sequence { Source.Count: < 2 })
            throw Invalid(id, "a sequence needs at least two parts");
        var tags = entry.Tags.Select(name => Laching.Companion.Phrases.Tags.Parse(name) ?? throw Invalid(id, $"unknown tag {name}"));
        FlavorId? flavor = null;
        if (entry.Flavor is { } name)
            flavor = flavors.Contains(name) ? new FlavorId(name) : throw Invalid(id, $"flavor {name} is not in the file's flavors");
        var phrase = new Phrase(id, kind, tags, entry.Weight, flavor, entry.Skins.Select(s => new SkinId(s)), entry.Languages);
        switch (kind)
        {
            case PhraseKind.Line when entry.Key != phrase.CatalogKey:
                throw Invalid(id, $"key {entry.Key} is not the catalog key");
            case PhraseKind.Sequence when entry.Keys is null || !entry.Keys.SequenceEqual(phrase.CatalogKeys):
                throw Invalid(id, "keys are not the catalog keys of the parts");
        }
        return phrase;
    }

    private static InvalidDataException Invalid(string id, string message) => new($"phrases.json: {id}: {message}");
}

/// <summary>The shape of phrases.json (conformance/README.md).</summary>
internal sealed record PhrasesFile(
    PhraseEntry[] Phrases,
    string[] Tags,
    string[] Flavors,
    string[] EventTags,
    Dictionary<string, int[]> Dayparts,
    Dictionary<string, int[]> Weekdays,
    int RecentWindow
);

/// <summary>
/// One phrase: <c>key</c> and <c>english</c> for a line, <c>keys</c> and <c>parts</c> for a sequence, <c>sound</c> for
/// a tune; the others are left out. <c>skins</c> is always there (empty = every character); <c>languages</c> only
/// when the phrase is scoped to some.
/// </summary>
internal sealed record PhraseEntry(
    string Id,
    string Kind,
    string[] Tags,
    int Weight,
    string[] Skins,
    string? Key = null,
    string? English = null,
    string[]? Keys = null,
    string[]? Parts = null,
    string? Sound = null,
    string? Flavor = null,
    string[]? Languages = null
);

/// <summary>Source-generated, so apps can compile it with Native AOT.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectRequiredConstructorParameters = true
)]
[JsonSerializable(typeof(PhrasesFile))]
internal sealed partial class PhrasesJson : JsonSerializerContext;
