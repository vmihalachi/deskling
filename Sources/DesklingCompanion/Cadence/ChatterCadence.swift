import DesklingCore
import Foundation

/// Turns the user's timing settings into the scheduler's config. The scheduler itself (active hours, idle
/// reset, busy hold, pause) is `ReminderScheduler`, pinned by `conformance/scheduler`;
/// `conformance/companion/cadence` pins this mapping.
public enum ChatterCadence {
    /// Being idle this long counts as away: nothing is said to an empty room, and the interval restarts.
    public static let idleThreshold: TimeInterval = 5 * 60
    /// After a call or a full-screen app ends, wait this long before a held line is said.
    public static let busyGrace: TimeInterval = 30

    /// What "Shh" can mean.
    public enum Shush: String, CaseIterable, Codable, Sendable {
        case hour
        case restOfToday
        case untilISay
    }

    public static func config(
        frequency: Frequency, activeStartMinute: Int, activeEndMinute: Int, activeWeekdays: Set<Int>,
        quietHours: Set<Int>, hushInFullScreen: Bool
    ) -> SchedulerConfig {
        SchedulerConfig(
            interval: frequency.interval, activeStartMinute: activeStartMinute, activeEndMinute: activeEndMinute,
            activeWeekdays: activeWeekdays, quietHours: quietHours, idleThreshold: idleThreshold,
            postponeDuringCalls: true, postponeDuringFullScreen: hushInFullScreen, busyGrace: busyGrace,
            intervalJitter: frequency.jitter)
    }
}
