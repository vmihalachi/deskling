namespace Laching.Companion.Phrases;

/// <summary>
/// Everything the character can say, in a stable order: the app builds one from its own lines (or loads its
/// phrases.json with <see cref="Parse"/>) and injects it. The order matters (the picker draws in bank order), and ids
/// are permanent.
/// </summary>
public sealed partial class PhraseBank
{
    public PhraseBank(IEnumerable<Phrase> phrases)
    {
        Phrases = phrases.ToArray();
    }

    public IReadOnlyList<Phrase> Phrases { get; }

    public Phrase? this[string id] => Phrases.FirstOrDefault(p => p.Id == id);

    /// <summary>The phrases carrying <paramref name="tag"/>, in bank order (Swift's <c>phrases(tagged:)</c>).</summary>
    public IReadOnlyList<Phrase> Tagged(Tag tag) => Phrases.Where(p => p.Tags.Contains(tag)).ToArray();

    /// <summary>The phrases the character <paramref name="skin"/> can say, in bank order (Swift's <c>phrases(for:)</c>).</summary>
    public IReadOnlyList<Phrase> For(SkinId skin) => Phrases.Where(p => p.Fits(skin)).ToArray();

    /// <summary>
    /// The phrases that exist in <paramref name="language"/> (a BCP-47 code, compared by its base), in bank order
    /// (Swift's <c>phrases(in:)</c>).
    /// </summary>
    public IReadOnlyList<Phrase> InLanguage(string language) => Phrases.Where(p => p.FitsLanguage(language)).ToArray();
}
