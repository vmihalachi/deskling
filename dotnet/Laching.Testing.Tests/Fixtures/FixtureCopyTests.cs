using Laching.Companion.Shell.Signals;

namespace Laching.Testing.Tests.Fixtures;

/// <summary>The fixture model's words, the loaded-once fixtures and the signal fakes.</summary>
public sealed class FixtureCopyTests
{
    [Fact]
    public void TheMenuIsPlainUnlessAccessKeysAreAsked()
    {
        Assert.Equal("Say something", FixtureCopy.Menu(accessKeys: false).Say);
        Assert.Equal("Thanks for supporting ♥…", FixtureCopy.Menu(accessKeys: false).ThanksForSupporting);
        Assert.Equal("&Say something", FixtureCopy.Menu(accessKeys: true).Say);
        Assert.Equal(FixtureWindow.Settings, FixtureCopy.Roles.Settings);
    }

    [Fact]
    public void TimesUseTheGivenFormat()
    {
        var time = new DateTimeOffset(2026, 9, 28, 11, 5, 0, TimeSpan.Zero);
        Assert.Equal("Shh until 11:05", FixtureCopy.Status("HH:mm").ShhUntil(time));
        Assert.Equal("Back at 11:05", FixtureCopy.Desktop("HH:mm").BackAt(time));
    }

    [Fact]
    public void TheFixturesLoadOnce()
    {
        Assert.Same(Fixture.Vector, Fixture.Vector);
        Assert.Equal(FixtureIds.VectorSkin, Fixture.Vector.Skin);
        Assert.Equal(FixtureIds.RasterSkin, Fixture.Raster.Skin);
        Assert.NotEmpty(Fixture.Bank.Phrases);
    }

    [Fact]
    public void TheSignalFakesRaiseTheirChanges()
    {
        var power = new FakePower();
        var appearance = new FakeAppearance();
        var changes = new List<string>();
        power.Changed += () => changes.Add("power");
        appearance.Changed += dark => changes.Add(dark ? "dark" : "light");
        power.Set(new PowerState(IsOnBattery: true, Percent: 19));
        appearance.Set(true);
        Assert.Equal(["power", "dark"], changes);
        Assert.Equal(new PowerState(true, 19), power.CurrentPowerState());
        Assert.True(appearance.IsDark);
        Assert.True(new FakeScreenLock { IsLocked = true }.IsLocked);
    }
}
