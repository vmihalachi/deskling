using Deskling.Companion.Coordination;
using Deskling.Companion.Desktop;
using Deskling.Companion.Phrases;
using Deskling.Companion.Rules;
using Deskling.Companion.Settings;
using Deskling.Companion.Shell.Performance;
using Deskling.Companion.Shell.Speech;
using Deskling.Core.Settings;

namespace Deskling.Companion.Shell.Tests.Performance;

/// <summary>Ported from the Swift <c>DeliveryPerformerTests</c>, case for case.</summary>
public sealed class DeliveryPerformerTests : IDisposable
{
    private readonly CompanionSettings settings = new(new InMemorySettingsStore(), Fixture.Defaults);
    private readonly ManualTimers timers = new();
    private readonly List<string> bubbles = [];
    private readonly List<string> posts = [];
    private readonly List<string> events = [];
    private string? folder;

    private const string Setup = "Fixture joke, setup [phrase.joke-1.1]";
    private const string Punchline = "Fixture joke, punchline [phrase.joke-1.2]";

    public void Dispose()
    {
        if (folder is not null)
            Directory.Delete(folder, recursive: true);
    }

    private DeliveryPerformer Performer(ISpeaking speaker, IChimePlaying? chime = null, VoiceLibrary? voices = null) =>
        new(Fixture.Bank, speaker, chime ?? new SilentChime(), voices ?? new VoiceLibrary(null, "en", allowTesting: true), posts.Add, timers)
        {
            Localize = (key, source) => $"{source} [{key}]",
            TuneBubbleText = () => "♪ hm ♪",
            OnBubble = bubbles.Add,
            OnSpeechStarted = () => events.Add("started"),
            OnSpeechFinished = () => events.Add("finished"),
        };

    private static Delivery Delivery(string id, bool speak = true, bool chime = false, bool notify = false) =>
        new(Fixture.Bank[id]!, Trigger.ButtonPress, new ChannelPlan(speak, chime, notify));

    [Fact]
    public void AJokesPartsBubbleInOrderWithTheBeatBetween()
    {
        var speaker = new FakeSpeaker();
        var performer = Performer(speaker);
        performer.Perform(Delivery("joke-1", notify: true), settings, null);
        Assert.Equal([Setup], bubbles);
        Assert.Equal([Setup], speaker.Spoken);
        Assert.Equal(["started"], events);
        speaker.Finish();
        Assert.Equal([PhraseTiming.PartBeatSeconds], timers.Delays);
        Assert.True(performer.IsSpeaking); // the beat keeps other lines out
        Assert.False(performer.IsVoicing); // the mouth is closed during the beat
        Assert.Empty(posts); // the banner waits for the punchline
        timers.RunNext();
        Assert.Equal([Setup, Punchline], bubbles);
        Assert.Equal([Setup, Punchline], speaker.Spoken);
        Assert.Equal([$"{Setup}\n{Punchline}"], posts); // one banner, both parts on two lines
        speaker.Finish();
        Assert.False(performer.IsSpeaking);
        Assert.Equal(["started", "finished"], events);
    }

    [Fact]
    public void WithTheSilentSpeakerTheBeatStillRuns()
    {
        var performer = Performer(new SilentSpeaker());
        performer.Perform(Delivery("joke-1"), settings, null);
        Assert.True(performer.IsSpeaking);
        timers.RunNext();
        Assert.False(performer.IsSpeaking);
        Assert.Equal([Setup, Punchline], bubbles);
    }

    [Fact]
    public void StopMidBeatCancelsThePunchline()
    {
        var speaker = new FakeSpeaker();
        var performer = Performer(speaker);
        performer.Perform(Delivery("joke-1"), settings, null);
        speaker.Finish();
        performer.Stop();
        Assert.False(performer.IsSpeaking);
        timers.RunNext();
        Assert.Equal([Setup], speaker.Spoken);
        Assert.Equal([Setup], bubbles);
        Assert.Equal(["started", "finished"], events);
    }

    [Fact]
    public void WithoutSpeechThePartsFollowTheirBubbleTime()
    {
        var performer = Performer(new FakeSpeaker());
        performer.Perform(Delivery("joke-1", speak: false, notify: true), settings, null);
        Assert.Equal([Setup], bubbles);
        Assert.Equal([DesktopPresence.BubbleSeconds(Setup)], timers.Delays);
        Assert.False(performer.IsSpeaking);
        timers.RunNext();
        Assert.Equal([Setup, Punchline], bubbles);
        Assert.Equal([$"{Setup}\n{Punchline}"], posts);
        Assert.Empty(events);
    }

