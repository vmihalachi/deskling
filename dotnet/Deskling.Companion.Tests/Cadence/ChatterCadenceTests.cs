using Deskling.Core.Settings;
using Deskling.Companion.Cadence;
using Deskling.Companion.Settings;

namespace Deskling.Companion.Tests;

public sealed class ChatterCadenceTests
{
    [Fact]
    public void Presets()
    {
        Assert.Equal(3600.0, Frequency.Rarely.IntervalSeconds());
        Assert.Equal(900.0, Frequency.Rarely.JitterSeconds());
        Assert.Equal(1800.0, Frequency.Sometimes.IntervalSeconds());
        Assert.Equal(600.0, Frequency.Sometimes.JitterSeconds());
        Assert.Equal(900.0, Frequency.Chatty.IntervalSeconds());
        Assert.Equal(300.0, Frequency.Chatty.JitterSeconds());
    }

    [Fact]
    public void ConfigMapsSettingsOntoTheScheduler()
    {
        var config = ChatterCadence.Config(Frequency.Chatty, 8 * 60, 20 * 60, new HashSet<int> { 1, 7 }, new HashSet<int> { 13 }, false);
        Assert.Equal(900.0, config.IntervalSeconds);
        Assert.Equal(300.0, config.IntervalJitterSeconds);
        Assert.Equal(480, config.ActiveStartMinute);
        Assert.Equal(1200, config.ActiveEndMinute);
        Assert.Equal(new[] { 1, 7 }, config.ActiveWeekdays.Order().ToArray());
        Assert.Equal(new[] { 13 }, config.QuietHours.Order().ToArray());
        Assert.Equal(300.0, config.IdleThresholdSeconds);
        Assert.True(config.PostponeDuringCalls);
        Assert.False(config.PostponeDuringFullScreen);
        Assert.Equal(30.0, config.BusyGraceSeconds);
    }

    [Fact]
    public void DefaultSettingsGiveTheDefaultConfig()
    {
        var settings = new CompanionSettings(new InMemorySettingsStore(), Fixture.Defaults);
        var config = ChatterCadence.Config(settings);
        Assert.Equal(1800.0, config.IntervalSeconds);
        Assert.Equal(600.0, config.IntervalJitterSeconds);
        Assert.Equal(new[] { 2, 3, 4, 5, 6 }, config.ActiveWeekdays.Order().ToArray());
        Assert.True(config.PostponeDuringFullScreen);
    }

    [Fact]
    public void RawValuesRoundTrip()
    {
        foreach (var frequency in Frequencies.All)
            Assert.Equal(frequency, Frequencies.Parse(frequency.RawValue()));
        foreach (var shush in Shushes.All)
            Assert.Equal(shush, Shushes.Parse(shush.RawValue()));
        Assert.Null(Frequencies.Parse("Sometimes"));
        Assert.Null(Shushes.Parse("forever"));
    }
}
