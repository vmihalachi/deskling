namespace Laching.Companion.Shell.Speech;

/// <summary>
/// One beat of a spoken joke: its text (shown and, without a clip, synthesized), its recorded clip's path, if any, and
/// the clip's mouth envelope (<see cref="VoiceLibrary.Mouth"/>: a level 0–3 per 1/<see cref="MouthFps"/> s; empty for none).
/// </summary>
public sealed record SpeechPart
{
    public SpeechPart(string text, string? clip = null, IReadOnlyList<int>? mouth = null, double mouthFps = 12.5)
    {
        Text = text;
        Clip = clip;
        Mouth = mouth ?? [];
        MouthFps = mouthFps;
    }

    public string Text { get; init; }

    public string? Clip { get; init; }

    public IReadOnlyList<int> Mouth { get; init; }

    public double MouthFps { get; init; }

    public bool Equals(SpeechPart? other) =>
        other is not null && Text == other.Text && Clip == other.Clip && MouthFps == other.MouthFps && Mouth.SequenceEqual(other.Mouth);

    public override int GetHashCode() => HashCode.Combine(Text, Clip, Mouth.Count, MouthFps);
}

/// <summary>
/// Speaks a line in parts through an <see cref="ISpeaking"/> voice, with a silent beat between them: a setup, a pause,
/// the punchline. It owns the voice's <see cref="ISpeaking.OnFinished"/> while it runs. From the first part until the
/// last one finished it is <see cref="IsActive"/>, beats included, so nothing else starts in the pause (the app's
/// <c>Snapshot.IsSpeaking</c>); <see cref="IsVoicing"/> is whether audio is playing right now (the character's mouth),
/// false during a beat. <see cref="Stop"/> cuts the part being spoken and cancels the one waiting. UI thread only;
/// never sleeps a thread.
/// </summary>
public sealed class SpeechSequencer
{
    private readonly ITimers timers;
    private IReadOnlyList<SpeechPart> parts = [];
    private int index;
    private double beat;
    private (string? Identifier, double Rate, double Volume) voice = (null, 0.5, 1);

    /// <summary>Bumped by every <see cref="Speak"/> and <see cref="Stop"/>, so a beat or a finish that arrives late is ignored.</summary>
    private int generation;

    /// <summary>What <see cref="OnVoicingChanged"/> last reported.</summary>
    private bool voicing;

    public SpeechSequencer(ISpeaking speaker, ITimers timers)
    {
        Speaker = speaker;
        this.timers = timers;
    }

    public ISpeaking Speaker { get; }

    /// <summary>From the first part until the last finished, beats included.</summary>
    public bool IsActive { get; private set; }

    /// <summary>A part starts: its index, from 0. Show it in the bubble.</summary>
    public Action<int>? OnPartStarted { get; set; }

    /// <summary>The last part finished, or the sequence was stopped.</summary>
    public Action? OnFinished { get; set; }

    /// <summary>
    /// Audio started (true, just before a part is spoken) or ended (false: the part finished, so a beat or the end
    /// follows, or the sequence was stopped). Moves the character's mouth only while words are heard.
    /// </summary>
    public Action<bool>? OnVoicingChanged { get; set; }

    /// <summary>Audio is playing right now.</summary>
    public bool IsVoicing => IsActive && Speaker.IsSpeaking;

    /// <summary>
    /// The playing clip's mouth level (0–3) right now, from its envelope; null without one (the system voice, or a
    /// manifest without an envelope), so the mouth keeps its own timer. Past the envelope's end the mouth rests (0).
    /// </summary>
    public int? MouthLevel
    {
        get
        {
            if (!IsVoicing || index >= parts.Count)
                return null;
            var part = parts[index];
            if (part.Mouth.Count == 0 || part.MouthFps <= 0 || Speaker.ClipTime is not { } time)
                return null;
            var frame = Math.Max(0, (int)(time * part.MouthFps));
            return frame < part.Mouth.Count ? part.Mouth[frame] : 0;
        }
    }

    /// <summary>
    /// Speaks <paramref name="parts"/> in order, waiting <paramref name="beat"/> seconds after each one before the next.
    /// Stops anything still running first. An empty list finishes at once.
    /// </summary>
    public void Speak(IReadOnlyList<SpeechPart> parts, double beat, string? voiceIdentifier, double rate, double volume)
    {
        Stop();
        if (parts.Count == 0)
        {
            OnFinished?.Invoke();
            return;
        }
        generation++;
        this.parts = parts;
        this.beat = beat;
        voice = (voiceIdentifier, rate, volume);
        index = 0;
        IsActive = true;
        Start(generation);
    }

    /// <summary>Cuts the part being spoken and cancels a pending one; reports <see cref="OnFinished"/> once if anything was running.</summary>
    public void Stop()
    {
        if (!IsActive)
            return;
        generation++;
        IsActive = false;
        parts = [];
        Speaker.OnFinished = null;
        Speaker.Stop();
        SetVoicing(false);
        OnFinished?.Invoke();
    }

    private void SetVoicing(bool on)
    {
        if (on == voicing)
            return;
        voicing = on;
        OnVoicingChanged?.Invoke(on);
    }

    private void Start(int current)
    {
        if (current != generation || !IsActive)
            return;
        Speaker.OnFinished = () => PartFinished(current);
        OnPartStarted?.Invoke(index);
        var part = parts[index];
        SetVoicing(true); // first: a voice may finish at once
        Speaker.Speak(part.Text, part.Clip, voice.Identifier, voice.Rate, voice.Volume);
    }

    private void PartFinished(int current)
    {
        if (current != generation || !IsActive)
            return;
        Speaker.OnFinished = null;
        SetVoicing(false);
        index++;
        if (index >= parts.Count)
        {
            IsActive = false;
            parts = [];
            OnFinished?.Invoke();
            return;
        }
        timers.After(beat, () => Start(current));
    }
}
