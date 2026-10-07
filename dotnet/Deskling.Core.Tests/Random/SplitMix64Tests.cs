using Deskling.Core.Random;

namespace Deskling.Core.Tests;

public sealed class SplitMix64Tests
{
    // Reference values of the published SplitMix64 algorithm (seed 0 is the sequence most implementations print).
    [Fact]
    public void TestSeedZeroMatchesTheReference()
    {
        var g = new SplitMix64(0);
        Assert.Equal(0xE220A8397B1DCDAFUL, g.Next());
        Assert.Equal(0x6E789E6AA1B965F4UL, g.Next());
        Assert.Equal(0x06C45D188009454FUL, g.Next());
    }

    [Fact]
    public void TestSeed42MatchesTheReference()
    {
        var g = new SplitMix64(42);
        Assert.Equal(13679457532755275413UL, g.Next());
        Assert.Equal(2949826092126892291UL, g.Next());
        Assert.Equal(5139283748462763858UL, g.Next());
    }

    [Fact]
    public void TestUnitsAreTheTop53BitsOverTwoToThe53()
    {
        var g = new SplitMix64(42);
        Assert.Equal(0.7415648787718233, g.NextUnit());
        Assert.Equal(0.1599103928769201, g.NextUnit());
        Assert.Equal(0.27860113025513866, g.NextUnit());
    }

    [Fact]
    public void TestUnitsStayInRange()
    {
        var g = new SplitMix64(123_456_789);
        for (var i = 0; i < 10_000; i++)
        {
            var unit = g.NextUnit();
            Assert.InRange(unit, 0.0, 1.0);
            Assert.NotEqual(1.0, unit);
        }
    }

    [Fact]
    public void TestSameSeedSameSequence()
    {
        Assert.Equal(Draw(9, 5), Draw(9, 5));
        Assert.NotEqual(Draw(9, 5), Draw(10, 5));
    }

    [Fact]
    public void TestStateAdvancesByTheGoldenGamma()
    {
        var g = new SplitMix64(1);
        _ = g.Next();
        Assert.Equal(1 + 0x9E3779B97F4A7C15UL, g.State);
    }

    private static ulong[] Draw(ulong seed, int count)
    {
        var g = new SplitMix64(seed);
        var values = new ulong[count];
        for (var i = 0; i < count; i++)
            values[i] = g.Next();
        return values;
    }
}
