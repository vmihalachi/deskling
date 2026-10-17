using Laching.Companion.Coordination;
using Laching.Companion.Phrases;
using Laching.Companion.Rules;
using Laching.Companion.Settings;
using Laching.Companion.Shell.Speech;

namespace Laching.Companion.Shell.Performance;

/// <summary>
/// Turns a <see cref="Coordination.Delivery"/> into what the user sees and hears: the bubble text (part by part for a
/// joke), the blip and the words through a <see cref="SpeechSequencer"/>, a tune on the chime, and one notification when
/// the punchline lands (both parts on two lines). A line is said with its recorded clip in the given voice when the
/// language has a recorded voice (a line without a current recording stays unsaid unless the library falls back to the
/// system voice), else with the system voice. Without speech the parts follow each other after
/// <see cref="PhraseTiming.PartDelaySeconds"/>. <see cref="IsSpeaking"/> holds from the first part until the last
/// finished, beats included, so the app's <c>Snapshot.IsSpeaking</c> keeps other lines out; a call, Shh or a button press
/// mid-line cuts the whole joke with <see cref="Stop"/>. The app supplies the words (<see cref="Localize"/>, from its strings) and the tune's
/// bubble text; the package ships none. UI thread only; never touches the network.
/// </summary>
public sealed class DeliveryPerformer
{
    private readonly Action<string>? post;
    private readonly ITimers timers;

    /// <summary>Bumped by every delivery and <see cref="Stop"/>, so a blip or a silent part that ends late does nothing.</summary>
    private int generation;

    /// <summary><paramref name="post"/> shows a notification body (null: the app shows none); <paramref name="timers"/> runs the silent parts' and beats' timers.</summary>
    public DeliveryPerformer(PhraseBank bank, ISpeaking speaker, IChimePlaying chime, VoiceLibrary voices, Action<string>? post, ITimers timers)
    {
        Bank = bank;
        Speaker = speaker;
        Chime = chime;
        Voices = voices;
        this.post = post;
        this.timers = timers;
        Sequencer = new SpeechSequencer(speaker, timers)
        {
            OnFinished = () => OnSpeechFinished?.Invoke(),
            OnVoicingChanged = on => OnVoicingChanged?.Invoke(on),
        };
    }

    public PhraseBank Bank { get; }

    public ISpeaking Speaker { get; }

    public IChimePlaying Chime { get; }

    public VoiceLibrary Voices { get; }

    public SpeechSequencer Sequencer { get; }

    /// <summary>The text the user sees for a catalog key; the source text by default.</summary>
    public Func<string, string, string> Localize { get; set; } = (_, source) => source;

    /// <summary>The bubble while a tune plays, such as "♪ hm hm hmm ♪".</summary>
    public Func<string> TuneBubbleText { get; set; } = () => "♪";

    /// <summary>The sound played before the words when the chime channel is on; null plays none.</summary>
    public string? BlipSound { get; set; }

    /// <summary>The bubble shows this text: the line, or the part of a joke being told.</summary>
    public Action<string>? OnBubble { get; set; }

    /// <summary>Speech started (after the blip): show the talking character, watch for calls.</summary>
    public Action? OnSpeechStarted { get; set; }

    /// <summary>The last part was said, or speech was stopped.</summary>
    public Action? OnSpeechFinished { get; set; }

    /// <summary>
    /// Audio started (true) or ended (false), from the sequencer: false in the beat between a setup and its punchline,
    /// so the character's mouth moves only while words are heard.
    /// </summary>
    public Action<bool>? OnVoicingChanged { get; set; }

    /// <summary>From the first part until the last finished, beats included.</summary>
    public bool IsSpeaking => Sequencer.IsActive;

    /// <summary>Audio is playing right now (the character's mouth).</summary>
    public bool IsVoicing => Sequencer.IsVoicing;

    /// <summary>The recorded clip's mouth level (0–3) right now; null when the mouth keeps its own timer. Read it every frame.</summary>
    public int? MouthLevel => Sequencer.MouthLevel;

