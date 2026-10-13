using Deskling.Companion.Shell.App;
using Deskling.Companion.Shell.Signals;
using Deskling.Companion.Shell.Speech;
using Deskling.Core.Scheduling;
using Deskling.Core.Settings;
using Deskling.Core.Store;

namespace Deskling.Companion.Shell.Screenshots;

/// <summary>
/// What a screenshot run's model is built on, so the user's real settings and the store are never touched (Swift's
/// <c>ScreenshotCapture.SampleEnvironment</c>): an in-memory settings store, a clock stopped at <see cref="Now"/> (a
/// Tuesday at 10:30 by default, inside the default active hours), an idle time of zero, never busy, a signal hub with no
/// system watchers, silent channels and a store with fixed prices. The capture itself is the platform's.
/// </summary>
public sealed class SampleEnvironment
{
    /// <param name="prices">Display prices by product id, as the store would show them ("1,99 €").</param>
    /// <param name="timers">The UI thread's timers (the hub's busy watch never runs: nothing is ever busy).</param>
    /// <param name="now">The moment the run shows; <see cref="SampleNow"/> by default.</param>
    public SampleEnvironment(IReadOnlyDictionary<string, string> prices, ITimers timers, DateTimeOffset? now = null)
    {
        Now = now ?? SampleNow(TimeZoneInfo.Local, DateTimeOffset.Now);
        Clock = new FixedClock(Now);
        Signals = new SignalHub(Idle, Busy, null, null, null, null, timers);
        Backend = new FixedPriceBackend(prices);
    }

    public DateTimeOffset Now { get; }

    public InMemorySettingsStore Store { get; } = new();

    public FixedClock Clock { get; }

    public ConstantIdle Idle { get; } = new();

    public NeverBusy Busy { get; } = new();

    public SignalHub Signals { get; }

    public SilentSpeaker Speaker { get; } = new();

    public SilentChime Chime { get; } = new();

    public SilentNotifier Notifier { get; } = new();

    /// <summary>The sample store; set <see cref="FixedPriceBackend.Owned"/> to shoot what a buyer sees.</summary>
    public FixedPriceBackend Backend { get; }

    /// <summary>A store over <see cref="Backend"/> that keeps what it learns in a throwaway settings store.</summary>
    public PurchaseStore Purchases(ProductCatalog catalog) => new(catalog, Backend, new InMemorySettingsStore());

    /// <summary>
    /// The next <paramref name="weekday"/> at <paramref name="hour"/>:<paramref name="minute"/> in
    /// <paramref name="timeZone"/> after <paramref name="after"/> (Swift's <c>sampleNow</c>): screenshots show the same
    /// time of day whenever they're taken.
    /// </summary>
    public static DateTimeOffset SampleNow(
        TimeZoneInfo timeZone, DateTimeOffset after, int hour = 10, int minute = 30, DayOfWeek weekday = DayOfWeek.Tuesday)
    {
        var local = TimeZoneInfo.ConvertTime(after, timeZone);
        for (var days = 0; days <= 7; days++)
        {
            var day = local.Date.AddDays(days);
            if (day.DayOfWeek != weekday)
                continue;
            var wall = day.AddHours(hour).AddMinutes(minute);
            var moment = new DateTimeOffset(wall, timeZone.GetUtcOffset(wall));
            if (moment > after)
                return moment;
        }
        return after;
    }
}

/// <summary>A clock stopped at one moment.</summary>
public sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset Now { get; } = now;
}

/// <summary>The user is always at the keyboard.</summary>
public sealed class ConstantIdle(double seconds = 0) : IIdleTimeProvider
{
    public double SecondsSinceLastInput() => seconds;
}

/// <summary>A notification that never shows and is always allowed, for screenshot runs and previews.</summary>
public sealed class SilentNotifier : ICompanionNotifying
{
    public Action<string>? OnAction { get; set; }

    public void Configure() { }

    public void RequestAuthorization(Action<bool> completion) => completion(true);

    public void RefreshAuthorization(Action<NotificationPermission> completion) => completion(NotificationPermission.Authorized);

    public void Post(string line) { }
}

/// <summary>Fixed prices without asking the store. Purchases are cancelled; <see cref="Owned"/> is what the store says is bought.</summary>
public sealed class FixedPriceBackend(IReadOnlyDictionary<string, string> prices) : IStoreBackend
{
    public IReadOnlyDictionary<string, string> Prices { get; } = prices;

    public HashSet<string> Owned { get; set; } = [];

    public Task<IReadOnlyList<StoreProduct>> LoadProductsAsync(IReadOnlyList<string> ids) =>
        Task.FromResult<IReadOnlyList<StoreProduct>>(
            ids.Where(Prices.ContainsKey).Select(id => new StoreProduct(id, Prices[id])).ToArray());

    public Task<IReadOnlySet<string>> EntitledProductIdsAsync() => Task.FromResult<IReadOnlySet<string>>(Owned.ToHashSet());

    public Task<PurchaseOutcome> PurchaseAsync(string id) => Task.FromResult(PurchaseOutcome.Cancelled);

    public Task RestoreAsync() => Task.CompletedTask;

    public void ObserveTransactions(Action<TransactionUpdate> onUpdate) { }
}
