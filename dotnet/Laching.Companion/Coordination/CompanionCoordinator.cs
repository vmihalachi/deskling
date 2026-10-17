using Laching.Companion.Cadence;
using Laching.Companion.Phrases;
using Laching.Companion.Rules;
using Laching.Companion.Settings;
using Laching.Core.Random;
using Laching.Core.Scheduling;

namespace Laching.Companion.Coordination;

/// <summary>
/// The pure composition root: runs the scheduler on each tick, turns what happened into a trigger, asks the rules,
/// picks a line and hands the shell a <see cref="Delivery"/>. Time comes from the clock, system state from the
/// shell's <see cref="Snapshot"/>, randomness from a seeded source, so a whole day of the companion replays exactly
/// (<c>conformance/companion/quiet</c>). The app injects its bank and its language; purchases are not its business (the
/// app keeps <see cref="CompanionSettings.Skin"/> to a skin the user may use, <c>SkinCatalog.EffectiveSkin</c>). Never
/// sleeps, never touches the system. Not thread-safe: the shell calls it from one thread.
/// </summary>
public sealed class CompanionCoordinator
{
    /// <summary>Welcome-back lines are at most this far apart.</summary>
    public const double WelcomeBackCooldownSeconds = 30 * 60;

    /// <summary>How many recent ids are remembered (the picker skips the last <see cref="PhrasePicker.RecentWindow"/>).</summary>
    public const int RecentCapacity = 24;