    [Fact]
    public void ALineNotifiesAtOnceAndTheBlipComesFirst()
    {
        var speaker = new FakeSpeaker();
        var chime = new FakeChime();
        var performer = Performer(speaker, chime);
        performer.BlipSound = "blip";
        performer.Perform(Delivery("general-a1", chime: true, notify: true), settings, null);
        const string line = "Fixture line, general, flavor a, weight 1 [phrase.general-a1]";
        Assert.Equal(["blip"], chime.Played);
        Assert.Equal([line], speaker.Spoken);
        Assert.Equal([line], posts);
        Assert.Empty(timers.Delays);
    }

    [Fact]
    public void ATunePlaysOnTheChimeWithItsBubble()
    {
        var speaker = new FakeSpeaker();
        var chime = new FakeChime();
        var performer = Performer(speaker, chime);
        performer.Perform(Delivery("tune-1", chime: true), settings, null);
        Assert.Equal(["tune-1"], chime.Played);
        Assert.Equal(["♪ hm ♪"], bubbles);
        Assert.Empty(speaker.Spoken);
    }

    // Recorded voices

    /// <summary>A voice folder for "en" where <c>male</c> recorded the given clips (texts as the performer localizes them).</summary>
    private VoiceLibrary Voices(Dictionary<string, string> clips, bool fallback = false)
    {
        folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var entries = new List<string>();
        foreach (var (id, text) in clips)
        {
            entries.Add($$"""  "{{id}}": { "textHash": "{{VoiceLibrary.Hash(text)}}", "duration": 1.0, "mouth": "0120" }""");
            var file = Path.Combine(folder, "en", "male", id + ".m4a");
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllBytes(file, []);
        }
        var manifest = $$"""
            { "format": 1, "mouthFPS": 12.5, "languages": { "en": { "status": "shipping", "default": "male",
              "voices": { "male": { "clips": { {{string.Join(",\n", entries)}} } } } } } }
            """;
        File.WriteAllText(Path.Combine(folder, "manifest.json"), manifest);
        return new VoiceLibrary(folder, "en", allowTesting: false, fallback);
    }

    [Fact]
    public void EachPartPlaysItsOwnClip()
    {
        var speaker = new FakeSpeaker();
        var performer = Performer(speaker, voices: Voices(new() { ["joke-1.1"] = Setup, ["joke-1.2"] = Punchline }));
        performer.Perform(Delivery("joke-1"), settings, "male");
        speaker.Finish();
        timers.RunNext();
        Assert.Equal(["joke-1.1.m4a", "joke-1.2.m4a"], speaker.Clips.Select(Path.GetFileName));
        Assert.DoesNotContain("joke-1", performer.UnavailableIds("male"));
    }

    [Fact]
    public void AJokeMissingAPartStaysUnsaid()
    {
        var speaker = new FakeSpeaker();
        var performer = Performer(speaker, voices: Voices(new() { ["joke-1.1"] = Setup }));
        Assert.Contains("joke-1", performer.UnavailableIds("male"));
        Assert.DoesNotContain("tune-1", performer.UnavailableIds("male")); // tunes are sounds
        Assert.DoesNotContain("it-only", performer.UnavailableIds("male")); // not in this language
        Assert.Empty(performer.UnavailableIds(null));
        performer.Perform(Delivery("joke-1"), settings, "male");
        Assert.Empty(speaker.Spoken);
        Assert.Equal([Setup], bubbles); // shown, not said
    }

    [Fact]
    public void TheFallbackSaysAMissingPartWithTheSystemVoice()
    {
        var speaker = new FakeSpeaker();
        var performer = Performer(speaker, voices: Voices(new() { ["joke-1.1"] = Setup }, fallback: true));
        Assert.Empty(performer.UnavailableIds("male"));
        performer.Perform(Delivery("joke-1"), settings, "male");
        speaker.Finish();
        timers.RunNext();
        Assert.Equal(["joke-1.1.m4a", null], speaker.Clips.Select(Path.GetFileName));
    }

    [Fact]
    public void TheMouthFollowsTheRecordedPartsEnvelope()
    {
        var speaker = new FakeSpeaker();
        var performer = Performer(speaker, voices: Voices(new() { ["joke-1.1"] = Setup }, fallback: true));
        performer.Perform(Delivery("joke-1"), settings, "male");
        speaker.ClipTime = 0.1; // the manifest's "0120" at 12.5 a second: frame 1
        Assert.Equal(1, performer.MouthLevel);
        speaker.ClipTime = 0.17;
        Assert.Equal(2, performer.MouthLevel);
        speaker.Finish();
        timers.RunNext();
        Assert.Null(performer.MouthLevel); // the system voice says the punchline: the mouth keeps its timer
    }

    [Fact]
    public void PreviewSpeaksWithoutChimeOrBanner()
    {
        var speaker = new FakeSpeaker();
        var chime = new FakeChime();
        var performer = Performer(speaker, chime);
        performer.BlipSound = "blip";
        performer.Preview(Fixture.Bank["general-b1"]!, settings, null);
        Assert.Single(speaker.Spoken);
        Assert.Empty(chime.Played);
        Assert.Empty(posts);
    }
}