    /// <summary>Performs <paramref name="delivery"/> as its plan says, with <paramref name="settings"/>' voice, rate and volume and the recorded <paramref name="voice"/>.</summary>
    public void Perform(Coordination.Delivery delivery, CompanionSettings settings, VoiceId? voice)
    {
        var current = ++generation;
        var phrase = delivery.Phrase;
        var plan = delivery.Plan;
        if (phrase.Kind is PhraseKind.Tune tune)
        {
            OnBubble?.Invoke(TuneBubbleText());
            Chime.Play(tune.Sound, settings.SpeechVolume, null);
            return;
        }
        var parts = phrase.LocalizedParts(Localize);
        if (parts.Count == 0)
            return;
        var clips = plan.Speak ? Clips(phrase, parts, voice) : null;
        var mouths = phrase.ClipIDs
            .Zip(clips ?? [])
            .Select(pair => pair.Second is null || voice is not { } v ? (IReadOnlyList<int>)[] : Voices.Mouth(pair.First, v))
            .ToArray();
        var notify = plan.Notify ? post : null;
        OnBubble?.Invoke(parts[0]);
        if (parts.Count == 1)
            notify?.Invoke(parts[0]);
        if (clips is null)
        {
            ShowSilently(parts, 0, notify, current);
            return;
        }
        void SpeakNow()
        {
            if (generation != current)
                return;
            Say(parts, clips, mouths, settings, notify);
        }
        if (plan.Chime && BlipSound is { } blip)
            Chime.Play(blip, settings.SpeechVolume, SpeakNow);
        else
            SpeakNow();
    }

    /// <summary>Says <paramref name="phrase"/> now, the way Settings plays a sample: spoken, no chime, no notification.</summary>
    public void Preview(Phrase phrase, CompanionSettings settings, VoiceId? voice)
    {
        Stop();
        Perform(new Coordination.Delivery(phrase, Trigger.ButtonPress, new ChannelPlan(Speak: true, Chime: false, Notify: false)), settings, voice);
    }

    /// <summary>The phrases <paramref name="voice"/> can't say yet in this language: give them to the coordinator's unavailable ids. None without a recorded voice.</summary>
    public IReadOnlySet<string> UnavailableIds(VoiceId? voice) =>
        voice is { } v ? Voices.PendingIds(Bank, v, Localize) : new HashSet<string>();

    /// <summary>Cuts the line or joke being told, and the parts still waiting.</summary>
    public void Stop()
    {
        generation++;
        Sequencer.Stop();
    }

    /// <summary>The clip for each part; null when the line can't be said (a recorded voice lacks one and doesn't fall back).</summary>
    private IReadOnlyList<string?>? Clips(Phrase phrase, IReadOnlyList<string> texts, VoiceId? voice)
    {
        if (voice is not { } v || !Voices.IsVoiced)
            return texts.Select(_ => (string?)null).ToArray();
        var clips = phrase.ClipIDs.Zip(texts, (id, text) => Voices.Clip(id, text, v)).ToArray();
        if (clips.Contains(null) && !Voices.FallbackToSystemVoice)
            return null;
        return clips;
    }

    private void Say(IReadOnlyList<string> parts, IReadOnlyList<string?> clips, IReadOnlyList<IReadOnlyList<int>> mouths, CompanionSettings settings, Action<string>? notify)
    {
        var last = parts.Count - 1;
        Sequencer.OnPartStarted = index =>
        {
            if (index > 0)
                OnBubble?.Invoke(parts[index]);
            if (index == last && last > 0)
                notify?.Invoke(string.Join("\n", parts));
        };
        OnSpeechStarted?.Invoke();
        var speech = parts
            .Select((text, i) => new SpeechPart(text, clips[i], i < mouths.Count ? mouths[i] : [], Voices.MouthFps))
            .ToArray();
        Sequencer.Speak(speech, PhraseTiming.PartBeatSeconds, settings.VoiceIdentifier, settings.SpeechRate, settings.SpeechVolume);
    }

    /// <summary>Shows the parts one after the other, each as long as its bubble would stay.</summary>
    private void ShowSilently(IReadOnlyList<string> parts, int index, Action<string>? notify, int current)
    {
        if (index + 1 >= parts.Count)
            return;
        timers.After(PhraseTiming.PartDelaySeconds(spoken: false, parts[index]), () =>
        {
            if (generation != current)
                return;
            OnBubble?.Invoke(parts[index + 1]);
            if (index + 2 == parts.Count)
                notify?.Invoke(string.Join("\n", parts));
            ShowSilently(parts, index + 1, notify, current);
        });
    }
}
