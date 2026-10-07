using Deskling.Core.Random;
using Deskling.Core.Scheduling;

namespace Deskling.Core.Tests;

/// <summary>A clock tests move by hand.</summary>
public sealed class MockClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset Now { get; set; } = now;

    public void AdvanceMinutes(double minutes) => Now = Now.AddMinutes(minutes);
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

/// <summary>A random source that returns the given unit values in order, then repeats the last one.</summary>
public sealed class ScriptedRandom(IReadOnlyList<double> values) : IRandomSource
{
    public int Reads { get; private set; }

    public double NextUnit()
    {
        var value = values[Math.Min(Reads, values.Count - 1)];
        Reads++;
        return value;
    }
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
