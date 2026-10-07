using Deskling.Core.Localization;

namespace Deskling.Core.Tests;

public sealed class LocalizedKeyTests
{
    [Fact]
    public void TestKeyAloneHasNoArguments()
    {
        var key = new LocalizedKey("menu.start");
        Assert.Equal("menu.start", key.Key);
        Assert.Empty(key.Args);
    }

    [Fact]
    public void TestArgumentsCarryTheirType()
    {
        Assert.Equal(new LocalizedKey.Argument("int", 3), LocalizedKey.Argument.Int(3));
        Assert.Equal(new LocalizedKey.Argument("durationMinutes", 65), LocalizedKey.Argument.DurationMinutes(65));
    }

    [Fact]
    public void TestEqualityComparesArgumentsByValue()
    {
        var a = new LocalizedKey("stats.breaks", [LocalizedKey.Argument.Int(3)]);
        var b = new LocalizedKey("stats.breaks", new List<LocalizedKey.Argument> { LocalizedKey.Argument.Int(3) });
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, new LocalizedKey("stats.breaks", [LocalizedKey.Argument.Int(4)]));
        Assert.NotEqual(a, new LocalizedKey("stats.breaks", [LocalizedKey.Argument.DurationMinutes(3)]));
        Assert.NotEqual(a, new LocalizedKey("stats.breaks"));
        Assert.NotEqual(a, new LocalizedKey("stats.moves", [LocalizedKey.Argument.Int(3)]));
    }
}
