using Deskling.Core.Settings;
using Deskling.Core.Store;

namespace Deskling.Core.Tests.Store;

public sealed class PurchaseStoreTests
{
    private const string Supporter = "test.supporter";
    private const string SmallTip = "test.tip.small";
    private const string MediumTip = "test.tip.medium";
    private static readonly ProductCatalog Catalog = new([Supporter], [SmallTip, MediumTip]);

    private static MockStoreBackend MakeBackend() => new(Catalog);

    private static PurchaseStore MakeStore(MockStoreBackend backend, ISettingsStore? settings = null) =>
        new(Catalog, backend, settings ?? new InMemorySettingsStore(), cacheKey: "test.owned");

    [Fact]
    public void CatalogListsUnlockablesFirst()
    {
        Assert.Equal([Supporter, SmallTip, MediumTip], Catalog.All);
        Assert.True(Catalog.IsUnlockable(Supporter));
        Assert.False(Catalog.IsTip(Supporter));
        Assert.True(Catalog.IsTip(SmallTip));
    }

    [Fact]
    public async Task BuyingAnUnlockableMakesItOwned()
    {
        var backend = MakeBackend();
        var store = MakeStore(backend);
        await store.StartAsync();
        Assert.False(store.IsOwned(Supporter));
        Assert.Empty(store.Owned);
        Assert.Equal($"€{Supporter.Length}", store.Price(Supporter));
        Assert.Equal(LoadState.Loaded, store.LoadState);

        await store.BuyAsync(Supporter);
        Assert.True(store.IsOwned(Supporter));
        Assert.Equal([Supporter], store.Owned);
        Assert.Equal([Supporter], backend.Purchased);
        Assert.False(store.JustTipped);
        Assert.Null(store.Error);
    }

    [Fact]
    public async Task OwnershipIsCachedForNextLaunch()
    {
        var settings = new InMemorySettingsStore();
        var backend = MakeBackend();
        backend.Owned = [Supporter];
        var first = MakeStore(backend, settings);
        await first.StartAsync();
        Assert.True(first.IsOwned(Supporter));
        // Before the store answers, the cached value keeps the unlock steady.
        Assert.True(MakeStore(MakeBackend(), settings).IsOwned(Supporter));
        Assert.Equal(new[] { Supporter }, settings.Get("test.owned"));
        // Another key is another cache.
        var other = new PurchaseStore(Catalog, MakeBackend(), settings, cacheKey: "other");
        Assert.False(other.IsOwned(Supporter));
    }

    [Fact]
    public async Task RefundLocksAgain()
    {
        var backend = MakeBackend();
        backend.Owned = [Supporter];
        var store = MakeStore(backend);
        await store.StartAsync();
        Assert.True(store.IsOwned(Supporter));

        backend.Owned = [];
        backend.Send(new TransactionUpdate(Supporter, IsRevoked: true));
        await Task.Yield();
        Assert.False(store.IsOwned(Supporter));
        Assert.Empty(store.Owned);
    }

    [Fact]
    public async Task UpdateWithoutAnIdRereadsOwnership()
    {
        var backend = MakeBackend();
        var store = MakeStore(backend);
        await store.StartAsync();

        backend.Owned = [Supporter];
        backend.Send(new TransactionUpdate("", IsRevoked: false));
        await Task.Yield();
        Assert.True(store.IsOwned(Supporter));
        Assert.False(store.JustTipped);
    }

    [Fact]
    public async Task TipsUnlockNothing()
    {
        var backend = MakeBackend();
        var store = MakeStore(backend);
        await store.StartAsync();
        await store.TipAsync(MediumTip);
        Assert.Equal([MediumTip], backend.Purchased);
        Assert.Empty(store.Owned);
        Assert.False(store.IsOwned(MediumTip));
        Assert.True(store.JustTipped);
        store.AcknowledgeTip();
        Assert.False(store.JustTipped);
    }

    [Fact]
    public async Task RevokedTipDoesNotThank()
    {
        var backend = MakeBackend();
        var store = MakeStore(backend);
        await store.StartAsync();
        backend.Send(new TransactionUpdate(SmallTip, IsRevoked: true));
        Assert.False(store.JustTipped);
        backend.Send(new TransactionUpdate(SmallTip, IsRevoked: false));
        Assert.True(store.JustTipped);
    }

