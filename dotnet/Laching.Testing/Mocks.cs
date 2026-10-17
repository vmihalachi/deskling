using Laching.Core.Random;
using Laching.Core.Scheduling;

namespace Laching.Testing;

/// <summary>A clock tests move by hand.</summary>
public sealed class MockClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset Now { get; set; } = now;

    public void AdvanceMinutes(double minutes) => Now = Now.AddMinutes(minutes);

    public void AdvanceSeconds(double seconds) => Now = Now.AddSeconds(seconds);
}

/// <summary>An idle-time provider with a settable value.</summary>
public sealed class MockIdle : IIdleTimeProvider
{
    public double Idle { get; set; }

    public double SecondsSinceLastInput() => Idle;
}

/// <summary>A busy-state provider with settable reasons.</summary>
public sealed class MockBusy : IBusyStateProvider
{
    public HashSet<BusyReason> Reasons { get; set; } = [];

    public IReadOnlySet<BusyReason> CurrentBusyReasons() => Reasons;
}

/// <summary>A random source that reports every read, for asserting that code never consults it.</summary>
public sealed class CountingRandom(Action? onRead = null) : IRandomSource
{
    public int Reads { get; private set; }

    public double NextUnit()
    {
        Reads++;
        onRead?.Invoke();
        return 0.5;
    }
}
