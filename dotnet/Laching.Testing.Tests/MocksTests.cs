using Laching.Core.Scheduling;

namespace Laching.Testing.Tests;

/// <summary>Each mock's contract: a clock moved by hand, settable idle and busy values, a counting random source.</summary>
public sealed class MocksTests
{
    [Fact]
    public void MockClockAdvancesByMinutesOrSeconds()
    {
        var clock = new MockClock(DateTimeOffset.UnixEpoch);
        clock.AdvanceMinutes(1.5);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddSeconds(90), clock.Now);
        clock.AdvanceSeconds(-60);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddSeconds(30), clock.Now);
    }

    [Fact]
    public void MockIdleAnswersItsValue()
    {
        var idle = new MockIdle();
        Assert.Equal(0, idle.SecondsSinceLastInput());
        idle.Idle = 42;
        Assert.Equal(42, idle.SecondsSinceLastInput());
    }

    [Fact]
    public void MockBusyAnswersItsReasons()
    {
        var busy = new MockBusy();
        Assert.Empty(busy.CurrentBusyReasons());
        busy.Reasons = [BusyReason.Call, BusyReason.FullScreen];
        Assert.Equal(new HashSet<BusyReason> { BusyReason.Call, BusyReason.FullScreen }, busy.CurrentBusyReasons());
    }

    [Fact]
    public void CountingRandomCountsAndReportsEveryRead()
    {
        var reported = 0;
        var random = new CountingRandom(() => reported++);
        Assert.Equal(0, random.Reads);
        Assert.Equal(0.5, random.NextUnit());
        Assert.Equal(0.5, random.NextUnit());
        Assert.Equal(2, random.Reads);
        Assert.Equal(2, reported);
    }

    [Fact]
    public void CountingRandomCountsWithoutAListener()
    {
        var random = new CountingRandom();
        random.NextUnit();
        Assert.Equal(1, random.Reads);
    }
}
