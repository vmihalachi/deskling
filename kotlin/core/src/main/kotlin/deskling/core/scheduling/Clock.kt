package deskling.core.scheduling

import java.time.Instant

/** Where the scheduler reads the time, so tests and conformance vectors drive it. */
public interface Clock {
    public val now: Instant
}

/** The wall clock. */
public object SystemClock : Clock {
    override val now: Instant get() = Instant.now()
}
