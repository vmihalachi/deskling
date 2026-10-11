using Deskling.Core.Scheduling;
using Deskling.Companion.Desktop;
using Deskling.Companion.Rules;
using Deskling.Companion.Settings;

namespace Deskling.Companion.Tests;

public sealed class DesktopPresenceTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
    private static readonly Snapshot Call = new() { Busy = new HashSet<BusyReason> { BusyReason.Call } };
    private static readonly Snapshot FullScreen = new() { Busy = new HashSet<BusyReason> { BusyReason.FullScreen } };

    private static DateTimeOffset At(double seconds) => Start.AddSeconds(seconds);

    [Fact]
    public void ShownWhenEnabledAndCalm()
    {
        var presence = new DesktopPresence();
        Assert.True(presence.Update(true, Snapshot.Calm, At(0)));
        Assert.False(presence.Update(false, Snapshot.Calm, At(1)));
        Assert.True(presence.Update(true, Snapshot.Calm, At(2)));
    }

    [Fact]
    public void ACallHidesTheCharacterUntilTenSecondsAfterItEnds()
    {
        var presence = new DesktopPresence();
        Assert.True(presence.Update(true, Snapshot.Calm, At(0)));
        Assert.False(presence.Update(true, Call, At(20)));
        Assert.False(presence.Update(true, Call, At(25)));
        Assert.False(presence.Update(true, Snapshot.Calm, At(26)));
        Assert.False(presence.Update(true, Snapshot.Calm, At(34.9)));
        Assert.True(presence.Update(true, Snapshot.Calm, At(35)));
    }

    [Fact]
    public void FullScreenHidesTheCharacterToo()
    {
        var presence = new DesktopPresence();
        Assert.False(presence.Update(true, FullScreen, At(0)));
        Assert.True(presence.Update(true, Snapshot.Calm, At(10)));
    }

    [Fact]
    public void ABusyMomentWhileDisabledStillCounts()
    {
        var presence = new DesktopPresence();
        Assert.False(presence.Update(false, Call, At(0)));
        Assert.False(presence.Update(true, Snapshot.Calm, At(5)));
        Assert.True(presence.Update(true, Snapshot.Calm, At(10)));
    }

    [Fact]
    public void HideForAWhileRunsOutByItself()
    {
        var presence = new DesktopPresence();
        presence.Hide(At(3600));
        Assert.False(presence.Update(true, Snapshot.Calm, At(0)));
        Assert.False(presence.Update(true, Snapshot.Calm, At(3599)));
        Assert.True(presence.Update(true, Snapshot.Calm, At(3600)));
        Assert.Null(presence.HiddenUntil);
    }

    [Fact]
    public void ShowNowEndsAHideEarly()
    {
        var presence = new DesktopPresence();
        presence.Hide(At(3600));
        Assert.False(presence.Update(true, Snapshot.Calm, At(0)));
        presence.ShowNow();
        Assert.True(presence.Update(true, Snapshot.Calm, At(60)));
    }

    [Fact]
    public void ACallAtTheEndOfAHideStillHoldsTheCharacterBack()
    {
        var presence = new DesktopPresence();
        presence.Hide(At(60));
        Assert.False(presence.Update(true, Call, At(55)));
        Assert.False(presence.Update(true, Snapshot.Calm, At(60)));
        Assert.True(presence.Update(true, Snapshot.Calm, At(65)));
    }

    [Fact]
    public void BubbleSecondsGrowWithTheLineBetweenTwoAndFive()
    {
        Assert.Equal(2, DesktopPresence.BubbleSeconds(""));
        Assert.Equal(2.5, DesktopPresence.BubbleSeconds(new string('a', 50)), 9);
        Assert.Equal(5, DesktopPresence.BubbleSeconds(new string('a', 500)));
        // Scalars, not UTF-16 units: the same count as the Mac.
        Assert.Equal(4.5, DesktopPresence.BubbleSeconds(string.Concat(Enumerable.Repeat("é", 100))), 9);
        Assert.Equal(4.5, DesktopPresence.BubbleSeconds(string.Concat(Enumerable.Repeat("\U0001F33B", 100))), 9);
    }

    [Fact]
    public void SizesStepAndStopAtTheEnds()
    {
        Assert.Equal(DesktopSize.Large, DesktopSize.Medium.Stepped(1));
        Assert.Equal(DesktopSize.Large, DesktopSize.Large.Stepped(1));
        Assert.Equal(DesktopSize.Small, DesktopSize.Medium.Stepped(-1));
        Assert.Equal(DesktopSize.Small, DesktopSize.Small.Stepped(-1));
        Assert.Equal(new double[] { 64, 96, 140 }, DesktopSizes.All.Select(s => s.Height()).ToArray());
    }
}
