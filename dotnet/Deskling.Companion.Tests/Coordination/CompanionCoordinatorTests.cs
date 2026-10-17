using Deskling.Companion.Cadence;
using Deskling.Companion.Coordination;
using Deskling.Companion.Phrases;
using Deskling.Companion.Rules;
using Deskling.Companion.Settings;
using Deskling.Core.Scheduling;
using Deskling.Core.Settings;

namespace Deskling.Companion.Tests;

public sealed class CompanionCoordinatorTests
{
    private MockClock clock = null!;
    private MockIdle idle = null!;
    private MockBusy busy = null!;
    private CompanionSettings settings = null!;

    /// <summary>Monday 2026-09-28 at the given UTC time.</summary>
    private static DateTimeOffset Monday(int hour, int minute = 0) => new(2026, 9, 28, hour, minute, 0, TimeSpan.Zero);

    private CompanionCoordinator Make(DateTimeOffset start, ulong seed = 42, string language = "en", Action<CompanionSettings>? configure = null)
    {
        clock = new MockClock(start);
        idle = new MockIdle();
        busy = new MockBusy();
        settings = new CompanionSettings(new InMemorySettingsStore(), Fixture.Defaults);
        settings.Frequency = Frequency.Sometimes; // every 30 minutes: these tests count lines on that clock
        configure?.Invoke(settings);
        return new CompanionCoordinator(settings, Fixture.Bank, language, clock, idle, busy, TimeZoneInfo.Utc, seed);
    }

    /// <summary>Ticks once a minute with the given snapshot, returning the minutes at which a line was delivered.</summary>
    private List<(int Minute, Delivery Delivery)> Run(CompanionCoordinator c, int minutes, Snapshot? snapshot = null)
    {
        var s = snapshot ?? Snapshot.Calm;
        idle.Idle = s.IdleSeconds;
        busy.Reasons = s.Busy.ToHashSet();
        var output = new List<(int, Delivery)>();
        for (var m = 1; m <= minutes; m++)
        {
            clock.AdvanceMinutes(1);
            if (c.Tick(s) is { } delivery)
                output.Add((m, delivery));
        }
        return output;
    }

    [Fact]
    public void AmbientChatterArrivesAroundTheInterval()
    {
        var c = Make(Monday(10));
        var deliveries = Run(c, 100);
        Assert.Equal(3, deliveries.Count);
        foreach (var (minute, delivery) in deliveries)
        {
            Assert.Equal(Trigger.Due, delivery.Trigger);
            Assert.False(delivery.Phrase.HasEventTag);
            Assert.True(delivery.Plan.Speak);
            Assert.InRange(minute, 20, 120);
        }
        Assert.Equal(3, c.RecentIDs.Count);
        Assert.Equal(deliveries[^1].Delivery, c.LastDelivery);
    }

    [Fact]
    public void PokeSaysSomethingNowAndRestartsTheInterval()
    {
        var c = Make(Monday(10));
        var delivery = c.Poke(Snapshot.Calm);
        Assert.Equal(Trigger.ButtonPress, delivery?.Trigger);
        Assert.Contains(Tag.ButtonPress, delivery!.Phrase.Tags);
        Assert.False(delivery.Plan.Notify);
        Assert.Empty(Run(c, 19));
    }

    [Fact]
    public void MutedAndUnavailableLinesAndFlavorsTurnedOffNeverPlay()
    {
        var pokes = Fixture.Bank.Tagged(Tag.ButtonPress).Select(p => p.Id).ToArray();
        var c = Make(Monday(10), configure: s => s.MutedPhraseIDs = pokes.Skip(2).ToHashSet());
        c.UnavailableIDs = new HashSet<string> { pokes[0] };
        for (var i = 0; i < 4; i++)
            Assert.Equal(pokes[1], c.Poke(Snapshot.Calm)?.Phrase.Id);
        settings.MutedPhraseIDs = new HashSet<string>();
        c.UnavailableIDs = new HashSet<string>();
        settings.PhraseFlavors = new HashSet<FlavorId> { FixtureIds.FlavorC };
        var ambient = Run(c, 300).Select(d => d.Delivery.Phrase).ToArray();
        Assert.NotEmpty(ambient);
        foreach (var phrase in ambient)
            Assert.True(phrase.Flavor == FixtureIds.FlavorC || phrase.IsTune, phrase.Id);
    }