    /// <summary>Foundation's <c>Date.distantFuture</c>: what "Shh until I say" pauses until. The vectors pin it.</summary>
    public static readonly DateTimeOffset DistantFuture = new(4001, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IClock clock;
    private readonly TimeZoneInfo timeZone;
    private readonly IRandomSource random;
    private readonly List<string> recentIDs = [];
    private Snapshot? lastSnapshot;

    /// <summary>
    /// <paramref name="seed"/> drives both the scheduler's jitter (seed) and the picks (seed + 1); pass a fixed one to
    /// replay. <paramref name="timeZone"/> is the zone whose days and hours the active window, the dayparts and
    /// "rest of today" use (the Swift calendar).
    /// </summary>
    public CompanionCoordinator(
        CompanionSettings settings,
        PhraseBank bank,
        string language,
        IClock clock,
        IIdleTimeProvider idle,
        IBusyStateProvider busy,
        TimeZoneInfo timeZone,
        ulong? seed = null
    )
    {
        Settings = settings;
        Bank = bank;
        Language = language;
        this.clock = clock;
        this.timeZone = timeZone;
        var chosenSeed = seed ?? RandomSeed();
        random = new SplitMix64(unchecked(chosenSeed + 1));
        Scheduler = new ReminderScheduler(ChatterCadence.Config(settings), clock, idle, busy, timeZone, new SplitMix64(chosenSeed));
        if (settings.ShushedUntil is { } until)
        {
            if (until > clock.Now)
                Scheduler.PauseUntil(until);
            else
                settings.ShushedUntil = null;
        }
    }

    public CompanionSettings Settings { get; }

    public PhraseBank Bank { get; }

    /// <summary>
    /// The app's language (BCP-47): phrases that don't exist in it are never picked. Set it when the app's language
    /// changes.
    /// </summary>
    public string Language { get; set; }

    public ReminderScheduler Scheduler { get; }

    public IReadOnlyList<string> RecentIDs => recentIDs;

    public Delivery? LastDelivery { get; private set; }

    /// <summary>Lines the shell can't say right now (pending a recording in the voice in use); never picked.</summary>
    public IReadOnlySet<string> UnavailableIDs { get; set; } = new HashSet<string>(StringComparer.Ordinal);

    public DateTimeOffset? LastWelcomeAt { get; private set; }

    /// <summary>True until the pause ends (the stored end, so it agrees with <see cref="Mood"/> even between ticks).</summary>
    public bool IsShushed => Scheduler.PausedUntil is { } until && clock.Now < until;

    // Ticks and triggers

    /// <summary>Call every 20 s with a fresh snapshot. Returns a line to deliver, or null.</summary>
    public Delivery? Tick(Snapshot snapshot)
    {
        var heeded = Heeded(snapshot);
        var now = clock.Now;
        // The stored pause, not IsShushed: the clock may already be past the end before this tick clears it.
        var wasShushed = Scheduler.PausedUntil is not null;
        // A line held for a call comes back as the after-call line; one held for full screen, as a plain due line.
        var heldForCall = Scheduler.Status is SchedulerStatus.Postponed { Reason: BusyReason.Call };
        var cameBack = CameBack(lastSnapshot, heeded);
        lastSnapshot = heeded;

        var decision = Scheduler.Tick();
        var shushEnded = wasShushed && Scheduler.PausedUntil is null;
        if (shushEnded)
            Settings.ShushedUntil = null;
        switch (decision)
        {
            case SchedulerDecision.Remind:
                return Deliver(heldForCall ? Trigger.AfterCall : Trigger.Due, heeded, now);
            case SchedulerDecision.Postpone:
                return null;
        }
        if (shushEnded)
            return Deliver(Trigger.ShushEnd, heeded, now);
        if (cameBack && !IsShushed)
        {
            if (LastWelcomeAt is { } last && (now - last).TotalSeconds < WelcomeBackCooldownSeconds)
                return null;
            return Deliver(Trigger.WelcomeBack, heeded, now);
        }
        return null;
    }

    /// <summary>
    /// Say something: the toy's button. Restarts the chatter interval. Answers even while a line is being spoken: the
    /// shell cuts that line before it performs this one (<see cref="QuietRules"/>).
    /// </summary>
    public Delivery? Poke(Snapshot snapshot)
    {
        Scheduler.SessionCompleted();
        return Deliver(Trigger.ButtonPress, Heeded(snapshot), clock.Now);
    }

    /// <summary>Something the shell noticed: battery, charger, appearance.</summary>
    public Delivery? Event(Trigger trigger, Snapshot snapshot)
    {
        if (IsShushed)
            return null;
        return Deliver(trigger, Heeded(snapshot), clock.Now);
    }

    // Shh

    public void Shush(ChatterCadence.Shush shush)
    {
        switch (shush)
        {
            case ChatterCadence.Shush.Hour:
                Scheduler.PauseFor(60 * 60);
                break;
            case ChatterCadence.Shush.RestOfToday:
                Scheduler.PauseUntil(Scheduler.EndOfToday);
                break;
            case ChatterCadence.Shush.UntilISay:
                Scheduler.PauseUntil(DistantFuture);
                break;
        }
        Settings.ShushedUntil = Scheduler.PausedUntil;
    }

    /// <summary>Ends Shh early. The character says one line about it.</summary>
    public Delivery? Wake(Snapshot snapshot)
    {
        if (!IsShushed)
            return null;
        Scheduler.Resume();
        Settings.ShushedUntil = null;
        return Deliver(Trigger.ShushEnd, Heeded(snapshot), clock.Now);
    }

    // Settings and state

    /// <summary>Call after a timing setting changed (the shell debounces).</summary>
    public void SettingsChanged() => Scheduler.Update(ChatterCadence.Config(Settings));

    public Mood Mood(Snapshot snapshot) => CompanionState.MoodFor(Scheduler.Status, Heeded(snapshot));

    // Private

    /// <summary>The snapshot as the settings want it read: a full-screen app only counts when "hush in full screen" is on.</summary>
    private Snapshot Heeded(Snapshot snapshot)
    {
        if (Settings.HushInFullScreen || !snapshot.Busy.Contains(BusyReason.FullScreen))
            return snapshot;
        return snapshot with { Busy = snapshot.Busy.Where(reason => reason != BusyReason.FullScreen).ToHashSet() };
    }

    private static bool CameBack(Snapshot? previous, Snapshot current)
    {
        if (previous is null)
            return false;
        var wasGone = previous.IsScreenLocked || previous.IdleSeconds >= QuietRules.AwayThresholdSeconds;
        var isHere = !current.IsScreenLocked && current.IdleSeconds < QuietRules.AwayThresholdSeconds;
        return wasGone && isHere;
    }

    private Delivery? Deliver(Trigger trigger, Snapshot snapshot, DateTimeOffset now)
    {
        if (trigger != Trigger.ButtonPress && !Scheduler.IsWithinActiveHours(now))
            return null;
        if (QuietRules.Decide(trigger, snapshot, Settings.Manners) is not Outcome.Deliver deliver)
            return null;
        var local = TimeZoneInfo.ConvertTime(now, timeZone);
        var context = new PhraseContext(local.Hour, (int)local.DayOfWeek + 1, deliver.Plan.Chime, Settings.Skin, Language)
        {
            Event = trigger.PhraseTag(),
            RecentIDs = recentIDs.ToArray(),
            Flavors = Settings.PhraseFlavors,
            ExcludedIDs = Settings.MutedPhraseIDs.Union(UnavailableIDs).ToHashSet(StringComparer.Ordinal),
        };
        // The unit is drawn even when nothing qualifies, exactly as the Swift reference does.
        var phrase = PhrasePicker.Pick(Bank, context, random.NextUnit());
        if (phrase is null)
            return null;
        recentIDs.Add(phrase.Id);
        if (recentIDs.Count > RecentCapacity)
            recentIDs.RemoveRange(0, recentIDs.Count - RecentCapacity);
        if (trigger == Trigger.WelcomeBack)
            LastWelcomeAt = now;
        var delivery = new Delivery(phrase, trigger, deliver.Plan);
        LastDelivery = delivery;
        return delivery;
    }

    private static ulong RandomSeed()
    {
        var bytes = new byte[8];
        System.Random.Shared.NextBytes(bytes);
        return BitConverter.ToUInt64(bytes, 0);
    }
}
