import Foundation

/// A source of uniform random numbers in `[0, 1)`, so anything random can be seeded in tests and vectors.
public protocol RandomSource {
    mutating func nextUnit() -> Double
}

/// The system's random generator.
public struct SystemRandom: RandomSource, Sendable {
    public init() {}
    public mutating func nextUnit() -> Double { Double.random(in: 0..<1) }
}

/// SplitMix64 (Steele, Lea and Flood), specified bit for bit so the .NET port draws the same numbers:
/// `state += 0x9E3779B97F4A7C15; z = state; z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
/// z = (z ^ (z >> 27)) * 0x94D049BB133111EB; return z ^ (z >> 31)`, all wrapping on 64 bits.
/// `nextUnit()` is the top 53 bits of `next()` divided by 2^53.
public struct SplitMix64: RandomSource, Equatable, Sendable {
    public private(set) var state: UInt64

    public init(seed: UInt64) {
        state = seed
    }

    public mutating func next() -> UInt64 {
        state &+= 0x9E37_79B9_7F4A_7C15
        var z = state
        z = (z ^ (z >> 30)) &* 0xBF58_476D_1CE4_E5B9
        z = (z ^ (z >> 27)) &* 0x94D0_49BB_1331_11EB
        return z ^ (z >> 31)
    }

    public mutating func nextUnit() -> Double {
        Double(next() >> 11) * (1.0 / 9_007_199_254_740_992.0)
    }
}
