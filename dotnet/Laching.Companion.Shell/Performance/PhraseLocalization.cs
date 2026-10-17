using Laching.Companion.Phrases;

namespace Laching.Companion.Shell.Performance;

/// <summary>
/// A phrase in the user's language (Swift's <c>Phrase+Localized</c>), through the app's lookup: <c>localize(catalogKey,
/// source)</c> returns the app's text for a catalog key (<c>phrase.&lt;id&gt;</c>, <c>.1</c>, <c>.2</c>), the bank's
/// source when it has none. On Windows the app passes <c>Loc.Localize</c> over its <c>.resw</c>.
/// </summary>
public static class PhraseLocalization
{
    /// <summary>One string per part (a setup and its punchline, or a single line); empty for a tune.</summary>
    public static IReadOnlyList<string> LocalizedParts(this Phrase phrase, Func<string, string, string> localize) =>
        phrase.Kind switch
        {
            PhraseKind.Line line => [localize(phrase.CatalogKey, line.Source)],
            PhraseKind.Sequence sequence => phrase.CatalogKeys.Zip(sequence.Source, localize).ToArray(),
            _ => [],
        };

    /// <summary>The whole phrase on one line (both parts of a joke); null for a tune.</summary>
    public static string? LocalizedText(this Phrase phrase, Func<string, string, string> localize)
    {
        var parts = phrase.LocalizedParts(localize);
        return parts.Count == 0 ? null : string.Join(" ", parts);
    }
}
