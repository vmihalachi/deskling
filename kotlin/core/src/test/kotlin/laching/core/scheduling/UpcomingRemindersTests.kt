package laching.core.scheduling

import laching.testing.MockBusy
import laching.testing.MockClock
import laching.testing.MockIdle
import java.time.Instant
import java.time.LocalDateTime
import java.time.ZoneId
import kotlin.test.Test
import kotlin.test.assertEquals

/** [ReminderScheduler.upcomingReminders], as `ReminderSchedulerTests.swift` checks it: the dates an app schedules ahead. */
class UpcomingRemindersTests {
    private val zone = ZoneId.of("UTC")

    /** Monday 2026-09-28, a working day on the default schedule (9–18, Monday to Friday, 45 min). */
    private fun monday(
        hour: Int,
        minute: Int = 0,
    ): Instant = LocalDateTime.of(2026, 9, 28, hour, minute).atZone(zone).toInstant()

    private fun tuesday(
        hour: Int,
        minute: Int = 0,
    ): Instant = monday(hour, minute).plusSeconds(86_400)

    private fun makeScheduler(start: Instant): ReminderScheduler =
        ReminderScheduler(SchedulerConfig(), MockClock(start), MockIdle(), MockBusy(), timeZone = zone)

    @Test
    fun `upcoming reminders follow the interval`() {
        val s = makeScheduler(monday(9, 30))
        assertEquals(listOf(monday(10, 15), monday(11), monday(11, 45)), s.upcomingReminders(monday(9, 30), monday(12)))
        assertEquals(listOf(monday(10, 15), monday(11)), s.upcomingReminders(monday(9, 30), monday(12), limit = 2))
        assertEquals(monday(10, 15), s.nextFireDate, "planning leaves the scheduler as it was")
    }

    @Test
    fun `upcoming reminders skip to the next active day`() {
        val s = makeScheduler(monday(17))
        assertEquals(
            listOf(monday(17, 45), tuesday(9, 45), tuesday(10, 30)),
            s.upcomingReminders(monday(17), tuesday(11)),
        )
    }

    @Test
    fun `upcoming reminders start after a pause`() {
        val s = makeScheduler(monday(10))
        s.pauseUntil(monday(11))
        assertEquals(listOf(monday(11, 45), monday(12, 30)), s.upcomingReminders(monday(10), monday(13)))
    }

    @Test
    fun `upcoming reminders follow a restored date`() {
        val s = makeScheduler(monday(10))
        s.restore(monday(10, 5))
        assertEquals(listOf(monday(10, 5), monday(10, 50)), s.upcomingReminders(monday(10), monday(11)))
    }

    @Test
    fun `nothing is planned without an active weekday or an interval`() {
        val none =
            ReminderScheduler(
                SchedulerConfig(activeWeekdays = emptySet()),
                MockClock(monday(10)),
                MockIdle(),
                MockBusy(),
                timeZone = zone,
            )
        assertEquals(emptyList(), none.upcomingReminders(monday(10), monday(13)))
        val s = makeScheduler(monday(10))
        assertEquals(emptyList(), s.upcomingReminders(monday(10), monday(10)), "an empty range")
        assertEquals(emptyList(), s.upcomingReminders(monday(10), monday(13), limit = 0))
    }
}
