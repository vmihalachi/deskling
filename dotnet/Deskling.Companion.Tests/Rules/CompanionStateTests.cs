using Deskling.Core.Scheduling;
using Deskling.Companion.Rules;

namespace Deskling.Companion.Tests;

public sealed class CompanionStateTests
{
    private static readonly SchedulerStatus Scheduled = new SchedulerStatus.Scheduled(DateTimeOffset.UnixEpoch);

    private static Snapshot BusyWith(params BusyReason[] reasons) => new() { Busy = reasons.ToHashSet() };

    [Fact]
    public void Priorities()
    {
        var paused = new SchedulerStatus.Paused(DateTimeOffset.UnixEpoch);
        Assert.Equal(Mood.Awake, CompanionState.MoodFor(Scheduled, Snapshot.Calm));
        Assert.Equal(Mood.Talking, CompanionState.MoodFor(Scheduled, new Snapshot { IsSpeaking = true }));
        Assert.Equal(Mood.Shushed, CompanionState.MoodFor(paused, Snapshot.Calm));
        Assert.Equal(Mood.Talking, CompanionState.MoodFor(paused, new Snapshot { IsSpeaking = true }));
        Assert.Equal(Mood.HushedForCall, CompanionState.MoodFor(Scheduled, BusyWith(BusyReason.Call, BusyReason.FullScreen)));
        Assert.Equal(Mood.HushedFullScreen, CompanionState.MoodFor(Scheduled, BusyWith(BusyReason.FullScreen)));
        Assert.Equal(Mood.Asleep, CompanionState.MoodFor(Scheduled, new Snapshot { IdleSeconds = 900 }));
        Assert.Equal(Mood.Asleep, CompanionState.MoodFor(Scheduled, new Snapshot { IsScreenLocked = true }));
        Assert.Equal(Mood.Asleep, CompanionState.MoodFor(new SchedulerStatus.Away(), Snapshot.Calm));
        Assert.Equal(Mood.HushedForCall, CompanionState.MoodFor(new SchedulerStatus.Postponed(BusyReason.Call), Snapshot.Calm));
        Assert.Equal(Mood.OffHours, CompanionState.MoodFor(new SchedulerStatus.OutsideActiveHours(), Snapshot.Calm));
    }

    [Fact]
    public void RawValuesRoundTrip()
    {
        foreach (var mood in Moods.All)
            Assert.Equal(mood, Moods.Parse(mood.RawValue()));
        Assert.Equal("hushedFullScreen", Mood.HushedFullScreen.RawValue());
        Assert.Null(Moods.Parse("sleepy"));
    }
}