    [Fact]
    public void ShushForAnHourThenOneLineOnWaking()
    {
        var c = Make(Monday(10));
        c.Shush(ChatterCadence.Shush.Hour);
        Assert.True(c.IsShushed);
        Assert.Equal(Monday(11), settings.ShushedUntil);
        Assert.Equal(Mood.Shushed, c.Mood(Snapshot.Calm));
        Assert.Empty(Run(c, 59));
        Assert.Equal([Trigger.ShushEnd], Run(c, 2).Select(d => d.Delivery.Trigger));
        Assert.False(c.IsShushed);
        Assert.Null(settings.ShushedUntil);
    }

    [Fact]
    public void WakeEndsShushEarly()
    {
        var c = Make(Monday(10));
        c.Shush(ChatterCadence.Shush.UntilISay);
        Assert.Equal(CompanionCoordinator.DistantFuture, settings.ShushedUntil);
        Assert.Empty(Run(c, 300));
        Assert.Equal(Trigger.ShushEnd, c.Wake(Snapshot.Calm)?.Trigger);
        Assert.False(c.IsShushed);
        Assert.Null(c.Wake(Snapshot.Calm));
    }

    [Fact]
    public void ShushRestOfTodayIsRestoredFromSettings()
    {
        var c = Make(Monday(10));
        c.Shush(ChatterCadence.Shush.RestOfToday);
        Assert.Equal(new DateTimeOffset(2026, 9, 29, 0, 0, 0, TimeSpan.Zero), settings.ShushedUntil);
        var again = new CompanionCoordinator(settings, Fixture.Bank, "en", clock, idle, busy, TimeZoneInfo.Utc, 1);
        Assert.True(again.IsShushed);
    }

    [Fact]
    public void FullScreenHoldsTheLineAndReleasesItAsAPlainLine()
    {
        var c = Make(Monday(10));
        Run(c, 15);
        var fullScreen = new Snapshot { Busy = new HashSet<BusyReason> { BusyReason.FullScreen } };
        Assert.Empty(Run(c, 40, fullScreen));
        Assert.Equal(new SchedulerStatus.Postponed(BusyReason.FullScreen), c.Scheduler.Status);
        var released = Run(c, 3);
        Assert.Equal([Trigger.Due], released.Select(d => d.Delivery.Trigger));
        Assert.False(released[0].Delivery.Phrase.HasEventTag);
    }

    [Fact]
    public void ACallHoldsTheLineAndReleasesItAsAfterCall()
    {
        var c = Make(Monday(10));
        Run(c, 15);
        var call = new Snapshot { Busy = new HashSet<BusyReason> { BusyReason.Call } };
        Assert.Empty(Run(c, 40, call));
        Assert.Equal(Mood.HushedForCall, c.Mood(call));
        var released = Run(c, 3);
        Assert.Equal([Trigger.AfterCall], released.Select(d => d.Delivery.Trigger));
        Assert.Equal([Tag.AfterCall], released[0].Delivery.Phrase.Tags);
    }

    [Fact]
    public void WelcomeBackAfterBeingAwayWithACooldown()
    {
        var c = Make(Monday(10));
        var away = new Snapshot { IdleSeconds = 600 };
        Run(c, 1);
        Assert.Empty(Run(c, 10, away));
        Assert.Equal([Trigger.WelcomeBack], Run(c, 1).Select(d => d.Delivery.Trigger));
        Assert.Equal(clock.Now, c.LastWelcomeAt);
        Run(c, 6, away);
        Assert.Empty(Run(c, 1)); // within the cooldown
    }

    [Fact]
    public void EventsRespectTheSettingsAndShh()
    {
        var c = Make(Monday(10), configure: s => s.CommentOnPower = false);
        Assert.Null(c.Event(Trigger.BatteryLow, Snapshot.Calm));
        Assert.Equal(Trigger.WentDark, c.Event(Trigger.WentDark, Snapshot.Calm)?.Trigger);
        c.Shush(ChatterCadence.Shush.Hour);
        Assert.Null(c.Event(Trigger.WentLight, Snapshot.Calm));
    }

    [Fact]
    public void NothingOutsideActiveHours()
    {
        var c = Make(Monday(20));
        Assert.Empty(Run(c, 120));
        Assert.Null(c.Event(Trigger.WentDark, Snapshot.Calm));
        Assert.Equal(Mood.OffHours, c.Mood(Snapshot.Calm));
        Assert.Equal(Trigger.ButtonPress, c.Poke(Snapshot.Calm)?.Trigger); // the button works at any hour
    }

