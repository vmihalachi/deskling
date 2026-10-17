using Laching.Core.Random;

namespace Laching.Testing;

/// <summary>A random source that returns the given unit values in order, then repeats the last one.</summary>
public sealed class ScriptedRandom(IReadOnlyList<double> values) : IRandomSource
{
    public int Reads { get; private set; }

    public double NextUnit()
    {
        var value = values[Math.Min(Reads, values.Count - 1)];
        Reads++;
        return value;
    }
}
