using Deskling.Windows.Companion.Speech;
using Windows.Media.SpeechSynthesis;

namespace Deskling.Windows.Companion.Tests.Speech;

public sealed class SpeakerTests : IDisposable
{
    private readonly TestUiContext ui = new();
    private readonly SoundFolder sounds = new();

    public void Dispose() => sounds.Dispose();

    [Fact]
    public void TheSpeechRateCentersOnWindowsNormalRate()
    {
        Assert.Equal(0.5, Speaker.SpeechRate(0), 6);
        Assert.Equal(0.75, Speaker.SpeechRate(0.25), 6);
        Assert.Equal(1, Speaker.SpeechRate(0.5), 6);
        Assert.Equal(1.5, Speaker.SpeechRate(0.75), 6);
        Assert.Equal(2, Speaker.SpeechRate(1), 6);
        Assert.Equal(0.5, Speaker.SpeechRate(-1), 6);
        Assert.Equal(2, Speaker.SpeechRate(3), 6);
    }

    [Fact]
    public void VoicesForARegionListThatRegionFirstThenTheDefaultThenByName()
    {
        (string Name, string Language, bool IsDefault)[] installed =
        [
            ("Zira", "en-US", false),
            ("Helena", "es-ES", false),
            ("Hazel", "en-GB", false),
            ("David", "en-US", false),
            ("Mark", "en-US", true),
            ("Maria", "pt-BR", false),
            ("Helia", "pt-PT", false),
        ];
        var ranked = Speaker.Rank(installed, "en-US", voice => voice.Language, voice => voice.Name, voice => voice.IsDefault);
        Assert.Equal(["Mark", "David", "Zira", "Hazel"], ranked.Select(voice => voice.Name));
        // A bare language takes every region, the default voice first.
        Assert.Equal(["Mark", "David", "Hazel", "Zira"], Speaker.Rank(installed, "en", v => v.Language, v => v.Name, v => v.IsDefault).Select(v => v.Name));
        Assert.Equal(["Maria", "Helia"], Speaker.Rank(installed, "pt-BR", v => v.Language, v => v.Name, v => v.IsDefault).Select(v => v.Name));
        Assert.Empty(Speaker.Rank(installed, "ro", v => v.Language, v => v.Name, v => v.IsDefault));
    }

    [Fact]
    public void AnUnknownVoiceFallsBackToTheLanguagesFirst()
    {
        Assert.Equal(Speaker.Voices("en").FirstOrDefault()?.Id, Speaker.Voice("no such voice", "en")?.Id);
        if (SpeechSynthesizer.AllVoices.FirstOrDefault() is { } installed)
            Assert.Equal(installed.Id, Speaker.Voice(installed.Id, "xx")?.Id);
    }

    [Fact]
    public void AClipThatIsntThereEndsTheLineAtOnce()
    {
        var speaker = new Speaker("en", ui);
        var finished = 0;
        speaker.OnFinished = () => finished++;
        speaker.Speak("Hello", Path.Combine(sounds.Path, "missing.m4a"), null, 0.5, 1);
        Assert.Equal(1, finished);
        Assert.False(speaker.IsSpeaking);
        Assert.Null(speaker.ClipTime);
    }

    [Fact]
    public void AClipThatCantBePlayedEndsTheLine()
    {
        var speaker = new Speaker("en", ui);
        var finished = 0;
        speaker.OnFinished = () => finished++;
        speaker.Speak("Hello", sounds.Garbage("broken.m4a"), null, 0.5, 1);
        Assert.True(speaker.IsSpeaking);
        Assert.True(ui.PumpUntil(() => finished > 0));
        Assert.False(speaker.IsSpeaking);
        ui.PumpFor(0.3);
        Assert.Equal(1, finished);
    }

    [Fact]
    public void AClipPlaysToItsEndOnce()
    {
        // Without an audio device (a CI runner) the player fails rather than plays; the line ends either way.
        var speaker = new Speaker("en", ui);
        var finished = 0;
        speaker.OnFinished = () => finished++;
        speaker.Speak("Hello", sounds.Silence("line.wav", 0.4), null, 0.5, 0);
        Assert.True(speaker.IsSpeaking);
        Assert.True(speaker.ClipTime >= 0);
        Assert.True(ui.PumpUntil(() => finished > 0));
        Assert.False(speaker.IsSpeaking);
        Assert.Null(speaker.ClipTime);
        ui.PumpFor(0.3);
        Assert.Equal(1, finished);
    }

    [Fact]
    public void StopCutsTheLineAndReportsItOnce()
    {
        var speaker = new Speaker("en", ui);
        var finished = 0;
        speaker.OnFinished = () => finished++;
        speaker.Speak("Hello", sounds.Silence("long.wav", 3), null, 0.5, 0);
        ui.PumpFor(0.2);
        speaker.Stop();
        Assert.Equal(1, finished);
        Assert.False(speaker.IsSpeaking);
        speaker.Stop();
        // The cut player's late events change nothing.
        ui.PumpFor(0.5);
        Assert.Equal(1, finished);
    }

    [Fact]
    public void ANewLineReportsTheOneItCutThenPlaysItsOwn()
    {
        var speaker = new Speaker("en", ui);
        var finished = 0;
        speaker.OnFinished = () => finished++;
        speaker.Speak("One", sounds.Silence("one.wav", 3), null, 0.5, 0);
        speaker.Speak("Two", sounds.Silence("two.wav", 0.3), null, 0.5, 0);
        Assert.Equal(1, finished);
        Assert.True(speaker.IsSpeaking);
        Assert.True(ui.PumpUntil(() => finished == 2));
        ui.PumpFor(0.3);
        Assert.Equal(2, finished);
    }

    [Fact]
    public void ASynthesizedLineEnds()
    {
        // Speaks with the system voice where there is one, at no volume; with none installed the line ends at once.
        var speaker = new Speaker("en", ui);
        var finished = 0;
        speaker.OnFinished = () => finished++;
        speaker.Speak("Hi.", null, null, 1, 0);
        Assert.True(speaker.IsSpeaking);
        Assert.Null(speaker.ClipTime);
        Assert.True(ui.PumpUntil(() => finished > 0, seconds: 30));
        Assert.False(speaker.IsSpeaking);
        ui.PumpFor(0.3);
        Assert.Equal(1, finished);
    }

    [Fact]
    public void StoppingWhileSynthesizingDropsTheResult()
    {
        var speaker = new Speaker("en", ui);
        var finished = 0;
        speaker.OnFinished = () => finished++;
        speaker.Speak("A longer line that takes the synthesizer a moment.", null, null, 0.5, 0);
        speaker.Stop();
        Assert.Equal(1, finished);
        ui.PumpFor(3);
        Assert.Equal(1, finished);
        Assert.False(speaker.IsSpeaking);
    }
}