    [Fact]
    public void SpeakingSkipsAndMutedFallsBack()
    {
        var c = Make(Monday(10), configure: s => s.Channels = Channels.Voice);
        Assert.Null(c.Event(Trigger.WentLight, new Snapshot { IsSpeaking = true }));
        // The button answers mid-line (a notification's Say more); the shell cuts the line being said.
        var pressed = c.Poke(new Snapshot { IsSpeaking = true });
        Assert.Equal(Trigger.ButtonPress, pressed?.Trigger);
        Assert.Equal(new ChannelPlan(true, false, false), pressed?.Plan);
        var muted = c.Event(Trigger.WentDark, new Snapshot { IsOutputMuted = true, NotificationsAuthorized = true });
        Assert.Equal(new ChannelPlan(false, false, true), muted?.Plan);
    }

    [Fact]
    public void SayMoreWhileTheNotifiedLineIsSpokenIsAnswered()
    {
        // conformance/companion/quiet/say-more-while-speaking.json's story.
        var c = Make(Monday(10), seed: 3, configure: s => s.Channels = Channels.Voice | Channels.Notification);
        var notified = Assert.Single(Run(c, 35)).Delivery;
        Assert.Equal(new ChannelPlan(true, false, true), notified.Plan);
        var speaking = new Snapshot { IsSpeaking = true };
        var pressed = c.Poke(speaking);
        Assert.Equal(Trigger.ButtonPress, pressed?.Trigger);
        Assert.Equal(new ChannelPlan(true, false, false), pressed?.Plan);
        Assert.Null(c.Event(Trigger.WentDark, speaking)); // anything else still waits for the speech to end
        Assert.Empty(Run(c, 5));
    }

    [Fact]
    public void SettingsChangedReschedules()
    {
        var c = Make(Monday(10));
        settings.Frequency = Frequency.Every2Hours;
        c.SettingsChanged();
        Assert.Equal(7200, c.Scheduler.Config.IntervalSeconds);
        Assert.Empty(Run(c, 89));
    }

    [Fact]
    public void FullScreenCountsOnlyWhenTheSettingSays()
    {
        var fullScreen = new Snapshot { Busy = new HashSet<BusyReason> { BusyReason.FullScreen } };
        var hushed = Make(Monday(10));
        Assert.Empty(Run(hushed, 60, fullScreen));
        Assert.Equal(Mood.HushedFullScreen, hushed.Mood(fullScreen));
        var chatty = Make(Monday(10), configure: s => s.HushInFullScreen = false);
        Assert.NotEmpty(Run(chatty, 60, fullScreen));
        Assert.Equal(Mood.Awake, chatty.Mood(fullScreen));
        Assert.Equal(Mood.HushedForCall, chatty.Mood(new Snapshot { Busy = new HashSet<BusyReason> { BusyReason.Call, BusyReason.FullScreen } }));
    }

    [Fact]
    public void SameSeedSameDay() => Assert.Equal(Make(Monday(10)).Poke(Snapshot.Calm), Make(Monday(10)).Poke(Snapshot.Calm));

    [Fact]
    public void TheLanguageScopesThePicks()
    {
        string[] italianOnly = ["it-only", "joke-it"];
        var english = Make(Monday(10), configure: s => s.Channels = Channels.Quiet);
        var said = Run(english, 600).Select(d => d.Delivery.Phrase.Id).ToHashSet();
        Assert.NotEmpty(said);
        Assert.Empty(said.Intersect(italianOnly));

        var italian = Make(Monday(10), language: "it", configure: s => s.Channels = Channels.Quiet);
        Assert.NotEmpty(Run(italian, 600).Select(d => d.Delivery.Phrase.Id).Intersect(italianOnly));
        italian.Language = "en";
        for (var i = 0; i < 20; i++)
            Assert.NotEqual("joke-it", italian.Poke(Snapshot.Calm)?.Phrase.Id);
    }

    [Fact]
    public void TheSkinScopesThePicks()
    {
        var c = Make(Monday(10), configure: s => s.Channels = Channels.Quiet);
        Assert.DoesNotContain(Run(c, 600), d => d.Delivery.Phrase.Id == "skin-raster-only");
        var raster = Make(
            Monday(10),
            configure: s =>
            {
                s.Channels = Channels.Quiet;
                s.Skin = FixtureIds.RasterSkin;
            }
        );
        Assert.Contains(Run(raster, 600), d => d.Delivery.Phrase.Id == "skin-raster-only");
    }
}
