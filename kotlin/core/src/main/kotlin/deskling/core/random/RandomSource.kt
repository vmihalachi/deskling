package deskling.core.random

/** A source of uniform numbers in [0, 1), injected so the scheduler's jitter replays from a seed in tests. */
public fun interface RandomSource {
    public fun nextUnit(): Double
}

/** The platform's random numbers. */
public class SystemRandom : RandomSource {
    override fun nextUnit(): Double = kotlin.random.Random.nextDouble()
}
