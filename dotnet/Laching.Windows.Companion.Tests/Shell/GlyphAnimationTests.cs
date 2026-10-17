using Laching.Companion.Rules;
using Laching.Testing;
using Laching.Windows.Companion.Shell;

namespace Laching.Windows.Companion.Tests.Shell;

public sealed class GlyphAnimationTests
{
    private readonly ManualTimers timers = new();
    private readonly List<(Mood Mood, int Frame)> drawn = [];
    private bool reduceMotion;

    private GlyphAnimation Make() => new(timers, () => reduceMotion, (mood, frame) => drawn.Add((mood, frame)));

    private (Mood Mood, int Frame) Last => drawn[^1];

    [Fact]
    public void AtRestItShowsTheMoodsFirstFrame()
    {
        var animation = Make();

        animation.Show(Mood.Shushed, talking: false);

        Assert.Equal((Mood.Shushed, 0), Last);
        Assert.Equal(0, timers.RepeatingCount);
    }

    [Fact]
    public void TalkingStepsTheTalkingFramesEightTimesASecond()
    {
        var animation = Make();

        animation.Show(Mood.Awake, talking: true);
        Assert.Equal((Mood.Talking, 1), Last);
        timers.Advance(GlyphAnimation.FrameInterval);
        Assert.Equal((Mood.Talking, 2), Last);
        timers.Advance(1);
        Assert.Equal((Mood.Talking, 10), Last);
    }

    [Fact]
    public void WhenTheLineEndsItRestsAndStopsStepping()
    {
        var animation = Make();
        animation.Show(Mood.Awake, talking: true);
        timers.Advance(0.5);

        animation.Show(Mood.Awake, talking: false);

        Assert.Equal((Mood.Awake, 0), Last);
        Assert.Equal(0, timers.RepeatingCount);
    }

    [Fact]
    public void AWaveRunsTheTalkingFramesForAMoment()
    {
        var animation = Make();
        animation.Show(Mood.Awake, talking: false);

        animation.Wave();
        Assert.Equal((Mood.Talking, 1), Last);
        timers.Advance(GlyphAnimation.WaveSeconds - 0.01);
        Assert.Equal(Mood.Talking, Last.Mood);
        timers.Advance(0.02);

        Assert.Equal((Mood.Awake, 0), Last);
        Assert.Equal(0, timers.RepeatingCount);
    }

    [Fact]
    public void ALineThatEndsDuringAWaveKeepsTheFramesUntilTheWaveEnds()
    {
        var animation = Make();
        animation.Show(Mood.Awake, talking: true);
        animation.Wave();

        animation.Show(Mood.Awake, talking: false);
        Assert.Equal(Mood.Talking, Last.Mood);
        timers.Advance(GlyphAnimation.WaveSeconds);

        Assert.Equal((Mood.Awake, 0), Last);
    }

    [Fact]
    public void ReducedMotionHoldsTheFirstTalkingFrame()
    {
        reduceMotion = true;
        var animation = Make();

        animation.Show(Mood.Awake, talking: true);
        timers.Advance(1);

        Assert.Equal((Mood.Talking, 1), Last);
        Assert.Equal(0, timers.RepeatingCount);
    }

    [Fact]
    public void ANewMoodRedrawsAtOnce()
    {
        var animation = Make();
        animation.Show(Mood.Awake, talking: false);

        animation.Show(Mood.Asleep, talking: false);

        Assert.Equal((Mood.Asleep, 0), Last);
    }
}
