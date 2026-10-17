package laching.core.random

/**
 * SplitMix64 (Steele, Lea and Flood 2014), the seeded generator behind the conformance vectors: the same seed gives the
 * same sequence in Swift, C# and Kotlin.
 */
public class SplitMix64(
    seed: ULong,
) : RandomSource {
    public var state: ULong = seed
        private set

    /** The next 64 random bits. */
    public fun next(): ULong {
        state += 0x9E3779B97F4A7C15uL
        var z = state
        z = (z xor (z shr 30)) * 0xBF58476D1CE4E5B9uL
        z = (z xor (z shr 27)) * 0x94D049BB133111EBuL
        return z xor (z shr 31)
    }

    /** The top 53 bits as a double in [0, 1). */
    override fun nextUnit(): Double = (next() shr 11).toDouble() * (1.0 / 9007199254740992.0)
}
