using Deskling.Windows.Companion.Speech;

namespace Deskling.Windows.Companion.Tests.Speech;

public sealed class ChimePlayerTests : IDisposable
{
    private readonly TestUiContext ui = new();
    private readonly SoundFolder sounds = new();

    public void Dispose() => sounds.Dispose();

    [Fact]
    public void ASoundThatIsntThereCompletesAtOnce()
    {
        var chime = new ChimePlayer(sounds.Path, ui);
        var completed = 0;
        chime.Play("missing", 1, () => completed++);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void ASoundCompletesOnceItEnds()
    {
        // Without an audio device (a CI runner) the player fails rather than plays; it completes either way.
        sounds.Silence("blip.wav", 0.2);
        var chime = new ChimePlayer(sounds.Path, ui);
        var completed = 0;
        chime.Play("blip", 0, () => completed++);
        Assert.Equal(0, completed);
        Assert.True(ui.PumpUntil(() => completed > 0));
        ui.PumpFor(0.3);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void SeveralSoundsPlayAtOnce()
    {
        sounds.Silence("blip.wav", 0.2);
        var chime = new ChimePlayer(sounds.Path, ui);
        var completed = 0;
        chime.Play("blip", 0, () => completed++);
        chime.Play("blip", 0, () => completed++);
        Assert.True(ui.PumpUntil(() => completed == 2));
    }

    [Fact]
    public void StopCutsEverySoundAndDropsTheirCompletions()
    {
        sounds.Silence("long.wav", 3);
        var chime = new ChimePlayer(sounds.Path, ui);
        var completed = 0;
        chime.Play("long", 0, () => completed++);
        chime.Play("long", 0, () => completed++);
        ui.PumpFor(0.2);
        chime.Stop();
        ui.PumpFor(0.5);
        Assert.Equal(0, completed);
    }
}
