using Laching.Companion.Shell.Speech;

namespace Laching.Companion.Shell.Tests.Speech;

/// <summary>Ported from the Swift <c>SpeechSequencerTests</c>, case for case.</summary>
public sealed class SpeechSequencerTests
{
    private readonly FakeSpeaker speaker = new();
    private readonly ManualTimers timers = new();
    private readonly SpeechSequencer sequencer;
    private readonly List<string> events = [];

    private static readonly SpeechPart[] Joke = [new("Setup"), new("Punchline")];

    public SpeechSequencerTests()
    {
        sequencer = new SpeechSequencer(speaker, timers)
        {
            OnPartStarted = index => events.Add($"part {index}"),
            OnFinished = () => events.Add("finished"),
        };
    }

    private void Speak(IReadOnlyList<SpeechPart> parts) => sequencer.Speak(parts, 1.2, null, 0.5, 1);

    [Fact]
    public void PartsPlayInOrderWithTheBeatBetween()
    {
        Speak(Joke);
        Assert.Equal(["Setup"], speaker.Spoken);
        Assert.Equal(["part 0"], events);
        speaker.Finish();
        Assert.Equal([1.2], timers.Delays);
        Assert.Equal(["Setup"], speaker.Spoken); // the punchline waits for the beat
        timers.RunNext();
        Assert.Equal(["Setup", "Punchline"], speaker.Spoken);
        speaker.Finish();
        Assert.Equal(["part 0", "part 1", "finished"], events);
        Assert.False(sequencer.IsActive);
        Assert.Empty(timers.Delays);
    }

    [Fact]
    public void ActiveThroughTheBeatButNotVoicing()
    {
        Speak(Joke);
        Assert.True(sequencer.IsActive);
        Assert.True(sequencer.IsVoicing);
        speaker.Finish();
        Assert.True(sequencer.IsActive); // nothing else may start in the pause
        Assert.False(sequencer.IsVoicing); // the mouth closes during the beat
        timers.RunNext();
        Assert.True(sequencer.IsVoicing);
    }

    [Fact]
    public void VoicingIsReportedAsAudioStartsAndStops()
    {
        var voicing = new List<bool>();
        sequencer.OnVoicingChanged = voicing.Add;
        Speak(Joke);
        Assert.Equal([true], voicing);
        speaker.Finish();
        Assert.Equal([true, false], voicing); // the beat is silent
        timers.RunNext();
        speaker.Finish();
        Assert.Equal([true, false, true, false], voicing);
        Speak(Joke);
        sequencer.Stop();
        Assert.Equal([true, false, true, false, true, false], voicing); // a stop ends the audio
        sequencer.Stop();
        Assert.Equal(6, voicing.Count); // reported once
    }

    [Fact]
    public void MouthLevelFollowsTheClipsEnvelope()
    {
        SpeechPart[] parts = [new("Setup", mouth: [0, 2, 3, 1], mouthFps: 10), new("Punchline")];
        Assert.Null(sequencer.MouthLevel); // nothing playing
        Speak(parts);
        Assert.Null(sequencer.MouthLevel); // no clip time: the mouth keeps its timer
        speaker.ClipTime = 0;
        Assert.Equal(0, sequencer.MouthLevel);
        speaker.ClipTime = 0.15;
        Assert.Equal(2, sequencer.MouthLevel);
        speaker.ClipTime = 0.29;
        Assert.Equal(3, sequencer.MouthLevel);
        speaker.ClipTime = 0.5;
        Assert.Equal(0, sequencer.MouthLevel); // past the envelope the mouth rests
        speaker.Finish();
        Assert.Null(sequencer.MouthLevel); // the beat is silent
        timers.RunNext();
        Assert.Null(sequencer.MouthLevel); // the punchline has no envelope
    }

    [Fact]
    public void StopCancelsThePendingPart()
    {
        Speak(Joke);
        speaker.Finish();
        sequencer.Stop();
        Assert.False(sequencer.IsActive);
        Assert.Equal(["part 0", "finished"], events);
        timers.RunNext();
        Assert.Equal(["Setup"], speaker.Spoken); // a cancelled beat says nothing
        Assert.Equal(["part 0", "finished"], events);
    }

    [Fact]
    public void StopCutsThePartBeingSpoken()
    {
        Speak(Joke);
        sequencer.Stop();
        Assert.False(speaker.IsSpeaking);
        Assert.Equal(1, speaker.Stops);
        Assert.Equal(["part 0", "finished"], events); // finished once, not again from the voice
        Assert.Empty(timers.Delays);
    }

    [Fact]
    public void ALineIsOnePartWithoutABeat()
    {
        Speak([new SpeechPart("Hello")]);
        speaker.Finish();
        Assert.Equal(["part 0", "finished"], events);
        Assert.Empty(timers.Delays);
    }

    [Fact]
    public void ANewLineReplacesTheOldOne()
    {
        Speak(Joke);
        speaker.Finish();
        Speak([new SpeechPart("Hello")]);
        timers.RunNext(); // the old joke's beat
        Assert.Equal(["Setup", "Hello"], speaker.Spoken);
        Assert.Equal(["part 0", "finished", "part 0"], events);
    }
}
