namespace Deskling.Core.Random;

/// <summary>A source of uniform random numbers in [0, 1), so anything random can be seeded in tests and vectors.</summary>
public interface IRandomSource
{
    double NextUnit();
}

/// <summary>The system's random generator.</summary>
public sealed class SystemRandom : IRandomSource
{
    public double NextUnit() => System.Random.Shared.NextDouble();
}

/// <summary>
/// SplitMix64 (Steele, Lea and Flood), specified bit for bit so both ports draw the same numbers:
/// <c>state += 0x9E3779B97F4A7C15; z = state; z = (z ^ (z &gt;&gt; 30)) * 0xBF58476D1CE4E5B9;
/// z = (z ^ (z &gt;&gt; 27)) * 0x94D049BB133111EB; return z ^ (z &gt;&gt; 31)</c>, all wrapping on 64 bits.
/// <see cref="NextUnit"/> is the top 53 bits of <see cref="Next"/> divided by 2^53.
/// </summary>
public sealed class SplitMix64 : IRandomSource
{
    public SplitMix64(ulong seed)
    {
        State = seed;
    }

    public ulong State { get; private set; }

    public ulong Next()
    {
        unchecked
        {
            State += 0x9E3779B97F4A7C15UL;
            var z = State;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    public double NextUnit() => (Next() >> 11) * (1.0 / 9007199254740992.0);
}
