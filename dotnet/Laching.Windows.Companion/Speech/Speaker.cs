using Laching.Companion.Shell.Speech;
using Laching.Windows.Shell.Localization;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace Laching.Windows.Companion.Speech;

/// <summary>
/// A character's voice (Swift's <c>Speaker</c>): a recorded clip (<see cref="VoiceLibrary"/>) when there is one, else
/// Windows' speech synthesizer with the user's voice, both played through a <see cref="MediaPlayer"/> kept out of the
/// system media controls. The user's rate and volume apply to both. One line at a time; <see cref="Stop"/> cuts a line
/// mid-word (a call started). Never records anything and asks for no permission. Used on the UI thread: create it
/// there (or pass that thread's <see cref="SynchronizationContext"/>), and its callbacks come back there.
/// </summary>
public sealed class Speaker : ISpeaking, IDisposable
{
    private readonly SynchronizationContext ui;

    /// <summary>The line playing; a late event from any earlier one is ignored by its generation.</summary>
    private MediaPlayer? player;

    private bool playingClip;

    /// <summary>Bumped by every line and every stop, so a synthesis or a player event that arrives late is ignored.</summary>
    private int generation;

    /// <param name="language">The language whose voice speaks when the user chose none; the app's (<see cref="Loc.Language"/>) by default.</param>
    /// <param name="ui">Where callbacks run; the current thread's context by default.</param>
    public Speaker(string? language = null, SynchronizationContext? ui = null)
    {
        Language = language ?? Loc.Language;
        this.ui =
            ui
            ?? SynchronizationContext.Current
            ?? throw new InvalidOperationException("Create the speaker on the UI thread, or pass its SynchronizationContext.");
    }

    public string Language { get; }

    public bool IsSpeaking { get; private set; }

    public Action? OnFinished { get; set; }

    /// <summary>How far into the playing clip it is, in the clip's own seconds (the rate doesn't change it); null for the system voice.</summary>
    public double? ClipTime => IsSpeaking && playingClip && player is { } current ? current.PlaybackSession.Position.TotalSeconds : null;

    public void Speak(string text, string? clip, string? voiceIdentifier, double rate, double volume)
    {
        Stop();
        var current = ++generation;
        if (clip is not null)
        {
            // A clip that isn't there ends the line at once, so nobody waits for it.
            if (!File.Exists(clip))
            {
                OnFinished?.Invoke();
                return;
            }
            IsSpeaking = true;
            playingClip = true;
            Play(current, MediaPlayers.File(clip), SpeechRates.ClipRate(rate), volume);
            return;
        }
        IsSpeaking = true;
        playingClip = false;
        _ = Synthesize(current, text, Voice(voiceIdentifier, Language), SpeechRate(rate), volume);
    }

    public void Stop()
    {
        if (!IsSpeaking)
            return;
        generation++;
        IsSpeaking = false;
        Release();
        OnFinished?.Invoke();
    }

    /// <summary>Lets go of the player without reporting a finish.</summary>
    public void Dispose()
    {
        generation++;
        IsSpeaking = false;
        Release();
    }

    /// <summary>
    /// The settings' 0…1 scale onto the synthesizer's <see cref="SpeechSynthesizerOptions.SpeakingRate"/>: 0.5 is
    /// Windows' normal rate (1×), the ends half and twice as fast (the synthesizer goes to 6×, past understanding).
    /// </summary>
    public static double SpeechRate(double unit)
    {
        unit = Math.Clamp(unit, 0, 1);
        return unit <= 0.5 ? 0.5 + 0.5 * (unit / 0.5) : 1 + (unit - 0.5) / 0.5;
    }

    /// <summary>The chosen voice, or the first of <see cref="Voices"/> for <paramref name="language"/> (null leaves it to Windows).</summary>
    public static VoiceInformation? Voice(string? identifier, string language)
    {
        if (identifier is not null && SpeechSynthesizer.AllVoices.FirstOrDefault(voice => voice.Id == identifier) is { } chosen)
            return chosen;
        return Voices(language).FirstOrDefault();
    }

    /// <summary>
    /// Installed voices for a language (the Settings voice picker): "pt-BR" lists pt-BR voices first and other
    /// Portuguese voices after; among those, Windows' default voice first, then by name.
    /// </summary>
    public static IReadOnlyList<VoiceInformation> Voices(string language)
    {
        var fallback = SpeechSynthesizer.DefaultVoice?.Id;
        return Rank(SpeechSynthesizer.AllVoices, language, voice => voice.Language, voice => voice.DisplayName, voice => voice.Id == fallback);
    }

    internal static List<T> Rank<T>(
        IEnumerable<T> voices,
        string code,
        Func<T, string> language,
        Func<T, string> name,
        Func<T, bool> isDefault
    )
    {
        var wanted = code.ToLowerInvariant();
        var bareLanguage = wanted.Split('-')[0];
        return
        [
            .. voices
                .Where(voice => language(voice).ToLowerInvariant().Split('-')[0] == bareLanguage)
                .OrderByDescending(voice => language(voice).Equals(wanted, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(isDefault)
                .ThenBy(name, StringComparer.CurrentCultureIgnoreCase),
        ];
    }

    /// <summary>Synthesizes off the UI thread, then plays the result there unless a newer line or a stop came first.</summary>
    private async Task Synthesize(int current, string text, VoiceInformation? voice, double speakingRate, double volume)
    {
        SpeechSynthesisStream? stream = null;
        try
        {
            using var synthesizer = new SpeechSynthesizer();
            if (voice is not null)
                synthesizer.Voice = voice;
            synthesizer.Options.SpeakingRate = speakingRate;
            stream = await synthesizer.SynthesizeTextToStreamAsync(text).AsTask().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // No voice installed, or one that can't say this: the line ends, as a broken clip's does.
        }
        Post(() =>
        {
            if (current != generation || !IsSpeaking)
            {
                stream?.Dispose();
                return;
            }
            if (stream is null)
            {
                Ended(current);
                return;
            }
            Play(current, MediaSource.CreateFromStream(stream, stream.ContentType), 1, volume);
        });
    }

    private void Play(int current, MediaSource source, double playbackRate, double volume)
    {
        var next = MediaPlayers.Create(MediaPlayerAudioCategory.Speech, volume, () => Post(() => Ended(current)));
        // Opening a source resets the rate, so it's set once the clip is open (MediaPlayer keeps the pitch).
        next.MediaOpened += (sender, _) => sender.PlaybackSession.PlaybackRate = playbackRate;
        player = next;
        next.Source = source;
        next.Play();
    }

    private void Ended(int current)
    {
        if (current != generation || !IsSpeaking)
            return;
        IsSpeaking = false;
        Release();
        OnFinished?.Invoke();
    }

    private void Release()
    {
        if (player is null)
            return;
        MediaPlayers.Release(player);
        player = null;
    }

    private void Post(Action work) => ui.Post(_ => work(), null);
}
