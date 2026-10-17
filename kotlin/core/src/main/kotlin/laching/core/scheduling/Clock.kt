package laching.core.scheduling

import java.time.Instant
import kotlin.math.roundToLong

/** Where the scheduler reads the time, so tests and conformance vectors drive it. */
public interface Clock {
    public val now: Instant
}

/** The wall clock. */
public object SystemClock : Clock {
    override val now: Instant get() = Instant.now()
}

/**
 * [Instant.plusSeconds] for a fractional number of seconds, rounded to the nanosecond: the rule the scheduler and a
 * replayed conformance vector both move time by, so a test clock that advances with it lands on the same instants.
 */
public fun Instant.plusSeconds(seconds: Double): Instant = plusNanos((seconds * 1_000_000_000).roundToLong())
