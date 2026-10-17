using Laching.Companion.Rules;
using Laching.Companion.Shell.Signals;
using Laching.Core.Scheduling;

namespace Laching.Companion.Shell.Tests.Signals;

/// <summary>The hub's busy watch and power events (the Swift hub is covered through the app model's tests; these pin it directly).</summary>
public sealed class SignalHubTests
{
    private readonly MockIdle idle = new() { Idle = 42 };
    private readonly MockBusy busy = new();
    private readonly ManualTimers timers = new();
    private readonly FakePower power = new() { State = new PowerState(IsOnBattery: false, Percent: 80) };
    private readonly FakeAppearance appearance = new();
    private readonly List<Trigger> events = [];

    private SignalHub Hub(IScreenLock? screenLock = null, IAudioOutputState? audio = null)
    {
        var hub = new SignalHub(idle, busy, audio, screenLock, power, appearance, timers);
        hub.OnEvent = events.Add;
        return hub;
    }

    [Fact]
    public void TheSnapshotReadsEverySource()
    {
        busy.Reasons = [BusyReason.FullScreen];
        var hub = Hub(new FakeScreenLock { IsLocked = true }, new FakeAudio { Muted = true });
        hub.IsSpeaking = () => true;
        hub.NotificationsAuthorized = true;
        var snapshot = hub.Snapshot();
        Assert.Equal(new HashSet<BusyReason> { BusyReason.FullScreen }, snapshot.Busy);
        Assert.Equal(42, snapshot.IdleSeconds);
        Assert.True(snapshot.IsScreenLocked);
        Assert.True(snapshot.IsOutputMuted);
        Assert.True(snapshot.IsSpeaking);
        Assert.True(snapshot.NotificationsAuthorized);
        Assert.Null(Hub().Snapshot().IsOutputMuted); // no source: unknown
    }

    [Fact]
    public void ACallMidLineStopsItOnce()
    {
        var hub = Hub();
        var calls = 0;
        hub.OnCallStarted = () => calls++;
        hub.SpeechStarted();
        Assert.True(hub.IsWatchingBusy);
        timers.Advance(1);
        Assert.Equal(0, calls);
        busy.Reasons = [BusyReason.Call];
        timers.Advance(1);
        Assert.Equal(1, calls);
        Assert.False(hub.IsWatchingBusy); // speech ended: the watch stops
        timers.Advance(5);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void TheDesktopWatchReportsChanges()
    {
        var hub = Hub();
        var polls = new List<(int Count, bool Changed)>();
        hub.OnBusyPoll = (reasons, changed) => polls.Add((reasons.Count, changed));
        hub.WatchForDesktop(true);
        timers.Advance(1);
        busy.Reasons = [BusyReason.FullScreen];
        timers.Advance(1);
        timers.Advance(1);
        Assert.Equal([(0, false), (1, true), (1, false)], polls);
        hub.WatchForDesktop(false);
        Assert.False(hub.IsWatchingBusy);
        Assert.Equal(0, timers.RepeatingCount);
    }

    [Fact]
    public void SpeechAndTheDesktopShareOneWatch()
    {
        var hub = Hub();
        hub.WatchForDesktop(true);
        hub.SpeechStarted();
        Assert.Equal(1, timers.RepeatingCount);
        hub.SpeechEnded();
        Assert.True(hub.IsWatchingBusy); // the desktop still watches
    }

    [Fact]
    public void PowerChangesAreEvents()
    {
        Hub();
        power.Set(new PowerState(IsOnBattery: true, Percent: 21));
        power.Set(new PowerState(IsOnBattery: true, Percent: 19));
        power.Set(new PowerState(IsOnBattery: true, Percent: 15));
        power.Set(new PowerState(IsOnBattery: false, Percent: 15));
        Assert.Equal([Trigger.Unplugged, Trigger.BatteryLow, Trigger.Charging], events);
    }

    [Fact]
    public void AppearanceChangesAreEvents()
    {
        Hub();
        appearance.Set(true);
        appearance.Set(false);
        Assert.Equal([Trigger.WentDark, Trigger.WentLight], events);
    }
}