    [Fact]
    public async Task CancelledPendingAndFailedPurchases()
    {
        var backend = MakeBackend();
        var store = MakeStore(backend);
        await store.StartAsync();

        backend.NextOutcome = PurchaseOutcome.Cancelled;
        await store.BuyAsync(Supporter);
        Assert.False(store.IsOwned(Supporter));
        Assert.Null(store.Error);
        Assert.False(store.IsPending);

        backend.NextOutcome = PurchaseOutcome.Pending;
        await store.BuyAsync(Supporter);
        Assert.True(store.IsPending);
        Assert.False(store.IsOwned(Supporter));

        // Approved later.
        backend.Owned = [Supporter];
        backend.Send(new TransactionUpdate(Supporter, IsRevoked: false));
        await Task.Yield();
        Assert.True(store.IsOwned(Supporter));
        Assert.False(store.IsPending);

        backend.FailPurchase = true;
        await store.TipAsync(SmallTip);
        Assert.Equal(PurchaseError.PurchaseFailed, store.Error);
        Assert.False(store.JustTipped);
        Assert.Null(store.Purchasing);
        Assert.False(store.IsBusy);

        store.ClearError();
        Assert.Null(store.Error);
    }

    [Fact]
    public async Task RestoreRefreshesAndReportsFailures()
    {
        var backend = MakeBackend();
        var store = MakeStore(backend);
        await store.StartAsync();
        backend.Owned = [Supporter];
        await store.RestoreAsync();
        Assert.Equal(1, backend.Restores);
        Assert.True(store.IsOwned(Supporter));
        Assert.Null(store.Error);
        Assert.False(store.IsRestoring);

        backend.FailRestore = true;
        await store.RestoreAsync();
        Assert.Equal(PurchaseError.RestoreFailed, store.Error);
        Assert.False(store.IsRestoring);
    }

    [Fact]
    public async Task MissingProductsLeavePricesEmpty()
    {
        var backend = MakeBackend();
        backend.FailProducts = true;
        var store = MakeStore(backend);
        await store.StartAsync();
        Assert.Null(store.Price(Supporter));
        Assert.Equal(LoadState.Failed, store.LoadState);
    }

    [Fact]
    public async Task MissingUnlockableFailsTheLoadButAMissingTipDoesNot()
    {
        var backend = MakeBackend();
        backend.MissingProducts = [Supporter];
        var store = MakeStore(backend);
        await store.StartAsync();
        Assert.Equal(LoadState.Failed, store.LoadState);
        Assert.NotNull(store.Price(SmallTip));

        var tipless = MakeBackend();
        tipless.MissingProducts = [SmallTip];
        var other = MakeStore(tipless);
        await other.StartAsync();
        Assert.Equal(LoadState.Loaded, other.LoadState);
        Assert.Null(other.Price(SmallTip));
    }

    [Fact]
    public async Task CatalogWithoutUnlockablesLoadsWhenAnythingLoads()
    {
        var tipsOnly = new ProductCatalog([], [SmallTip]);
        var store = new PurchaseStore(tipsOnly, new MockStoreBackend(tipsOnly), new InMemorySettingsStore());
        await store.StartAsync();
        Assert.Equal(LoadState.Loaded, store.LoadState);

        var empty = new MockStoreBackend(tipsOnly) { MissingProducts = [SmallTip] };
        var failed = new PurchaseStore(tipsOnly, empty, new InMemorySettingsStore());
        await failed.StartAsync();
        Assert.Equal(LoadState.Failed, failed.LoadState);
    }

    [Fact]
    public async Task RetryRecoversFromFailedLoad()
    {
        var backend = MakeBackend();
        backend.FailProducts = true;
        var store = MakeStore(backend);
        Assert.Equal(LoadState.Idle, store.LoadState);
        await store.StartAsync();
        Assert.Equal(LoadState.Failed, store.LoadState);

        backend.FailProducts = false;
        await store.LoadProductsIfNeededAsync();
        Assert.Equal(LoadState.Loaded, store.LoadState);
        Assert.NotNull(store.Price(Supporter));

        // Loaded already: another call doesn't ask again.
        backend.FailProducts = true;
        await store.LoadProductsIfNeededAsync();
        Assert.Equal(LoadState.Loaded, store.LoadState);
    }

    [Fact]
    public async Task ChangedIsRaisedForEveryVisibleChange()
    {
        var backend = MakeBackend();
        var store = MakeStore(backend);
        var changes = 0;
        store.Changed += () => changes++;

        await store.StartAsync();
        Assert.True(changes >= 2, "loading and loaded");

        changes = 0;
        await store.BuyAsync(Supporter);
        Assert.True(changes >= 2, "purchasing, then owned and done");

        changes = 0;
        await store.RestoreAsync();
        Assert.True(changes >= 2, "restoring, then done");

        changes = 0;
        store.AcknowledgeTip();
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task CustomPricesShowAsGiven()
    {
        var backend = new MockStoreBackend(Catalog) { PriceOf = id => id == Supporter ? "0,99 €" : "1,99 €" };
        var store = MakeStore(backend);
        await store.StartAsync();
        Assert.Equal("0,99 €", store.Price(Supporter));
        Assert.Equal("1,99 €", store.Price(SmallTip));
    }
}
