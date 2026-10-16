package deskling.testing

import deskling.core.random.RandomSource
import deskling.core.scheduling.BusyReason
import deskling.core.scheduling.BusyStateProvider
import deskling.core.scheduling.Clock
import deskling.core.scheduling.IdleTimeProvider
import java.time.Instant
import kotlin.math.roundToLong

/** A clock the test moves. */
public class MockClock(
    override var now: Instant,
) : Clock {
    public fun advanceSeconds(seconds: Double) {
        now = now.plusNanos((seconds * 1_000_000_000).roundToLong())
    }

    public fun advanceMinutes(minutes: Double): Unit = advanceSeconds(minutes * 60)
}

/** Idle time the test sets. */
public class MockIdle(
    public var idle: Double = 0.0,
) : IdleTimeProvider {
    override fun secondsSinceLastInput(): Double = idle
}

/** Busy reasons the test sets. */
public class MockBusy(
    public val reasons: MutableSet<BusyReason> = mutableSetOf(),
) : BusyStateProvider {
    override fun currentBusyReasons(): Set<BusyReason> = reasons.toSet()
}

/** Always 0.5, counting how often it was read: for tests that check when randomness is consumed. */
public class CountingRandom(
    private val onRead: (() -> Unit)? = null,
) : RandomSource {
    public var reads: Int = 0
        private set

    override fun nextUnit(): Double {
        reads++
        onRead?.invoke()
        return 0.5
    }
}

/** Returns [values] in order, then repeats the last one. */
public class ScriptedRandom(
    private val values: List<Double>,
) : RandomSource {
    init {
        require(values.isNotEmpty()) { "ScriptedRandom needs at least one value" }
    }

    public var reads: Int = 0
        private set

    override fun nextUnit(): Double {
        val value = values[minOf(reads, values.size - 1)]
        reads++
        return value
    }
}
