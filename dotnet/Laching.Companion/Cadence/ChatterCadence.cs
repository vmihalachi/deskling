using Laching.Core.Scheduling;
using Laching.Companion.Settings;

namespace Laching.Companion.Cadence;

/// <summary>
/// Turns the user's timing settings into the scheduler's config. The scheduler itself (active hours, idle reset,
/// busy hold, pause) is <see cref="ReminderScheduler"/>, pinned by <c>conformance/scheduler</c>;
/// <c>conformance/companion/cadence</c> pins this mapping.
/// </summary>
public static class ChatterCadence
{
    /// <summary>Being idle this long counts as away: nothing is said to an empty room, and the interval restarts.</summary>
    public const double IdleThresholdSeconds = 5 * 60;

    /// <summary>
    /// After a call or a full-screen app ends, wait about this long before a held line is said (20–40 s with the shell's
    /// 20 s ticks: <see cref="SchedulerConfig.BusyGraceSeconds"/>).
    /// </summary>
    public const double BusyGraceSeconds = 30;

    /// <summary>What "Shh" can mean. The raw values (<see cref="Shushes.RawValue"/>) are in the vectors; never rename one.</summary>
    public enum Shush
    {
        Hour,
        RestOfToday,
        UntilISay,
    }

    public static SchedulerConfig Config(
        Frequency frequency,
        int activeStartMinute,
        int activeEndMinute,
        IReadOnlySet<int> activeWeekdays,
        IReadOnlySet<int> quietHours,
        bool hushInFullScreen
    ) =>
        new()
        {
            IntervalSeconds = frequency.IntervalSeconds(),
            ActiveStartMinute = activeStartMinute,
            ActiveEndMinute = activeEndMinute,
            ActiveWeekdays = activeWeekdays,
            QuietHours = quietHours,
            IdleThresholdSeconds = ChatterCadence.IdleThresholdSeconds,
            PostponeDuringCalls = true,
            PostponeDuringFullScreen = hushInFullScreen,
            BusyGraceSeconds = ChatterCadence.BusyGraceSeconds,
            IntervalJitterSeconds = frequency.JitterSeconds(),
        };

    public static SchedulerConfig Config(CompanionSettings settings) =>
        Config(
            settings.Frequency,
            settings.ActiveStartMinute,
            settings.ActiveEndMinute,
            settings.ActiveWeekdays,
            settings.QuietHours,
            settings.HushInFullScreen
        );
}

/// <summary>The raw values of <see cref="ChatterCadence.Shush"/> (Swift's <c>rawValue</c>).</summary>
public static class Shushes
{
    /// <summary>Every kind, in declaration order (the <c>shushes</c> list in settings.json).</summary>
    public static IReadOnlyList<ChatterCadence.Shush> All { get; } = Enum.GetValues<ChatterCadence.Shush>();

    public static string RawValue(this ChatterCadence.Shush shush) =>
        shush switch
        {
            ChatterCadence.Shush.Hour => "hour",
            ChatterCadence.Shush.RestOfToday => "restOfToday",
            ChatterCadence.Shush.UntilISay => "untilISay",
            _ => throw new ArgumentOutOfRangeException(nameof(shush), shush, "unknown shush"),
        };

    /// <summary>The inverse of <see cref="RawValue"/>: null for an unknown name.</summary>
    public static ChatterCadence.Shush? Parse(string? rawValue) => RawValues.Parse(All, RawValue, rawValue);
}
