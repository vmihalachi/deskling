using Laching.Companion.Shell.Speech;

namespace Laching.Companion.Shell.Tests.Speech;

/// <summary>
/// Ported from the Swift <c>VoiceLibraryTests</c>, over the same <c>Tests/LachingShellTests/Resources/Voice</c>:
/// Italian shipping with male (default) and female, English testing. The Swift cases about the AVFoundation speaker
/// and chime are the Windows package's.
/// </summary>
public sealed class VoiceLibraryTests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "Resources", "Voice");

    private static VoiceLibrary Library(string language = "it", bool allowTesting = false, bool fallback = false) =>
        new(Folder, language, allowTesting, fallback);

    [Fact]
    public void VoicesTheDefaultFirst()
    {
        var voices = Library();
        Assert.True(voices.IsVoiced);
        Assert.Equal([new VoiceId("male"), new VoiceId("female")], voices.AvailableVoices);
        Assert.Equal(new VoiceId("female"), voices.Voice("female"));
        Assert.Equal(new VoiceId("male"), voices.Voice("robot")); // a voice the language lacks falls back to its default
        Assert.Equal(new VoiceId("male"), voices.Voice(null));
        Assert.Equal(12.5, voices.MouthFps);
    }

    [Fact]
    public void OnlyAClipOfTheCurrentTextCounts()
    {
        var voices = Library();
        Assert.NotNull(voices.Clip("hello", "Ciao.", "male"));
        Assert.Null(voices.Clip("hello", "Ciao!", "male")); // the bubble and the audio must agree
        Assert.Null(voices.Clip("stale", "Nuovo testo.", "male")); // recorded from older text
        Assert.Null(voices.Clip("nofile", "Senza file.", "male")); // the file is missing
        Assert.Equal([0, 1, 2, 0], voices.Mouth("hello", "male"));
        Assert.Equal([0, 1, 1, 0], voices.Mouth("hello", "female"));
    }

    [Fact]
    public void AJokeHasAClipPerPart()
    {
        var voices = Library();
        (string Id, string Text)[] parts = [("joke.1", "Perché il pomodoro arrossisce?"), ("joke.2", "Ha visto l'insalata.")];
        Assert.Equal("joke.1.m4a", Path.GetFileName(voices.Clip("joke.1", parts[0].Text, "male")));
        Assert.Equal("joke.2.m4a", Path.GetFileName(voices.Clip("joke.2", parts[1].Text, "male")));
        Assert.Empty(voices.PendingClipIds(parts, "male"));
        Assert.Equal(new HashSet<string> { "joke.1", "joke.2" }, voices.PendingClipIds(parts, "female"));
        Assert.Equal([0, 2, 3, 1], voices.Mouth("joke.1", "male"));
    }

    [Fact]
    public void PendingClips()
    {
        (string Id, string Text)[] clips = [("hello", "Ciao."), ("stale", "Nuovo testo."), ("nofile", "Senza file."), ("never", "Mai registrata.")];
        Assert.Equal(new HashSet<string> { "stale", "nofile", "never" }, Library().PendingClipIds(clips, "male"));
        Assert.Empty(Library(fallback: true).PendingClipIds(clips, "male")); // the system voice says them
        Assert.Null(Library(fallback: true).Clip("stale", "Nuovo testo.", "male"));
    }

    [Fact]
    public void ATestingLanguageStaysOffInRelease()
    {
        Assert.False(Library("en").IsVoiced);
        Assert.True(Library("en", allowTesting: true).IsVoiced);
    }

    [Fact]
    public void ALanguageWithoutARecordedVoiceKeepsTheSystemVoice()
    {
        var voices = Library("de", allowTesting: true);
        Assert.False(voices.IsVoiced);
        Assert.Null(voices.Voice("male"));
        Assert.False(new VoiceLibrary(null, "it", allowTesting: true).IsVoiced);
        Assert.False(VoiceLibrary.None("it").IsVoiced);
    }

    [Fact]
    public void TheHashIsSha256Hex() =>
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", VoiceLibrary.Hash(""));

    [Fact]
    public void ClipRateKeepsTheVoiceNatural()
    {
        Assert.Equal(1, SpeechRates.ClipRate(0.5));
        Assert.Equal(0.85, SpeechRates.ClipRate(0), 4);
        Assert.Equal(1.2, SpeechRates.ClipRate(1), 4);
    }
}
