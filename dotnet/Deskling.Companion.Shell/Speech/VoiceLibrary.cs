using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deskling.Companion.Shell.Speech;

/// <summary>
/// A recorded voice of the app's character, such as "male" or "female": the app names its own. Raw values are stored
/// in settings and name the folders under <c>Voice/&lt;lang&gt;/</c>; never rename one.
/// </summary>
public readonly record struct VoiceId(string RawValue)
{
    public static implicit operator VoiceId(string rawValue) => new(rawValue);

    public override string ToString() => RawValue;
}

/// <summary>
/// A character's recorded voices: <c>Voice/&lt;lang&gt;/&lt;voice&gt;/&lt;clip id&gt;.m4a</c> and <c>Voice/manifest.json</c>
/// (format 1, made by <c>tools/voice.py</c>). Says which clip speaks a text in the app's language and a voice, and which
/// clips are pending there (none, or one of older text), so the app never picks a line it can't say. A clip counts only
/// when its manifest <c>textHash</c> matches the current text, so the bubble and the audio never disagree. A language
/// marked <c>testing</c> plays only when <c>allowTesting</c> (the app's Debug builds); a language without a recorded voice
/// keeps the system voice. With <see cref="FallbackToSystemVoice"/>, nothing is pending: a missing clip is spoken by the
/// system voice instead. Clip ids are the phrase's (<c>&lt;id&gt;</c>, or <c>&lt;id&gt;.1</c>, <c>&lt;id&gt;.2</c> for the
/// parts of a joke); the library knows no bank. Reads files only; never touches the network.
/// </summary>
public sealed class VoiceLibrary
{
    private readonly string? folder;
    private readonly VoiceManifest.Language? language;

    /// <summary>
    /// <paramref name="folder"/> holds <c>manifest.json</c> and <c>&lt;lang&gt;/&lt;voice&gt;/&lt;id&gt;.m4a</c>; null (or
    /// no manifest) means no recorded voice. The apps pass the <c>Voice</c> folder next to their executable.
    /// </summary>
    public VoiceLibrary(string? folder, string language, bool allowTesting, bool fallbackToSystemVoice = false)
    {
        var manifest = Load(folder);
        var entry = manifest?.Languages.GetValueOrDefault(language);
        LanguageCode = language;
        MouthFps = manifest?.MouthFps ?? 12.5;
        FallbackToSystemVoice = fallbackToSystemVoice;
        this.folder = folder;
        if (entry is not null && (entry.Status == "shipping" || (allowTesting && entry.Status == "testing")))
            this.language = entry;
    }

    /// <summary>No recorded voice: the system voice says everything.</summary>
    public static VoiceLibrary None(string language) => new(null, language, allowTesting: false);

    public string LanguageCode { get; }

    /// <summary>Mouth levels per second in the manifest (12.5 when there is none).</summary>
    public double MouthFps { get; }

    /// <summary>A missing clip is spoken by the system voice instead of keeping its line back.</summary>
    public bool FallbackToSystemVoice { get; }

    /// <summary>
    /// The voices with clips in this language, the language's default first, then by name (ordinal). Empty when the
    /// language has no recorded voice.
    /// </summary>
    public IReadOnlyList<VoiceId> AvailableVoices
    {
        get
        {
            if (language is null)
                return [];
            return language.Voices
                .Where(v => v.Value.Clips.Count > 0)
                .Select(v => v.Key)
                .OrderBy(name => name == language.Default ? 0 : 1)
                .ThenBy(name => name, StringComparer.Ordinal)
                .Select(name => new VoiceId(name))
                .ToArray();
        }
    }

    /// <summary>The language speaks with a recorded voice.</summary>
    public bool IsVoiced => AvailableVoices.Count > 0;

    /// <summary>The voice that speaks: <paramref name="preferred"/> when this language has it, else the language's default.</summary>
    public VoiceId? Voice(VoiceId? preferred)
    {
        var voices = AvailableVoices;
        if (preferred is { } wanted && voices.Contains(wanted))
            return wanted;
        return voices.Count > 0 ? voices[0] : (VoiceId?)null;
    }

    /// <summary>The path of the clip that says <paramref name="text"/> as clip <paramref name="id"/> in <paramref name="voice"/>, or null when there's none for this exact text.</summary>
    public string? Clip(string id, string text, VoiceId voice)
    {
        if (folder is null || ClipEntry(id, voice) is not { } entry || entry.TextHash != Hash(text))
            return null;
        var path = Path.Combine(folder, LanguageCode, voice.RawValue, id + ".m4a");
        return File.Exists(path) ? path : null;
    }

    /// <summary>The clip's mouth levels (0–3), one per 1/<see cref="MouthFps"/> s.</summary>
    public IReadOnlyList<int> Mouth(string id, VoiceId voice) =>
        ClipEntry(id, voice)?.Mouth.Where(char.IsAsciiDigit).Select(c => c - '0').ToArray() ?? [];

    /// <summary>The ids among <paramref name="clips"/> that <paramref name="voice"/> can't say yet in this language; none with <see cref="FallbackToSystemVoice"/>.</summary>
    public IReadOnlySet<string> PendingClipIds(IEnumerable<(string Id, string Text)> clips, VoiceId voice)
    {
        if (FallbackToSystemVoice)
            return new HashSet<string>();
        return clips.Where(c => Clip(c.Id, c.Text, voice) is null).Select(c => c.Id).ToHashSet();
    }

    /// <summary>The manifest's <c>textHash</c>: SHA-256 of the UTF-8 text, lowercase hex.</summary>
    public static string Hash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private VoiceManifest.Clip? ClipEntry(string id, VoiceId voice) =>
        language?.Voices.GetValueOrDefault(voice.RawValue)?.Clips.GetValueOrDefault(id);

    private static VoiceManifest? Load(string? folder)
    {
        if (folder is null)
            return null;
        try
        {
            using var stream = File.OpenRead(Path.Combine(folder, "manifest.json"));
            return JsonSerializer.Deserialize(stream, VoiceManifestJson.Default.VoiceManifest);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }
}

internal sealed record VoiceManifest(
    int Format,
    [property: JsonPropertyName("mouthFPS")] double MouthFps,
    IReadOnlyDictionary<string, VoiceManifest.Language> Languages)
{
    internal sealed record Language(string Status, string Default, IReadOnlyDictionary<string, Voice> Voices);

    internal sealed record Voice(IReadOnlyDictionary<string, Clip> Clips);

    internal sealed record Clip(string TextHash, double Duration, string Mouth);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(VoiceManifest))]
internal sealed partial class VoiceManifestJson : JsonSerializerContext;
