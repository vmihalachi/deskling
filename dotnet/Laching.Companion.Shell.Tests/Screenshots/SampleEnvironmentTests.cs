using Laching.Companion.Shell.Screenshots;
using Laching.Core.Store;

namespace Laching.Companion.Shell.Tests.Screenshots;

public sealed class SampleEnvironmentTests
{
    [Fact]
    public void SampleNowIsTheNextTuesdayMorning()
    {
        var monday = new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateTimeOffset(2026, 9, 29, 10, 30, 0, TimeSpan.Zero), SampleEnvironment.SampleNow(TimeZoneInfo.Utc, monday));
        var tuesdayNoon = monday.AddDays(1);
        Assert.Equal(new DateTimeOffset(2026, 10, 6, 10, 30, 0, TimeSpan.Zero), SampleEnvironment.SampleNow(TimeZoneInfo.Utc, tuesdayNoon));
    }

    [Fact]
    public void NothingIsBusyIdleOrReal()
    {
        var now = new DateTimeOffset(2026, 9, 29, 10, 30, 0, TimeSpan.Zero);
        var sample = new SampleEnvironment(new ProductCatalog([]), new Dictionary<string, string>(), new ManualTimers(), now);
        var snapshot = sample.Signals.Snapshot();
        Assert.Empty(snapshot.Busy);
        Assert.Equal(0, snapshot.IdleSeconds);
        Assert.Equal(now, sample.Clock.Now);
    }

    [Fact]
    public void TheSeedIsTheSameEveryRunAndOnEverySide()
    {
        var first = new SampleEnvironment(new ProductCatalog([]), new Dictionary<string, string>(), new ManualTimers());
        var second = new SampleEnvironment(new ProductCatalog([]), new Dictionary<string, string>(), new ManualTimers());
        Assert.Equal(first.Seed, second.Seed);
        Assert.Equal(20_261_013UL, first.Seed);  // Swift's ScreenshotCapture.SampleEnvironment.sampleSeed
    }

    [Fact]
    public async Task ThePricesAreFixedAndPurchasesCancelled()
    {
        var catalog = new ProductCatalog(["fixture.supporter"], ["fixture.tip"]);
        var sample = new SampleEnvironment(catalog, new Dictionary<string, string> { ["fixture.supporter"] = "1,99 €" }, new ManualTimers());
        var store = sample.Purchases();
        await store.LoadProductsAsync();
        Assert.Equal("1,99 €", store.Price("fixture.supporter"));
        Assert.Null(store.Price("fixture.tip"));
        await store.BuyAsync("fixture.supporter");
        Assert.False(store.IsOwned("fixture.supporter"));
        sample.Backend.Owned = ["fixture.supporter"];
        await store.RefreshEntitlementsAsync();
        Assert.True(store.IsOwned("fixture.supporter"));
    }
}
