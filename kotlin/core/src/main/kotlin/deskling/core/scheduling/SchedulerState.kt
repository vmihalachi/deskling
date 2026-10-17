package deskling.core.scheduling

import java.time.Instant

/** What a [ReminderScheduler.tick] asks the app to do. */
public enum class SchedulerDecision {
    /** Nothing this tick. */
    NONE,

    /** Show the reminder. */
    REMIND,

    /** The reminder is due but the user is busy: hold it. */
    POSTPONE,
}

/** Where the scheduler stands, for the menu or the status line. */
public sealed interface SchedulerStatus {
    /** The next reminder comes at [date]. */
    public data class Scheduled(
        val date: Instant,
    ) : SchedulerStatus

    /** Reminders are off until [until]. */
    public data class Paused(
        val until: Instant,
    ) : SchedulerStatus

    /** Outside the active hours or days, or in a quiet hour. */
    public data object OutsideActiveHours : SchedulerStatus

    /** The user has been away from the keyboard past the idle threshold. */
    public data object Away : SchedulerStatus

    /** A reminder is due and held while the user is busy for [reason]. */
    public data class Postponed(
        val reason: BusyReason,
    ) : SchedulerStatus
}
