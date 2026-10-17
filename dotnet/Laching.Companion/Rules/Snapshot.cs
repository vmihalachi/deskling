using Laching.Core.Scheduling;

namespace Laching.Companion.Rules;

/// <summary>
/// What the shell knows about this moment, read from the system without any permission, as a value the rules can
/// judge. Two snapshots are equal when every field is (the busy set by its members).
/// </summary>
public sealed record Snapshot
{
    private static readonly IReadOnlySet<BusyReason> NoBusy = new HashSet<BusyReason>();

    /// <summary>Another app uses the camera or microphone, or an app is full screen.</summary>
    public IReadOnlySet<BusyReason> Busy { get; init; } = NoBusy;

    public double IdleSeconds { get; init; }

    public bool IsScreenLocked { get; init; }

    /// <summary>null when the output state can't be read.</summary>
    public bool? IsOutputMuted { get; init; }

    /// <summary>A line is being spoken right now.</summary>
    public bool IsSpeaking { get; init; }

    /// <summary>Notifications may be shown (the user allowed them earlier).</summary>
    public bool NotificationsAuthorized { get; init; }

    /// <summary>Nobody busy, nobody away, nothing muted.</summary>
    public static Snapshot Calm { get; } = new();

    public bool Equals(Snapshot? other) =>
        other is not null
        && Busy.SetEquals(other.Busy)
        && IdleSeconds.Equals(other.IdleSeconds)
        && IsScreenLocked == other.IsScreenLocked
        && IsOutputMuted == other.IsOutputMuted
        && IsSpeaking == other.IsSpeaking
        && NotificationsAuthorized == other.NotificationsAuthorized;

    public override int GetHashCode() =>
        HashCode.Combine(Busy.Count, IdleSeconds, IsScreenLocked, IsOutputMuted, IsSpeaking, NotificationsAuthorized);
}
