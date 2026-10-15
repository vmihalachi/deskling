namespace Deskling.Testing.Tests.Shell;

/// <summary><see cref="ManualTimers"/>: one-shots by hand or by time, repeating timers, disposal and the clock.</summary>
public sealed class ManualTimersTests
{
    [Fact]
    public void RunNextFiresTheOldestOneShotWhateverItsDelay()
    {
        var timers = new ManualTimers();
        var fired = new List<string>();
        timers.After(10, () => fired.Add("slow"));
        timers.After(1, () => fired.Add("fast"));
        Assert.Equal(new double[] { 10, 1 }, timers.Delays);
        timers.RunNext();
        Assert.Equal(["slow"], fired);
        Assert.Equal(new double[] { 1 }, timers.Delays);
        timers.RunNext();
        Assert.Equal(["slow", "fast"], fired);
        Assert.Throws<InvalidOperationException>(timers.RunNext);
    }

    [Fact]
    public void AdvanceFiresWhatComesDueInOrderAndMovesTheClock()
    {
        var clock = new MockClock(DateTimeOffset.UnixEpoch);
        var timers = new ManualTimers(clock);
        var fired = new List<string>();
        timers.After(5, () => fired.Add("b"));
        timers.After(2, () => fired.Add("a"));
        timers.After(9, () => fired.Add("c"));
        timers.Advance(5);
        Assert.Equal(["a", "b"], fired);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddSeconds(5), clock.Now);
        timers.Advance(4);
        Assert.Equal(["a", "b", "c"], fired);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddSeconds(9), clock.Now);
        Assert.Empty(timers.Delays);
    }

    [Fact]
    public void ARepeatingTimerFiresEveryIntervalUntilDisposed()
    {
        var timers = new ManualTimers();
        var ticks = 0;
        var handle = timers.Every(2, () => ticks++);
        Assert.Equal(1, timers.RepeatingCount);
        Assert.Empty(timers.Delays);
        timers.Advance(7);
        Assert.Equal(3, ticks);
        handle.Dispose();
        Assert.Equal(0, timers.RepeatingCount);
        timers.Advance(10);
        Assert.Equal(3, ticks);
    }

    [Fact]
    public void ADisposedOneShotNeverFires()
    {
        var timers = new ManualTimers();
        var fired = false;
        var handle = timers.After(1, () => fired = true);
        handle.Dispose();
        Assert.Empty(timers.Delays);
        timers.Advance(2);
        Assert.False(fired);
        Assert.Throws<InvalidOperationException>(timers.RunNext);
    }

    [Fact]
    public void WorkScheduledWhileAdvancingRunsWhenItIsDue()
    {
        var timers = new ManualTimers();
        var fired = new List<string>();
        timers.After(1, () =>
        {
            fired.Add("first");
            timers.After(1, () => fired.Add("chained"));
        });
        timers.Advance(1.5);
        Assert.Equal(["first"], fired);
        timers.Advance(1);
        Assert.Equal(["first", "chained"], fired);
    }
}
