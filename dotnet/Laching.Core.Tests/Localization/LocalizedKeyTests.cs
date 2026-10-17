using System.Text.Json;
using Laching.Core.Localization;

namespace Laching.Core.Tests;

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
    public void TestArgumentsCarryTheirKind()
    {
        Assert.Equal(new LocalizedKey.Argument(LocalizedKey.ArgumentKind.Int, 3), LocalizedKey.Argument.Int(3));
        Assert.Equal(
            new LocalizedKey.Argument(LocalizedKey.ArgumentKind.DurationMinutes, 65),
            LocalizedKey.Argument.DurationMinutes(65));
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

    [Fact]
    public void TestBothArgumentKindsRoundTripThroughJson()
    {
        var key = new LocalizedKey(
            "stats.breaks",
            [LocalizedKey.Argument.Int(3), LocalizedKey.Argument.DurationMinutes(65)]);

        var json = JsonSerializer.Serialize(key, LocalizedKeyJson.Default.LocalizedKey);
        Assert.Equal(
            """{"key":"stats.breaks","args":[{"type":"int","value":3},{"type":"durationMinutes","value":65}]}""",
            json);

        var decoded = JsonSerializer.Deserialize(json, LocalizedKeyJson.Default.LocalizedKey);
        Assert.Equal(key, decoded);
    }

    [Fact]
    public void TestAnUnknownKindFailsToDeserialize()
    {
        const string json = """{"key":"stats.breaks","args":[{"type":"float","value":3}]}""";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, LocalizedKeyJson.Default.LocalizedKey));
    }
}
