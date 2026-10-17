using Laching.Core.Scheduling;

namespace Laching.Companion.Rules;

/// <summary>How the character looks and behaves right now. The raw values (<see cref="Moods.RawValue"/>) appear in the vectors; never rename one.</summary>
public enum Mood
{
    Awake,
    Talking,

    /// <summary>Shh is on: drooped, mouth closed.</summary>
    Shushed,

    /// <summary>Away or the screen is locked: eyes closed, drooped.</summary>
    Asleep,

    /// <summary>A call holds the lines.</summary>
    HushedForCall,

    /// <summary>A full-screen app holds the lines.</summary>
    HushedFullScreen,

    /// <summary>Outside the active hours and days.</summary>
    OffHours,
}

/// <summary>The mood tables and raw values (Swift's <c>Mood.allCases</c>, <c>rawValue</c>).</summary>
public static class Moods
{
    /// <summary>Every mood, in declaration order.</summary>
    public static IReadOnlyList<Mood> All { get; } = Enum.GetValues<Mood>();

    public static string RawValue(this Mood mood) =>
        mood switch
        {
            Mood.Awake => "awake",
            Mood.Talking => "talking",
            Mood.Shushed => "shushed",
            Mood.Asleep => "asleep",
            Mood.HushedForCall => "hushedForCall",
            Mood.HushedFullScreen => "hushedFullScreen",
            Mood.OffHours => "offHours",
            _ => throw new ArgumentOutOfRangeException(nameof(mood), mood, "unknown mood"),
        };

    /// <summary>The inverse of <see cref="RawValue"/>: null for an unknown name.</summary>
    public static Mood? Parse(string? rawValue) =>
        rawValue switch
        {
            "awake" => Mood.Awake,
            "talking" => Mood.Talking,
            "shushed" => Mood.Shushed,
            "asleep" => Mood.Asleep,
            "hushedForCall" => Mood.HushedForCall,
            "hushedFullScreen" => Mood.HushedFullScreen,
            "offHours" => Mood.OffHours,
            _ => null,
        };
}

/// <summary>Derives the character's mood from the scheduler's status and the moment. Pure; part of <c>conformance/companion/quiet</c>.</summary>
public static class CompanionState
{
    public static Mood MoodFor(SchedulerStatus status, Snapshot snapshot)
    {
        if (snapshot.IsSpeaking)
            return Mood.Talking;
        if (status is SchedulerStatus.Paused)
            return Mood.Shushed;
        if (snapshot.Busy.Contains(BusyReason.Call))
            return Mood.HushedForCall;
        if (snapshot.Busy.Contains(BusyReason.FullScreen))
            return Mood.HushedFullScreen;
        if (snapshot.IsScreenLocked || snapshot.IdleSeconds >= QuietRules.AwayThresholdSeconds)
            return Mood.Asleep;
        if (status is SchedulerStatus.Postponed postponed)
            return postponed.Reason == BusyReason.Call ? Mood.HushedForCall : Mood.HushedFullScreen;
        if (status is SchedulerStatus.Away)
            return Mood.Asleep;
        if (status is SchedulerStatus.OutsideActiveHours)
            return Mood.OffHours;
        return Mood.Awake;
    }
}
