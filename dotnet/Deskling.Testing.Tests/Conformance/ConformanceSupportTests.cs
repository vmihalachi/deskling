namespace Deskling.Testing.Tests.Conformance;

/// <summary>The vectors' date format, written as the Swift generators write it.</summary>
public sealed class ConformanceSupportTests
{
    [Fact]
    public void DatesParseWithAnOffsetWithOrWithoutAFraction()
    {
        Assert.Equal(
            new DateTimeOffset(2026, 9, 28, 10, 0, 0, TimeSpan.FromHours(3)), ConformanceSupport.Date("2026-09-28T10:00:00+03:00"));
        Assert.Equal(new DateTimeOffset(2026, 9, 28, 7, 0, 0, 250, TimeSpan.Zero), ConformanceSupport.Date("2026-09-28T07:00:00.250Z"));
    }

    [Fact]
    public void IsoWritesZForUtcAnOffsetOtherwiseAndMillisecondsOnlyWithAFraction()
    {
        var utc = ConformanceSupport.TimeZone("UTC");
        var bucharest = ConformanceSupport.TimeZone("Europe/Bucharest");
        var date = ConformanceSupport.Date("2026-09-28T07:00:00Z");
        Assert.Equal("2026-09-28T07:00:00Z", ConformanceSupport.Iso(date, utc));
        Assert.Equal("2026-09-28T10:00:00+03:00", ConformanceSupport.Iso(date, bucharest));
        Assert.Equal("2026-09-28T07:00:00.250Z", ConformanceSupport.Iso(date.AddMilliseconds(250), utc));
    }

    [Fact]
    public void IsoRoundTripsThroughDate()
    {
        var bucharest = ConformanceSupport.TimeZone("Europe/Bucharest");
        foreach (var text in new[] { "2026-01-15T09:30:00+02:00", "2026-07-15T09:30:00+03:00", "2026-07-15T09:30:00.500+03:00" })
            Assert.Equal(text, ConformanceSupport.Iso(ConformanceSupport.Date(text), bucharest));
    }
}
