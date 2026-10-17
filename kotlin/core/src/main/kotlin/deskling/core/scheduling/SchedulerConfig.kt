package deskling.core.scheduling

/**
 * What the user chose about reminders. Minutes of the day count from local midnight; weekdays are 1 = Sunday … 7 =
 * Saturday, as the conformance vectors write them.
 */
public data class SchedulerConfig(
    /** Seconds between reminders. */
    val intervalSeconds: Double = 45.0 * 60,
    /** Start of the active window, in minutes of the day. */
    val activeStartMinute: Int = 9 * 60,
    /** End of the active window, in minutes of the day; before the start, the window runs overnight. */
    val activeEndMinute: Int = 18 * 60,
    val activeWeekdays: Set<Int> = setOf(2, 3, 4, 5, 6),
    /** Hours of the day (0…23) with no reminders. */
    val quietHours: Set<Int> = emptySet(),
    /** After this many seconds without input the user is away and the timer restarts when they are back. */
    val idleThresholdSeconds: Double = 5.0 * 60,
    val postponeDuringCalls: Boolean = true,
    val postponeDuringFullScreen: Boolean = true,
    /**
     * About how long a held reminder waits after the user stops being busy before it fires: counted from the last
     * tick that saw the user busy, so with ticks every 20 s it fires 20–40 s after being busy really ended.
     */
    val busyGraceSeconds: Double = 30.0,
    /** When the user asks to be reminded after a call that never ends, remind after this many seconds anyway. */
    val afterCallFallbackSeconds: Double = 15.0 * 60,
    /**
     * Each interval is lengthened or shortened by up to this many seconds, drawn from the scheduler's [RandomSource]
     * in whole seconds; 0 keeps every interval exact.
     */
    val intervalJitterSeconds: Double = 0.0,
) {
    /** The busy reasons that postpone a reminder, from the two switches. */
    val postponeReasons: Set<BusyReason>
        get() =
            buildSet {
                if (postponeDuringCalls) add(BusyReason.CALL)
                if (postponeDuringFullScreen) add(BusyReason.FULL_SCREEN)
            }
}
