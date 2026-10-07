namespace Deskling.Core.Scheduling;

/// <summary>Everything that decides when <see cref="ReminderScheduler"/> fires. Durations are in seconds.</summary>
public sealed record SchedulerConfig
{
    public double IntervalSeconds { get; init; } = 45 * 60;

    /// <summary>Minutes after local midnight when reminders start.</summary>
    public int ActiveStartMinute { get; init; } = 9 * 60;

    /// <summary>Minutes after local midnight when reminders stop. Before the start means an overnight window.</summary>
    public int ActiveEndMinute { get; init; } = 18 * 60;

    /// <summary>Weekdays with reminders (1 = Sunday ... 7 = Saturday), in the scheduler's time zone.</summary>
    public IReadOnlySet<int> ActiveWeekdays { get; init; } = new HashSet<int> { 2, 3, 4, 5, 6 };

    /// <summary>Hours of the day (0–23) without reminders, inside active hours.</summary>
    public IReadOnlySet<int> QuietHours { get; init; } = new HashSet<int>();

    /// <summary>Being idle for this long counts as "away" (a natural break).</summary>
    public double IdleThresholdSeconds { get; init; } = 5 * 60;

    /// <summary>Hold a due reminder while in a call.</summary>
    public bool PostponeDuringCalls { get; init; } = true;

    /// <summary>Hold a due reminder while the frontmost app is full screen.</summary>
    public bool PostponeDuringFullScreen { get; init; } = true;

    /// <summary>After being busy ends, wait this long before the held reminder fires.</summary>
    public double BusyGraceSeconds { get; init; } = 30;

    /// <summary>"After this call" fires at the latest this long after snoozing if no call starts.</summary>
    public double AfterCallFallbackSeconds { get; init; } = 15 * 60;

    /// <summary>
    /// Each new interval is <see cref="IntervalSeconds"/> plus a random offset within ± this many seconds, rounded
    /// to whole seconds (<c>floor(x + 0.5)</c>), drawn from the scheduler's
    /// <see cref="Deskling.Core.Random.IRandomSource"/>. Zero (the default) keeps every interval exact and never
    /// consults the random source. Keep it well below <see cref="IntervalSeconds"/>.
    /// </summary>
    public double IntervalJitterSeconds { get; init; }

    /// <summary>The busy reasons whose settings are on.</summary>
    public IReadOnlySet<BusyReason> PostponeReasons
    {
        get
        {
            var reasons = new HashSet<BusyReason>();
            if (PostponeDuringCalls)
                reasons.Add(BusyReason.Call);
            if (PostponeDuringFullScreen)
                reasons.Add(BusyReason.FullScreen);
            return reasons;
        }
    }
}

/// <summary>What one <see cref="ReminderScheduler.Tick"/> decided.</summary>
public enum SchedulerDecision
{
    None,
    Remind,

    /// <summary>A reminder is due, but the user is busy (or just stopped being busy). It fires once they're free.</summary>
    Postpone,
}

/// <summary>What the scheduler is doing right now, for a menu or a status line.</summary>
public abstract record SchedulerStatus
{
    private SchedulerStatus()
    {
    }

    /// <summary>The next reminder is due at <see cref="Date"/>.</summary>
    public sealed record Scheduled(DateTimeOffset Date) : SchedulerStatus;

    /// <summary>Paused until <see cref="Until"/>.</summary>
    public sealed record Paused(DateTimeOffset Until) : SchedulerStatus;

    /// <summary>Outside the active hours, weekdays or inside a quiet hour.</summary>
    public sealed record OutsideActiveHours : SchedulerStatus;

    /// <summary>The user has been idle past the threshold; the interval restarts when they're back.</summary>
    public sealed record Away : SchedulerStatus;

    /// <summary>A due reminder is being held until the user is free.</summary>
    public sealed record Postponed(BusyReason Reason) : SchedulerStatus;
}
