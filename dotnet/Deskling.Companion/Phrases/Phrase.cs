namespace Deskling.Companion.Phrases;

/// <summary>
/// What a line does for the listener's mood; the app names its own flavors and lets the user turn them off. Raw values
/// are public: they appear in phrases.json, the vectors and the settings. Never rename one.
/// </summary>
public readonly record struct FlavorId(string RawValue)
{
    public static implicit operator FlavorId(string rawValue) => new(rawValue);

    public override string ToString() => RawValue;
}

/// <summary>Which character the user picked; the app names its own skins. Raw values are stored and name the skin's art; never rename one.</summary>
public readonly record struct SkinId(string RawValue)
{
    public static implicit operator SkinId(string rawValue) => new(rawValue);

    public override string ToString() => RawValue;
}

/// <summary>What a phrase delivers (Swift's <c>Phrase.Kind</c>).</summary>
public abstract record PhraseKind
{
    private PhraseKind() { }

    /// <summary>
    /// <see cref="Source"/> is the catalog's source text for the key <c>phrase.&lt;id&gt;</c>; the app displays the
    /// catalog's translation, never this string directly.
    /// </summary>
    public sealed record Line(string Source) : PhraseKind;

    /// <summary>A short original tune in the app's sound files, played only when the chime channel is on.</summary>
    public sealed record Tune(string Sound) : PhraseKind;

    /// <summary>
    /// A joke in beats, at least two: a setup and a punchline. Part <i>n</i> is the catalog key
    /// <c>phrase.&lt;id&gt;.&lt;n&gt;</c>, counted from 1. Equal when every part is.
    /// </summary>
    public sealed record Sequence(IReadOnlyList<string> Source) : PhraseKind
    {
        public bool Equals(Sequence? other) => other is not null && Source.SequenceEqual(other.Source);

        public override int GetHashCode() => Source.Count;
    }
}

/// <summary>
/// One thing the character can say: a spoken line (a String Catalog key, resolved by the app), a joke told in beats
/// (one key per part) or a hummed tune (a sound file name). <see cref="Id"/> is stable and public: it is persisted in
/// the recent list and the muted list, written to phrases.json and used in the catalog keys. Never rename or reuse
/// one. Recents, muting and the quiet rules are keyed on the id, so a joke counts as one phrase. Two phrases are equal
/// when every part is.
/// </summary>
public sealed record Phrase
{
    /// <summary>A <see cref="PhraseKind.Sequence"/> needs at least two parts.</summary>
    public Phrase(
        string id,
        PhraseKind kind,
        IEnumerable<Tag> tags,
        int weight = 3,
        FlavorId? flavor = null,
        IEnumerable<SkinId>? skins = null,
        IEnumerable<string>? languages = null
    )
    {
        if (kind is PhraseKind.Sequence { Source.Count: < 2 })
            throw new ArgumentException($"Phrase {id}: a sequence needs at least two parts", nameof(kind));
        Id = id;
        Kind = kind;
        Tags = new HashSet<Tag>(tags);
        Weight = weight;
        Flavor = flavor;
        Skins = new HashSet<SkinId>(skins ?? []);
        Languages = new HashSet<string>(languages ?? [], StringComparer.Ordinal);
    }

    public string Id { get; }

    public PhraseKind Kind { get; }

    public IReadOnlySet<Tag> Tags { get; }

    /// <summary>1 (rare) … 5 (a favorite); 3 is normal. Doubled when a daypart tag matches the moment.</summary>
    public int Weight { get; }

    /// <summary>Lines and sequences have one; tunes have none.</summary>
    public FlavorId? Flavor { get; }

    /// <summary>The characters that can say it; empty means every character.</summary>
    public IReadOnlySet<SkinId> Skins { get; }

    /// <summary>
    /// The languages it exists in, as BCP-47 base codes ("it"); empty means every language. A joke that only works in
    /// Italian is written in Italian and listed for <c>["it"]</c> only.
    /// </summary>
    public IReadOnlySet<string> Languages { get; }

    public static PhraseKind Line(string source) => new PhraseKind.Line(source);

    public static PhraseKind Tune(string sound) => new PhraseKind.Tune(sound);

    public static PhraseKind Sequence(params string[] source) => new PhraseKind.Sequence(source);

    /// <summary>How many beats it is delivered in: the parts of a sequence, otherwise 1.</summary>
    public int PartCount => Kind is PhraseKind.Sequence sequence ? sequence.Source.Count : 1;

    /// <summary>The String Catalog key of a spoken line; the stem of a sequence's part keys.</summary>
    public string CatalogKey => "phrase." + Id;

    /// <summary>
    /// The catalog keys whose text is spoken, in order: <c>phrase.&lt;id&gt;</c> for a line, <c>phrase.&lt;id&gt;.1</c>,
    /// <c>.2</c>, … for a sequence, none for a tune.
    /// </summary>
    public IReadOnlyList<string> CatalogKeys => Numbered(CatalogKey);

    /// <summary>The ids of its recorded clips, in order: <c>&lt;id&gt;</c> for a line, <c>&lt;id&gt;.1</c>, <c>.2</c>, … for a sequence, none for a tune.</summary>
    public IReadOnlyList<string> ClipIDs => Numbered(Id);

    public bool IsTune => Kind is PhraseKind.Tune;

    // Phrases.Tags: the static class, not this phrase's Tags property.
    public bool HasEventTag => Tags.Overlaps(Phrases.Tags.Events);

    /// <summary>Whether the character <paramref name="skin"/> can say it.</summary>
    public bool Fits(SkinId skin) => Skins.Count == 0 || Skins.Contains(skin);

    /// <summary>Whether it exists in <paramref name="language"/>, a BCP-47 code compared by its base ("pt-BR" fits <c>["pt"]</c>).</summary>
    public bool FitsLanguage(string language) => Languages.Count == 0 || Languages.Contains(BaseLanguage(language));

    /// <summary>The base of a BCP-47 code, lowercased: "pt-BR" and "pt_BR" → "pt".</summary>
    public static string BaseLanguage(string code)
    {
        var end = code.IndexOfAny(['-', '_']);
        return (end < 0 ? code : code[..end]).ToLowerInvariant();
    }

    public bool Equals(Phrase? other) =>
        other is not null
        && Id == other.Id
        && Kind == other.Kind
        && Weight == other.Weight
        && Flavor == other.Flavor
        && Tags.SetEquals(other.Tags)
        && Skins.SetEquals(other.Skins)
        && Languages.SetEquals(other.Languages);

    public override int GetHashCode() => HashCode.Combine(Id, Kind, Weight, Tags.Count);

    private string[] Numbered(string stem) =>
        Kind switch
        {
            PhraseKind.Line => [stem],
            PhraseKind.Sequence sequence => Enumerable.Range(1, sequence.Source.Count).Select(n => $"{stem}.{n}").ToArray(),
            _ => [],
        };
}
