using Laching.Companion.Phrases;
using Laching.Companion.Shell.Speech;

namespace Laching.Companion.Shell.Performance;

/// <summary>Swift's <c>VoiceLibrary+Bank</c>: what a recorded voice can't say yet, phrase by phrase.</summary>
public static class VoiceLibraryBank
{
    /// <summary>
    /// The bank's phrases in this language that <paramref name="voice"/> can't say yet: a line whose clip is missing or
    /// of older text, a joke with any such part. <paramref name="text"/> gives the words the user sees for a catalog key
    /// (the clips record the translation). Tunes are sounds and always fine; none with
    /// <see cref="VoiceLibrary.FallbackToSystemVoice"/>.
    /// </summary>
    public static IReadOnlySet<string> PendingIds(this VoiceLibrary voices, PhraseBank bank, VoiceId voice, Func<string, string, string> text)
    {
        var pending = new HashSet<string>();
        foreach (var phrase in bank.InLanguage(voices.LanguageCode))
        {
            IReadOnlyList<string> sources = phrase.Kind switch
            {
                PhraseKind.Line line => [line.Source],
                PhraseKind.Sequence sequence => sequence.Source,
                _ => [],
            };
            if (sources.Count == 0)
                continue;
            var clips = phrase.ClipIDs.Zip(phrase.CatalogKeys, sources).Select(c => (c.First, text(c.Second, c.Third)));
            if (voices.PendingClipIds(clips, voice).Count > 0)
                pending.Add(phrase.Id);
        }
        return pending;
    }
}
