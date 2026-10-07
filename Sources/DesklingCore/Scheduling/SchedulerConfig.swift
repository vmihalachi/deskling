import Foundation

/// Everything that decides when `ReminderScheduler` fires.
public struct SchedulerConfig: Equatable, Sendable {
    public var interval: TimeInterval = 45 * 60
    /// Minutes after midnight when reminders start / stop.
    public var activeStartMinute: Int = 9 * 60
    public var activeEndMinute: Int = 18 * 60
    /// Calendar weekdays (1 = Sunday ... 7 = Saturday).
    public var activeWeekdays: Set<Int> = [2, 3, 4, 5, 6]
    /// Hours of the day (0–23) without reminders, inside active hours.
    public var quietHours: Set<Int> = []
    /// Being idle for this long counts as "away" (a natural break).
    public var idleThreshold: TimeInterval = 5 * 60
    /// Hold a due reminder while in a call / a full-screen app.
    public var postponeDuringCalls = true
    public var postponeDuringFullScreen = true
    /// After being busy ends, wait this long before the held reminder fires.
    public var busyGrace: TimeInterval = 30
    /// "After this call" fires at the latest this long after snoozing if no call starts.
    public var afterCallFallback: TimeInterval = 15 * 60
    /// Each new interval is `interval` plus a random offset within ±`intervalJitter`, rounded to whole seconds
    /// (`floor(x + 0.5)`), drawn from the scheduler's `RandomSource`. Zero (the default) keeps every interval
    /// exact and never consults the random source. Keep it well below `interval`.
    public var intervalJitter: TimeInterval = 0

    public init(
        interval: TimeInterval = 45 * 60, activeStartMinute: Int = 9 * 60, activeEndMinute: Int = 18 * 60,
        activeWeekdays: Set<Int> = [2, 3, 4, 5, 6], quietHours: Set<Int> = [], idleThreshold: TimeInterval = 5 * 60,
        postponeDuringCalls: Bool = true, postponeDuringFullScreen: Bool = true, busyGrace: TimeInterval = 30,
        afterCallFallback: TimeInterval = 15 * 60, intervalJitter: TimeInterval = 0
    ) {
        self.interval = interval
        self.activeStartMinute = activeStartMinute
        self.activeEndMinute = activeEndMinute
        self.activeWeekdays = activeWeekdays
        self.quietHours = quietHours
        self.idleThreshold = idleThreshold
        self.postponeDuringCalls = postponeDuringCalls
        self.postponeDuringFullScreen = postponeDuringFullScreen
        self.busyGrace = busyGrace
        self.afterCallFallback = afterCallFallback
        self.intervalJitter = intervalJitter
    }

    public var postponeReasons: Set<BusyReason> {
        var reasons: Set<BusyReason> = []
        if postponeDuringCalls { reasons.insert(.call) }
        if postponeDuringFullScreen { reasons.insert(.fullScreen) }
        return reasons
    }
}

public enum SchedulerDecision: Equatable, Sendable {
    case none
    case remind
    /// A reminder is due, but the user is busy (or just stopped being busy). It fires once they're free.
    case postpone
}

public enum SchedulerStatus: Equatable, Sendable {
    case scheduled(Date)
    case paused(until: Date)
    case outsideActiveHours
    case away
    /// A due reminder is being held until the user is free.
    case postponed(BusyReason)
}
